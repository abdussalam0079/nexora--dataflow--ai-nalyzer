namespace DataFlow.Core.Models;

public sealed class DashboardDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int? DatasetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Scheme { get; set; } = "Metric Flow";
    public string? LayoutJson { get; set; }
    public bool IsPinned { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class DashboardCreateRequest
{
    public int ProjectId { get; set; }
    public int? DatasetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Scheme { get; set; } = "Metric Flow";
    public object? Layout { get; set; }
}

public sealed class DashboardUpdateRequest
{
    public string? Name { get; set; }
    public string? Scheme { get; set; }
    public object? Layout { get; set; }
    public bool? IsPinned { get; set; }
}
