using DataFlow.Core.Enums;

namespace DataFlow.Core.Navigation;

public sealed class NavigationArgs
{
    public AppView View { get; init; }
    public int? ProjectId { get; init; }
    public int? DashboardId { get; init; }
    public int? ChatSessionId { get; init; }
    public int? DatasetId { get; init; }

    public static NavigationArgs For(AppView view, int? projectId = null, int? dashboardId = null, int? chatSessionId = null) =>
        new()
        {
            View = view,
            ProjectId = projectId,
            DashboardId = dashboardId,
            ChatSessionId = chatSessionId
        };
}

public sealed class NavigationChangedEventArgs : EventArgs
{
    public required NavigationArgs Args { get; init; }
}
