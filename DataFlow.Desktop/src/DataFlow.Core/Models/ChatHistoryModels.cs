namespace DataFlow.Core.Models;

public sealed class ChatSessionCreateRequest
{
    public int? ProjectId { get; set; }
    public int? DatasetId { get; set; }
    public string? SessionId { get; set; }
    public string? Title { get; set; }
}

public sealed class ChatMessageCreateRequest
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public bool HasChart { get; set; }
    public object? ChartJson { get; set; }
}

public sealed class ReviveSessionDto
{
    public string? AiSessionId { get; set; }
    public string? ProfileContext { get; set; }
    public bool Revived { get; set; }
}
