namespace DataFlow.Core.Interfaces;

public interface IAppStateService
{
    int? ActiveProjectId { get; set; }
    int? ActiveDatasetId { get; set; }
    int? ActiveDashboardId { get; set; }
    int? ActiveChatSessionId { get; set; }
    string? SessionId { get; set; }
    bool SidebarCollapsed { get; set; }
    event EventHandler? StateChanged;
    void SetProject(int? projectId);
}
