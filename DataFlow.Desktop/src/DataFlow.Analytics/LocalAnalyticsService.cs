using System.Globalization;
using System.Text;
using DataFlow.Application.Analytics;
using DataFlow.Core.Interfaces;
using DataFlow.Core.Models;

namespace DataFlow.Analytics;

public sealed class LocalAnalyticsService : ILocalAnalyticsService
{
    public bool CanParse(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext is ".csv" or ".tsv" or ".txt";
    }

    public ChartDataDto LoadFromFile(string filePath, int maxRows = 5000)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext is not (".csv" or ".tsv" or ".txt"))
            throw new NotSupportedException($"Local analytics supports CSV/TSV only. Use the API for {ext} files.");

        var delimiter = ext == ".tsv" ? '\t' : DetectDelimiter(filePath);
        var lines = File.ReadLines(filePath).Take(maxRows + 1).ToList();
        if (lines.Count == 0)
            return new ChartDataDto();

        var headers = SplitCsvLine(lines[0], delimiter).Select(h => h.Trim()).ToList();
        var rows = new List<Dictionary<string, object?>>();

        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cells = SplitCsvLine(line, delimiter);
            var row = new Dictionary<string, object?>();
            for (var i = 0; i < headers.Count; i++)
            {
                var raw = i < cells.Count ? cells[i].Trim() : "";
                row[headers[i]] = ParseCell(raw);
            }
            rows.Add(row);
        }

        return new ChartDataDto { Headers = headers, Rows = rows };
    }

    public DataSchemaInfo ComputeSchema(ChartDataDto data) => DataSchemaHelper.ComputeSchema(data);

    public InsightsReportDto ComputeBasicInsights(ChartDataDto data)
    {
        var schema = ComputeSchema(data);
        var report = new InsightsReportDto();

        foreach (var col in schema.Numeric)
        {
            var values = data.Rows
                .Select(r => r.GetValueOrDefault(col))
                .Where(v => v is not null)
                .Select(v => Convert.ToDouble(v))
                .ToList();

            if (values.Count < 2) continue;

            var mean = values.Average();
            var max = values.Max();
            var min = values.Min();
            report.Trends.Add(new InsightItemDto
            {
                Column = col,
                Message = $"{col}: avg {mean:N1}, range {min:N1}–{max:N1} ({values.Count} values)",
                Severity = "info"
            });

            var outliers = values.Count(v => Math.Abs(v - mean) > 2 * StdDev(values));
            if (outliers > 0)
            {
                report.Anomalies.Add(new InsightItemDto
                {
                    Column = col,
                    Message = $"{outliers} potential outlier(s) in {col}",
                    Severity = "warning"
                });
            }
        }

        for (var i = 0; i < schema.Numeric.Count; i++)
        {
            for (var j = i + 1; j < schema.Numeric.Count; j++)
            {
                var a = schema.Numeric[i];
                var b = schema.Numeric[j];
                var corr = Pearson(data, a, b);
                if (Math.Abs(corr) >= 0.6)
                {
                    report.Correlations.Add(new InsightItemDto
                    {
                        ColA = a,
                        ColB = b,
                        Correlation = corr,
                        Message = $"{a} ↔ {b}: correlation {corr:F2}",
                        Severity = Math.Abs(corr) >= 0.85 ? "high" : "info"
                    });
                }
            }
        }

        report.TotalInsights = report.Trends.Count + report.Correlations.Count + report.Anomalies.Count;
        report.HighPriority = report.Anomalies.Count + report.Correlations.Count(c => c.Severity == "high");
        report.SummaryText = report.TotalInsights > 0
            ? $"Local analysis found {report.TotalInsights} insight(s) across {schema.Numeric.Count} numeric column(s)."
            : "Upload more numeric data for deeper insights (AI insights require the API server).";

        return report;
    }

    private static char DetectDelimiter(string path)
    {
        var line = File.ReadLines(path).FirstOrDefault() ?? "";
        var commas = line.Count(c => c == ',');
        var tabs = line.Count(c => c == '\t');
        return tabs > commas ? '\t' : ',';
    }

    private static List<string> SplitCsvLine(string line, char delimiter)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"') inQuotes = !inQuotes;
            else if (ch == delimiter && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else sb.Append(ch);
        }
        result.Add(sb.ToString());
        return result;
    }

    private static object? ParseCell(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
        if (double.TryParse(raw, out d)) return d;
        return raw;
    }

    private static double StdDev(List<double> values)
    {
        if (values.Count < 2) return 0;
        var avg = values.Average();
        return Math.Sqrt(values.Sum(v => (v - avg) * (v - avg)) / values.Count);
    }

    private static double Pearson(ChartDataDto data, string colA, string colB)
    {
        var pairs = data.Rows
            .Select(r => (
                A: ToDouble(r.GetValueOrDefault(colA)),
                B: ToDouble(r.GetValueOrDefault(colB))))
            .Where(p => p.A.HasValue && p.B.HasValue)
            .Select(p => (p.A!.Value, p.B!.Value))
            .ToList();

        if (pairs.Count < 3) return 0;
        var meanA = pairs.Average(p => p.Item1);
        var meanB = pairs.Average(p => p.Item2);
        var num = pairs.Sum(p => (p.Item1 - meanA) * (p.Item2 - meanB));
        var den = Math.Sqrt(pairs.Sum(p => (p.Item1 - meanA) * (p.Item1 - meanA)) *
                            pairs.Sum(p => (p.Item2 - meanB) * (p.Item2 - meanB)));
        return den == 0 ? 0 : num / den;
    }

    private static double? ToDouble(object? v)
    {
        if (v is null) return null;
        if (v is double d) return d;
        if (v is int or long or float or decimal) return Convert.ToDouble(v);
        return double.TryParse(Convert.ToString(v), out var x) ? x : null;
    }
}
