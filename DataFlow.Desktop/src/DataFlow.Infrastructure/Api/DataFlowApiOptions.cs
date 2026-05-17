namespace DataFlow.Infrastructure.Api;

public sealed class DataFlowApiOptions
{
    public const string SectionName = "Api";
    public string BaseUrl { get; set; } = "http://127.0.0.1:8000/api/v1";
    public int TimeoutSeconds { get; set; } = 120;
}
