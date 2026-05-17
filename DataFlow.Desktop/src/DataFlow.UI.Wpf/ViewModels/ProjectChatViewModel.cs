using System.Collections.ObjectModel;
using DataFlow.Core.Interfaces;
using DataFlow.Core.Models;

namespace DataFlow.UI.Wpf.ViewModels;

public sealed class ProjectChatViewModel : BaseViewModel
{
    private readonly IDataFlowApiClient _api;
    private int _projectId;

    // ── Dataset banner ────────────────────────────────────────────
    private DatasetDto? _dataset;
    private bool        _datasetReady;
    public DatasetDto?  Dataset      { get => _dataset;      set { Set(ref _dataset, value); Notify(nameof(HasDataset)); } }
    public bool         DatasetReady { get => _datasetReady; set => Set(ref _datasetReady, value); }
    public bool         HasDataset   => _dataset != null;

    // ── Sessions panel ────────────────────────────────────────────
    private bool   _panelCollapsed;
    private int?   _activeSessionId;
    public bool    PanelCollapsed   { get => _panelCollapsed;  set => Set(ref _panelCollapsed, value); }
    public int?    ActiveSessionId  { get => _activeSessionId; set => Set(ref _activeSessionId, value); }
    public ObservableCollection<ChatSessionDto> Sessions { get; } = [];

    // ── Chat messages ─────────────────────────────────────────────
    private bool   _showWelcome = true;
    private bool   _sending;
    private string _inputText  = string.Empty;
    public bool    ShowWelcome { get => _showWelcome; set => Set(ref _showWelcome, value); }
    public bool    Sending     { get => _sending;     set { Set(ref _sending, value); SendCommand.RaiseCanExecuteChanged(); } }
    public string  InputText   { get => _inputText;   set { Set(ref _inputText, value); SendCommand.RaiseCanExecuteChanged(); } }
    public ObservableCollection<ChatMessage> Messages { get; } = [];

    // ── Rename modal ──────────────────────────────────────────────
    private bool   _renameVisible;
    private string _renameText = string.Empty;
    private int?   _renamingId;
    public bool    RenameVisible { get => _renameVisible; set => Set(ref _renameVisible, value); }
    public string  RenameText   { get => _renameText;    set => Set(ref _renameText, value); }

    // ── File attach ───────────────────────────────────────────────
    private string _attachedFile = string.Empty;
    private bool   _hasAttachment;
    private string? _attachedFilePath;
    public string  AttachedFile  { get => _attachedFile;  set => Set(ref _attachedFile, value); }
    public bool    HasAttachment { get => _hasAttachment; set => Set(ref _hasAttachment, value); }

    // ── AI session ────────────────────────────────────────────────
    private string? _aiSessionId;
    private readonly List<(string role, string content)> _history = [];
    private int? _dbSessionId;
    private int  _savedCount;

    // ── Commands ──────────────────────────────────────────────────
    public AsyncRelayCommand  SendCommand        { get; }
    public RelayCommand       NewChatCommand     { get; }
    public RelayCommand       TogglePanelCommand { get; }
    public RelayCommand       AttachCommand      { get; }
    public RelayCommand       RemoveAttachment   { get; }
    public AsyncRelayCommand  RenameConfirmCommand { get; }
    public RelayCommand       RenameCancelCommand  { get; }
    public RelayCommand<ChatSessionDto> SelectSessionCommand { get; }
    public RelayCommand<ChatSessionDto> OpenRenameCommand    { get; }
    public RelayCommand<ChatSessionDto> DeleteSessionCommand { get; }

    public ProjectChatViewModel(IDataFlowApiClient api)
    {
        _api = api;
        SendCommand          = new AsyncRelayCommand(SendAsync, () => !string.IsNullOrWhiteSpace(InputText) && !Sending);
        NewChatCommand       = new RelayCommand(StartNewChat);
        TogglePanelCommand   = new RelayCommand(() => PanelCollapsed = !PanelCollapsed);
        AttachCommand        = new RelayCommand(() => AttachRequested?.Invoke(this, EventArgs.Empty));
        RemoveAttachment     = new RelayCommand(ClearAttachment);
        RenameConfirmCommand = new AsyncRelayCommand(ConfirmRenameAsync);
        RenameCancelCommand  = new RelayCommand(() => RenameVisible = false);
        SelectSessionCommand = new RelayCommand<ChatSessionDto>(s => _ = SelectSessionAsync(s));
        OpenRenameCommand    = new RelayCommand<ChatSessionDto>(s => { _renamingId = s.Id; RenameText = s.Title ?? ""; RenameVisible = true; });
        DeleteSessionCommand = new RelayCommand<ChatSessionDto>(s => _ = DeleteSessionAsync(s));
    }

    public async Task LoadAsync(int projectId, int? chatSessionId = null)
    {
        _projectId = projectId;
        await InitDatasetAsync();
        await LoadSessionsAsync();
        if (chatSessionId.HasValue)
        {
            var s = Sessions.FirstOrDefault(x => x.Id == chatSessionId.Value);
            if (s != null) await SelectSessionAsync(s);
        }
    }

