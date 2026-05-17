namespace DataFlow.Core.Models;

public sealed class InsightsReportDto
{
    public List<InsightItemDto> Trends { get; set; } = [];
    public List<InsightItemDto> Correlations { get; set; } = [];
    public List<InsightItemDto> Anomalies { get; set; } = [];
    public List<InsightItemDto> Seasonality { get; set; } = [];
    public List<InsightItemDto> KpiAlerts { get; set; } = [];
    public int TotalInsights { get; set; }
    public int HighPriority { get; set; }
    public string? SummaryText { get; set; }
}

public sealed class InsightItemDto
{
    public string? Message { get; set; }
    public string? Column { get; set; }
    public string? Severity { get; set; }
    public double? ChangePct { get; set; }
    public double? Correlation { get; set; }
    public string? ColA { get; set; }
    public string? ColB { get; set; }
    public string? Kpi { get; set; }
}
