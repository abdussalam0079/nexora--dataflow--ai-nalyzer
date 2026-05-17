using DataFlow.Core.Models;

namespace DataFlow.Core.Interfaces;

/// <summary>Embedded C# analytics — works on local CSV/TSV without Python.</summary>
public interface ILocalAnalyticsService
{
    ChartDataDto LoadFromFile(string filePath, int maxRows = 5000);
    DataSchemaInfo ComputeSchema(ChartDataDto data);
    InsightsReportDto ComputeBasicInsights(ChartDataDto data);
    bool CanParse(string filePath);
}