    private async Task InitDatasetAsync()
    {
        try
        {
            var datasets = await _api.ListDatasetsAsync(_projectId);
            Dataset = datasets.FirstOrDefault();
            if (Dataset != null)
            {
                try
                {
                    var revived = await _api.ReviveSessionAsync(Dataset.Id);
                    _aiSessionId = revived.AiSessionId ?? Dataset.AiSessionId;
                }
                catch { _aiSessionId = Dataset.AiSessionId; }
            }
        }
        catch { /* ignore */ }
        finally { DatasetReady = true; }
    }

    private async Task LoadSessionsAsync()
    {
        try
        {
            var sessions = await _api.ListChatSessionsAsync(_projectId);
            Sessions.Clear();
            foreach (var s in sessions) Sessions.Add(s);
        }
        catch { /* ignore */ }
    }

    private async Task SelectSessionAsync(ChatSessionDto session)
    {
        try
        {
            var msgs = await _api.GetChatMessagesAsync(session.Id);
            ActiveSessionId = session.Id;
            _dbSessionId    = session.Id;
            _savedCount     = msgs.Count;
            _history.Clear();
            Messages.Clear();
            ShowWelcome = false;
            foreach (var m in msgs)
            {
                Messages.Add(new ChatMessage { Role = m.Role, Content = m.Content });
                _history.Add((m.Role, m.Content));
            }
        }
        catch { /* ignore */ }
    }

    private void StartNewChat()
    {
        ActiveSessionId = null;
        _dbSessionId    = null;
        _savedCount     = 0;
        _history.Clear();
        Messages.Clear();
        ShowWelcome = true;
        ClearAttachment();
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

    private async Task SendAsync()
    {
        var text = InputText.Trim();
        if (string.IsNullOrEmpty(text)) return;

        ShowWelcome = false;
        InputText   = string.Empty;
        Sending     = true;
        var filePath = _attachedFilePath;
        ClearAttachment();

        Messages.Add(new ChatMessage { Role = "user", Content = text, FileName = filePath != null ? System.IO.Path.GetFileName(filePath) : null });
        _history.Add(("user", text));

        var thinking = new ChatMessage { Role = "assistant", Content = "Thinking…", IsThinking = true };
        Messages.Add(thinking);

        try
        {
            var histJson = System.Text.Json.JsonSerializer.Serialize(
                _history.TakeLast(10).Select(h => new { role = h.role, content = h.content }));
            var resp = await _api.SendChatAsync(text, _aiSessionId, filePath, histJson);
            if (filePath != null && resp.SessionId != null) _aiSessionId = resp.SessionId;
            var answer = resp.Answer ?? "No response.";
            _history.Add(("assistant", answer));
            Messages.Remove(thinking);
            Messages.Add(new ChatMessage { Role = "assistant", Content = answer });
            await PersistAsync(text, answer);
        }
        catch (Exception ex)
        {
            Messages.Remove(thinking);
            Messages.Add(new ChatMessage { Role = "assistant", Content = $"⚠ {ex.Message}" });
        }
        finally { Sending = false; }
    }

    private async Task PersistAsync(string userText, string aiAnswer)
    {
        try
        {
            if (_dbSessionId == null)
            {
                var s = await _api.CreateChatSessionAsync(new ChatSessionCreateRequest
                {
                    ProjectId = _projectId,
                    Title = userText.Length > 80 ? userText[..80] : userText
                });
                _dbSessionId    = s.Id;
                ActiveSessionId = s.Id;
                Sessions.Insert(0, s);
            }
            var allMsgs = _history.ToList();
            foreach (var m in allMsgs.Skip(_savedCount))
                await _api.AddChatMessageAsync(_dbSessionId.Value, new ChatMessageCreateRequest { Role = m.role, Content = m.content });
            _savedCount = allMsgs.Count;
        }
        catch { /* ignore */ }
    }

    private async Task ConfirmRenameAsync()
    {
        if (_renamingId == null || string.IsNullOrWhiteSpace(RenameText)) { RenameVisible = false; return; }
        try
        {
            await _api.UpdateChatSessionTitleAsync(_renamingId.Value, RenameText.Trim());
            var s = Sessions.FirstOrDefault(x => x.Id == _renamingId.Value);
            if (s != null) s.Title = RenameText.Trim();
        }
        catch { /* ignore */ }
        RenameVisible = false;
    }

    private async Task DeleteSessionAsync(ChatSessionDto session)
    {
        try
        {
            await _api.DeleteChatSessionAsync(session.Id);
            Sessions.Remove(session);
            if (ActiveSessionId == session.Id) StartNewChat();
        }
        catch { /* ignore */ }
    }

    // ── Events ────────────────────────────────────────────────────
    public event EventHandler? AttachRequested;
}
