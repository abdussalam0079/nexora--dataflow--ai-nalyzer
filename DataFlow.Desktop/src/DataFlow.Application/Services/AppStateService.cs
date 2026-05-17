using DataFlow.Core.Interfaces;

namespace DataFlow.Application.Services;

public sealed class AppStateService : IAppStateService
{
    private int? _activeProjectId;
    private int? _activeDatasetId;
    private int? _activeDashboardId;
    private int? _activeChatSessionId;
    private string? _sessionId;
    private bool _sidebarCollapsed;

    public int? ActiveProjectId
    {
        get => _activeProjectId;
        set { _activeProjectId = value; OnStateChanged(); }
    }

    public int? ActiveDatasetId
    {
        get => _activeDatasetId;
        set { _activeDatasetId = value; OnStateChanged(); }
    }

    public int? ActiveDashboardId
    {
        get => _activeDashboardId;
        set { _activeDashboardId = value; OnStateChanged(); }
    }

    public int? ActiveChatSessionId
    {
        get => _activeChatSessionId;
        set { _activeChatSessionId = value; OnStateChanged(); }
    }

    public string? SessionId
    {
        get => _sessionId;
        set { _sessionId = value; OnStateChanged(); }
    }

    public bool SidebarCollapsed
    {
        get => _sidebarCollapsed;
        set { _sidebarCollapsed = value; OnStateChanged(); }
    }

    public event EventHandler? StateChanged;

    public void SetProject(int? projectId)
    {
        _activeProjectId = projectId;
        _activeDatasetId = null;
        _activeDashboardId = null;
        _activeChatSessionId = null;
        OnStateChanged();
    }

    private void OnStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}
