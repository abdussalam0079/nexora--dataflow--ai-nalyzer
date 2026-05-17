using System.Collections.ObjectModel;
using DataFlow.Core.Interfaces;
using DataFlow.Core.Models;

namespace DataFlow.UI.Wpf.ViewModels;

public sealed class ChatMessage
{
    public string Role      { get; init; } = "user";
    public string Content   { get; init; } = string.Empty;
    public string? FileName { get; init; }
    public string Time      { get; init; } = DateTime.Now.ToString("HH:mm");
    public bool IsUser      => Role == "user";
    public bool IsThinking  { get; init; }
}

public sealed class ChatWorkspaceViewModel : BaseViewModel
{
    private readonly IDataFlowApiClient? _api;

    // ── State ─────────────────────────────────────────────────────
    private string  _inputText       = string.Empty;
    private bool    _sending         = false;
    private bool    _apiOnline       = false;
    private string  _apiStatusText   = "Checking API…";
    private string  _attachedFile    = string.Empty;
    private bool    _hasAttachment   = false;
    private bool    _showWelcome     = true;

    public string InputText     { get => _inputText;     set { Set(ref _inputText, value);   SendCommand.RaiseCanExecuteChanged(); } }
    public bool   Sending       { get => _sending;       set { Set(ref _sending, value);     SendCommand.RaiseCanExecuteChanged(); } }
    public bool   ApiOnline     { get => _apiOnline;     set => Set(ref _apiOnline, value); }
    public string ApiStatusText { get => _apiStatusText; set => Set(ref _apiStatusText, value); }
    public string AttachedFile  { get => _attachedFile;  set => Set(ref _attachedFile, value); }
    public bool   HasAttachment { get => _hasAttachment; set => Set(ref _hasAttachment, value); }
    public bool   ShowWelcome   { get => _showWelcome;   set => Set(ref _showWelcome, value); }

    public ObservableCollection<ChatMessage> Messages { get; } = [];

    private readonly List<(string role, string content)> _history = [];
    private string? _sessionId;
    private string? _attachedFilePath;

    // ── Commands ──────────────────────────────────────────────────
    public AsyncRelayCommand  SendCommand       { get; }
    public RelayCommand       NewChatCommand    { get; }
    public RelayCommand       AttachFileCommand { get; }
    public RelayCommand       RemoveAttachment  { get; }

    public ChatWorkspaceViewModel(IDataFlowApiClient? api)
    {
        _api = api;
        SendCommand       = new AsyncRelayCommand(SendAsync, () => !string.IsNullOrWhiteSpace(InputText) && !Sending);
        NewChatCommand    = new RelayCommand(NewChat);
        AttachFileCommand = new RelayCommand(() => AttachFileRequested?.Invoke(this, EventArgs.Empty));
        RemoveAttachment  = new RelayCommand(ClearAttachment);
        _ = CheckApiAsync();
    }

    public async Task CheckApiAsync()
    {
        if (_api == null) { SetOffline(); return; }
        try
        {
            ApiOnline = await _api.HealthCheckAsync();
            if (ApiOnline) { ApiStatusText = "API Connected"; }
            else SetOffline();
        }
        catch { SetOffline(); }
    }

    private void SetOffline()
    {
        ApiOnline     = false;
        ApiStatusText = "Offline — local mode";
    }

    public void SetAttachment(string path)
    {
        _attachedFilePath = path;
        AttachedFile      = System.IO.Path.GetFileName(path);
        HasAttachment     = true;
    }

    private void ClearAttachment()
    {
        _attachedFilePath = null;
        AttachedFile      = string.Empty;
        HasAttachment     = false;
    }

    public void SendSuggestion(string prompt)
    {
        InputText = prompt;
        _ = SendAsync();
    }

    private async Task SendAsync()
    {
        var text = InputText.Trim();
        if (string.IsNullOrEmpty(text)) return;

        ShowWelcome = false;
        InputText   = string.Empty;
        Sending     = true;

        var filePath = _attachedFilePath;
        ClearAttachment();

        Messages.Add(new ChatMessage
        {
            Role     = "user",
            Content  = text,
            FileName = filePath != null ? System.IO.Path.GetFileName(filePath) : null
        });
        _history.Add(("user", text));

        // Thinking indicator
        var thinking = new ChatMessage { Role = "assistant", Content = "Thinking…", IsThinking = true };
        Messages.Add(thinking);

        try
        {
            string reply;
            if (ApiOnline && _api != null)
            {
                var histJson = System.Text.Json.JsonSerializer.Serialize(
                    _history.TakeLast(10).Select(h => new { role = h.role, content = h.content }));
                var resp = await _api.SendChatAsync(text, _sessionId, filePath, histJson);
                _sessionId = resp.SessionId ?? _sessionId;
                reply = resp.Errors.Count > 0
                    ? $"⚠ {resp.Errors[0].Message}"
                    : resp.Answer ?? "No response.";
            }
            else
            {
                reply = "API is offline. Start the API server to enable AI analysis.";
            }

            Messages.Remove(thinking);
            Messages.Add(new ChatMessage { Role = "assistant", Content = reply });
            _history.Add(("assistant", reply));
        }
        catch (Exception ex)
        {
            Messages.Remove(thinking);
            Messages.Add(new ChatMessage { Role = "assistant", Content = $"⚠ Error: {ex.Message}" });
        }
        finally { Sending = false; }
    }

    private void NewChat()
    {
        _sessionId = null;
        _history.Clear();
        Messages.Clear();
        ShowWelcome = true;
        ClearAttachment();
    }

    // ── Events ────────────────────────────────────────────────────
    public event EventHandler? AttachFileRequested;
}
