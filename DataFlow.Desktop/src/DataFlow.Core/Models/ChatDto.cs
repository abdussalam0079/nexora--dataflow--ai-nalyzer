namespace DataFlow.Core.Models;

public sealed class ChatSessionDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int? DatasetId { get; set; }
    public string? Title { get; set; }
    public string? SessionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int MessageCount { get; set; }
}

public sealed class ChatMessageDto
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public bool HasChart { get; set; }
    public string? ChartJson { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class ChatResponseDto
{
    public string Status { get; set; } = "ok";
    public string? Answer { get; set; }
    public string? Source { get; set; }
    public string? SessionId { get; set; }
    public List<string> Warnings { get; set; } = [];
    public List<ApiErrorDto> Errors { get; set; } = [];
}

public sealed class ApiErrorDto
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? FixHint { get; set; }
}
