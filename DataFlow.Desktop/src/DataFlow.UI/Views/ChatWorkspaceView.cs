using DataFlow.Core.Enums;
using DataFlow.Core.Interfaces;
using DataFlow.Core.Models;
using DataFlow.Core.Navigation;
using DataFlow.Core.Themes;
using DataFlow.UI.Controls;
using DataFlow.UI.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace DataFlow.UI.Views;

public sealed class ChatWorkspaceView : UserControl, INavigationAware
{
    private readonly IDataFlowApiClient _api;
    private readonly ILocalAnalyticsService _local;
    private readonly IAppStateService _state;
    private readonly INavigationService _navigation;
    private readonly IAppShellService _shell;

    private readonly Panel _heroPanel;
    private readonly RichTextBox _messages;
    private readonly ChatStarterCardsControl _starters;
    private readonly TextBox _input;
    private readonly Label _disclaimer;
    private string? _sessionId;
    private bool _hasMessages;

    public ChatWorkspaceView(IServiceProvider services)
    {
        _api = services.GetRequiredService<IDataFlowApiClient>();
        _local = services.GetRequiredService<ILocalAnalyticsService>();
        _state = services.GetRequiredService<IAppStateService>();
        _navigation = services.GetRequiredService<INavigationService>();
        _shell = services.GetRequiredService<IAppShellService>();
        BackColor = DesignTokens.PageBg;
        Dock = DockStyle.Fill;

        var subHeader = new Panel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(24, 12, 24, 0) };
        subHeader.Controls.Add(new Label
        {
            Text = "AI Workspace",
            Font = new Font(DesignTokens.FontFamily, 13f, FontStyle.Bold),
            ForeColor = DesignTokens.Text,
            AutoSize = true,
            Location = new Point(0, 6)
        });
        var contextBtn = new Button
        {
            Text = "Context  ▾",
            Size = new Size(96, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = DesignTokens.InputBg,
            ForeColor = DesignTokens.TextMuted,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        contextBtn.FlatAppearance.BorderColor = DesignTokens.Border;
        subHeader.Controls.Add(contextBtn);
        subHeader.Resize += (_, _) => contextBtn.Location = new Point(subHeader.Width - 100, 8);

        _heroPanel = new Panel { Dock = DockStyle.Fill, BackColor = DesignTokens.PageBg };

        var heroCenter = new Panel { Size = new Size(560, 420), BackColor = Color.Transparent };
        heroCenter.Anchor = AnchorStyles.None;

        var aiBadge = new Panel { Size = new Size(48, 48), Location = new Point(256, 0), BackColor = DesignTokens.Accent };
        aiBadge.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var path = GraphicsExtensions.CreateRoundedRect(new Rectangle(0, 0, 47, 47), 12);
            e.Graphics.FillPath(new SolidBrush(DesignTokens.Accent), path);
            TextRenderer.DrawText(e.Graphics, "AI", new Font(DesignTokens.FontFamily, 11f, FontStyle.Bold),
                new Rectangle(0, 0, 48, 48), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };

        var title = new Label
        {
            Text = "Ask anything about your data",
            Location = new Point(0, 64),
            Size = new Size(560, 32),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(DesignTokens.FontFamily, 18f, FontStyle.Bold),
            ForeColor = DesignTokens.Text
        };
        var subtitle = new Label
        {
            Text = "Upload a dataset and start exploring. DataFlow will analyze,\nvisualize, and summarize your data instantly.",
            Location = new Point(0, 100),
            Size = new Size(560, 44),
            TextAlign = ContentAlignment.TopCenter,
            Font = new Font(DesignTokens.FontFamily, 9.5f),
            ForeColor = DesignTokens.TextMuted
        };

        _starters = new ChatStarterCardsControl { Location = new Point(20, 152), Size = new Size(520, 200) };
        _starters.PromptSelected += async (_, p) => await SendPromptAsync(p);
        _starters.CreateDashboardRequested += (_, _) => _navigation.Navigate(NavigationArgs.For(AppView.ProjectsHome));

        heroCenter.Controls.AddRange([aiBadge, title, subtitle, _starters]);
        _heroPanel.Controls.Add(heroCenter);
        _heroPanel.Resize += (_, _) =>
        {
            heroCenter.Left = Math.Max(0, (_heroPanel.Width - heroCenter.Width) / 2);
            heroCenter.Top = Math.Max(40, (_heroPanel.Height - heroCenter.Height) / 2 - 40);
        };

        _messages = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Visible = false,
            BackColor = DesignTokens.PageBg,
            BorderStyle = BorderStyle.None,
            Font = new Font(DesignTokens.FontFamily, 10f)
        };

        _disclaimer = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 22,
            Text = "DataFlow may make mistakes. Double-check important outputs.",
            ForeColor = DesignTokens.TextDim,
            Font = new Font(DesignTokens.FontFamily, 8f),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var inputPanel = new Panel { Dock = DockStyle.Bottom, Height = 72, Padding = new Padding(24, 8, 24, 8), BackColor = DesignTokens.PageBg };
        var inputWrap = new Panel { Dock = DockStyle.Fill, Height = 48, BackColor = DesignTokens.InputBg };
        inputWrap.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var path = GraphicsExtensions.CreateRoundedRect(new Rectangle(0, 0, inputWrap.Width - 1, inputWrap.Height - 1), 12);
            e.Graphics.FillPath(new SolidBrush(DesignTokens.InputBg), path);
            using var pen = new Pen(DesignTokens.Border);
            e.Graphics.DrawPath(pen, path);
        };

