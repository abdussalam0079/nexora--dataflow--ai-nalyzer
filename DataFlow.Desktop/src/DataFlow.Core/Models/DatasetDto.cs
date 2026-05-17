namespace DataFlow.Core.Models;

public sealed class DatasetDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public int ColCount { get; set; }
    public long SizeBytes { get; set; }
    public string? SessionId { get; set; }
    public string? AiSessionId { get; set; }
    public string? ProfileContext { get; set; }
    public string? SchemaJson { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class ChartDataDto
{
    public List<string> Headers { get; set; } = [];
    public List<Dictionary<string, object?>> Rows { get; set; } = [];
}
