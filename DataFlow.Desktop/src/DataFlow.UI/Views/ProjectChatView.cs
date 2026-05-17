using DataFlow.Core.Enums;
using DataFlow.Core.Interfaces;
using DataFlow.Core.Models;
using DataFlow.Core.Navigation;
using DataFlow.Core.Themes;
using DataFlow.UI.Controls;
using DataFlow.UI.Dialogs;
using DataFlow.UI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace DataFlow.UI.Views;

public sealed class ProjectChatView : UserControl, INavigationAware
{
    private readonly IDataFlowApiClient _api;
    private readonly INavigationService _navigation;
    private readonly IAppShellService _shell;
    private readonly RichTextBox _messages;
    private readonly TextBox _input;
    private readonly Label _banner;

    private int _projectId;
    private int? _dbSessionId;
    private DatasetDto? _dataset;
    private string? _aiSessionId;
    private readonly List<(string Role, string Content)> _history = [];
    private int _savedCount;

    public ProjectChatView(IServiceProvider services)
    {
        _api = services.GetRequiredService<IDataFlowApiClient>();
        _navigation = services.GetRequiredService<INavigationService>();
        _shell = services.GetRequiredService<IAppShellService>();
        BackColor = DesignTokens.PageBg;
        Dock = DockStyle.Fill;

        var subHeader = new Panel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(24, 12, 24, 0) };
        var back = new Button { Text = "← Back", FlatStyle = FlatStyle.Flat, AutoSize = true, Location = new Point(0, 6) };
        back.Click += (_, _) => _navigation.Navigate(NavigationArgs.For(AppView.ProjectDetail, _projectId));
        subHeader.Controls.Add(back);
        subHeader.Controls.Add(new Label
        {
            Text = "Project Chat",
            Location = new Point(80, 6),
            AutoSize = true,
            Font = new Font(DesignTokens.FontFamily, 13f, FontStyle.Bold),
            ForeColor = DesignTokens.Text
        });

        _banner = new Label
        {
            Dock = DockStyle.Top,
            Height = 32,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(24, 0, 0, 0),
            BackColor = DesignTokens.AccentBg,
            ForeColor = DesignTokens.Accent,
            Font = new Font(DesignTokens.FontFamily, 9f)
        };

        _messages = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BackColor = Color.White, BorderStyle = BorderStyle.None, Font = new Font(DesignTokens.FontFamily, 10f) };

        var inputPanel = new Panel { Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(24, 8, 24, 8) };
        _input = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Ask about this project's data…", Font = new Font(DesignTokens.FontFamily, 10f) };
        _input.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter && !e.Shift) { e.SuppressKeyPress = true; await SendAsync(); } };
        var send = new Button { Text = "➤", Dock = DockStyle.Right, Width = 48, BackColor = DesignTokens.Accent, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        send.FlatAppearance.BorderSize = 0;
        send.Click += async (_, _) => await SendAsync();
        inputPanel.Controls.Add(_input);
        inputPanel.Controls.Add(send);

        Controls.Add(_messages);
        Controls.Add(inputPanel);
        Controls.Add(_banner);
        Controls.Add(subHeader);

        _shell.NewConversationRequested += OnNewConversation;
        _shell.ConversationSelected += OnConversationSelected;
    }

    public void OnNavigatedTo(NavigationArgs args)
    {
        if (args.ProjectId is not int id) return;
        _projectId = id;
        if (args.ChatSessionId is int chatId) _dbSessionId = chatId;
        _shell.ShowConversationsPanel = true;
        _ = InitAsync();
    }

    private async Task InitAsync()
    {
        try
        {
            var datasets = await _api.ListDatasetsAsync(_projectId);
            _dataset = datasets.FirstOrDefault();
            if (_dataset != null)
            {
                var revive = await _api.ReviveSessionAsync(_dataset.Id);
                _aiSessionId = revive.AiSessionId ?? _dataset.SessionId;
                _banner.Text = $"  📊  {_dataset.FileName} — {_dataset.RowCount:N0} rows";
            }
            else
            {
                _banner.Text = "  ⚠ Upload a dataset on the project page.";
                _banner.BackColor = Color.FromArgb(255, 251, 235);
                _banner.ForeColor = Color.FromArgb(245, 158, 11);
            }

            await RefreshConversationsAsync();
            if (_dbSessionId.HasValue)
                await LoadSessionAsync(_dbSessionId.Value);
            else
                NewChat();
        }
        catch (Exception ex)
        {
            _banner.Text = "  " + ex.Message;
        }
    }

    private async Task RefreshConversationsAsync()
    {
        var list = await _api.ListChatSessionsAsync(_projectId);
        _shell.LoadConversations(list.Select(s => new ConversationItem { Id = s.Id, Title = s.Title ?? "Untitled" }).ToList());
        _shell.SelectConversation(_dbSessionId);
    }

    private void OnNewConversation(object? sender, EventArgs e) => NewChat();

    private async void OnConversationSelected(object? sender, int id) => await LoadSessionAsync(id);

    private void NewChat()
    {
        _dbSessionId = null;
        _savedCount = 0;
        _history.Clear();
        _messages.Clear();
        _shell.SelectConversation(null);
    }

    private async Task LoadSessionAsync(int sessionId)
    {
        _dbSessionId = sessionId;
        _history.Clear();
        _messages.Clear();
        var msgs = await _api.GetChatMessagesAsync(sessionId);
        foreach (var m in msgs) { _history.Add((m.Role, m.Content)); Append(m.Role, m.Content); }
        _savedCount = _history.Count;
        _shell.SelectConversation(sessionId);
    }

    private async Task SendAsync()
    {
        var text = _input.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;
        _input.Clear();
        Append("user", text);
        _history.Add(("user", text));
        try
        {
            var historyJson = JsonConvert.SerializeObject(_history.TakeLast(20).Select(h => new { role = h.Role, content = h.Content }));
            var response = await _api.SendChatAsync(text, _aiSessionId, null, historyJson);
            if (!string.IsNullOrEmpty(response.SessionId)) _aiSessionId = response.SessionId;
            if (response.Errors.Count > 0) { foreach (var err in response.Errors) Append("system", err.Message); }
            else if (!string.IsNullOrEmpty(response.Answer)) { Append("assistant", response.Answer); _history.Add(("assistant", response.Answer)); await PersistMessagesAsync(); }
        }
        catch (Exception ex) { Append("system", ex.Message); }
    }

    private async Task PersistMessagesAsync()
    {
        if (_history.Count <= _savedCount) return;
        var newMsgs = _history.Skip(_savedCount).ToList();
        if (!_dbSessionId.HasValue)
        {
            var firstUser = newMsgs.FirstOrDefault(m => m.Role == "user");
            var title = firstUser.Content ?? "New conversation";
            if (title.Length > 80) title = title[..80];
            var session = await _api.CreateChatSessionAsync(new ChatSessionCreateRequest
            {
                ProjectId = _projectId, DatasetId = _dataset?.Id, SessionId = _aiSessionId, Title = title
            });
            _dbSessionId = session.Id;
            await RefreshConversationsAsync();
        }
        foreach (var m in newMsgs)
            await _api.AddChatMessageAsync(_dbSessionId.Value, new ChatMessageCreateRequest { Role = m.Role, Content = m.Content });
        _savedCount = _history.Count;
    }

    private void Append(string role, string text) => ChatMarkdownRenderer.AppendMessage(_messages, role, text);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _shell.NewConversationRequested -= OnNewConversation;
            _shell.ConversationSelected -= OnConversationSelected;
        }
        base.Dispose(disposing);
    }
}