        var attach = new Button { Text = "📎", Size = new Size(36, 36), Location = new Point(8, 6), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent };
        attach.Click += async (_, _) => await AttachFileAsync();

        _input = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = DesignTokens.InputBg,
            Font = new Font(DesignTokens.FontFamily, 10f),
            PlaceholderText = "Ask anything about your data… (Enter to send, Shift+Enter for newline)",
            Location = new Point(48, 14),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        _input.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter && !e.Shift) { e.SuppressKeyPress = true; await SendAsync(); }
        };

        var send = new Button { Text = "➤", Size = new Size(36, 36), FlatStyle = FlatStyle.Flat, BackColor = DesignTokens.Accent, ForeColor = Color.White, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        send.FlatAppearance.BorderSize = 0;
        send.Click += async (_, _) => await SendAsync();

        inputWrap.Controls.Add(attach);
        inputWrap.Controls.Add(_input);
        inputWrap.Controls.Add(send);
        inputWrap.Resize += (_, _) => { _input.Width = inputWrap.Width - 100; send.Location = new Point(inputWrap.Width - 44, 6); };
        inputPanel.Controls.Add(inputWrap);

        Controls.Add(_messages);
        Controls.Add(_heroPanel);
        Controls.Add(inputPanel);
        Controls.Add(_disclaimer);
        Controls.Add(subHeader);
    }

    public void OnNavigatedTo(NavigationArgs args)
    {
        _sessionId = _state.SessionId;
        _shell.ShowConversationsPanel = true;
        _shell.SetConversationsEmpty("No conversations yet.\nStart a new chat.");
        UpdateViewMode();
    }

    private void UpdateViewMode()
    {
        _heroPanel.Visible = !_hasMessages;
        _messages.Visible = _hasMessages;
    }

    private async Task AttachFileAsync()
    {
        using var dlg = new OpenFileDialog { Filter = "Data files|*.csv;*.tsv;*.xlsx;*.xls;*.json;*.parquet|All files|*.*" };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        if (_local.CanParse(dlg.FileName))
        {
            try
            {
                var data = _local.LoadFromFile(dlg.FileName);
                var insights = _local.ComputeBasicInsights(data);
                _hasMessages = true;
                UpdateViewMode();
                AppendMessage("assistant", $"**Dataset loaded** ({data.Rows.Count} rows)\n\n{insights.SummaryText}");
                return;
            }
            catch { }
        }

        try
        {
            var response = await _api.SendChatAsync("Analyze this dataset.", null, dlg.FileName, null);
            ApplyResponse(response);
        }
        catch (Exception ex) { AppendMessage("system", ex.Message); }
    }

    private async Task SendPromptAsync(string prompt) { _input.Text = prompt; await SendAsync(); }

    private async Task SendAsync()
    {
        var text = _input.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;
        _input.Clear();
        AppendMessage("user", text);
        try
        {
            var response = await _api.SendChatAsync(text, _sessionId, null, null);
            ApplyResponse(response);
        }
        catch (Exception ex) { AppendMessage("system", ex.Message); }
    }

    private void ApplyResponse(ChatResponseDto response)
    {
        if (!string.IsNullOrEmpty(response.SessionId)) { _sessionId = response.SessionId; _state.SessionId = response.SessionId; }
        if (response.Errors.Count > 0) { foreach (var err in response.Errors) AppendMessage("system", err.Message); return; }
        if (!string.IsNullOrEmpty(response.Answer)) AppendMessage("assistant", response.Answer);
    }

    private void AppendMessage(string role, string text)
    {
        if (role is "user" or "assistant") { _hasMessages = true; UpdateViewMode(); }
        ChatMarkdownRenderer.AppendMessage(_messages, role, text);
    }
}
