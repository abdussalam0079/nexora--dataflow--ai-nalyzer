namespace DataFlow.Core.Models;

public sealed class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#6366f1";
    public string Icon { get; set; } = "📊";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int DatasetCount { get; set; }
    public int DashboardCount { get; set; }
    public int ChatCount { get; set; }
}

public sealed class ProjectCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#6366f1";
    public string Icon { get; set; } = "📊";
}

public sealed class ProjectSummaryDto
{
    public List<DashboardSummaryItem> Dashboards { get; set; } = [];
    public List<ChatSummaryItem> Chats { get; set; } = [];
}

public sealed class DashboardSummaryItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
}

public sealed class ChatSummaryItem
{
    public int Id { get; set; }
    public string? Title { get; set; }
}
