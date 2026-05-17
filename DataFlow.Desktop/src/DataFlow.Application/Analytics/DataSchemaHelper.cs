using DataFlow.Core.Models;

namespace DataFlow.Application.Analytics;

public static class DataSchemaHelper
{
    public static DataSchemaInfo ComputeSchema(ChartDataDto? raw)
    {
        if (raw?.Headers is not { Count: > 0 })
            return new DataSchemaInfo();

        var numeric = raw.Headers.Where(h =>
            raw.Rows.Take(20).Any(r => IsNumeric(r.GetValueOrDefault(h)))).ToList();

        var dates = raw.Headers.Where(h =>
            raw.Rows.Take(5).Any(r =>
            {
                var s = Convert.ToString(r.GetValueOrDefault(h)) ?? "";
                return s.Contains("202", StringComparison.Ordinal) || s.Contains('/');
            })).ToList();

        var categorical = raw.Headers.Where(h => !numeric.Contains(h)).ToList();

        return new DataSchemaInfo
        {
            Numeric = numeric,
            Dates = dates,
            Categorical = categorical,
            All = raw.Headers.ToList()
        };
    }

    private static bool IsNumeric(object? value)
    {
        if (value is null) return false;
        if (value is int or long or float or double or decimal) return true;
        var s = Convert.ToString(value)?.Trim();
        return !string.IsNullOrEmpty(s) && double.TryParse(s, out _);
    }
}
