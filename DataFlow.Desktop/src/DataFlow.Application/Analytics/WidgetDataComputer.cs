using DataFlow.Core.Models;

namespace DataFlow.Application.Analytics;

public static class WidgetDataComputer
{
    public static IReadOnlyList<Dictionary<string, object?>> Compute(
        ChartDataDto? rawData,
        DashboardWidgetModel cfg,
        IEnumerable<DashboardFilterModel> filters)
    {
        if (rawData?.Rows is not { Count: > 0 })
            return [];

        var rows = rawData.Rows.Select(r => new Dictionary<string, object?>(r)).ToList();

        foreach (var f in filters)
        {
            rows = rows.Where(r => MatchFilter(r, f)).ToList();
        }

        if (cfg.Type == "table")
        {
            var cols = !string.IsNullOrWhiteSpace(cfg.SortBy)
                ? rawData.Headers
                : rawData.Headers;

            var result = rows.Select(r =>
            {
                var o = new Dictionary<string, object?>();
                foreach (var c in cols)
                    o[c] = r.GetValueOrDefault(c);
                return o;
            }).ToList();

            if (!string.IsNullOrWhiteSpace(cfg.SortBy))
            {
                result.Sort((a, b) => CompareSort(a, b, cfg.SortBy!, cfg.SortDir));
            }

            return result;
        }

        if (cfg.Type == "kpi")
        {
            var col = !string.IsNullOrWhiteSpace(cfg.YCol) ? cfg.YCol : cfg.XCol;
            if (string.IsNullOrWhiteSpace(col))
                return [new Dictionary<string, object?> { ["value"] = rows.Count }];

            var nums = rows.Select(r => ToDouble(r.GetValueOrDefault(col))).ToList();
            var value = Aggregate(nums, cfg.Aggregation, rows.Count);
            return [new Dictionary<string, object?> { ["value"] = Math.Round(value, 2) }];
        }

        if (cfg.Type == "scatter")
        {
            if (string.IsNullOrWhiteSpace(cfg.XCol) || string.IsNullOrWhiteSpace(cfg.YCol))
                return [];

            return rows.Take(200).Select(r => new Dictionary<string, object?>
            {
                ["x"] = ToDouble(r.GetValueOrDefault(cfg.XCol)),
                ["y"] = ToDouble(r.GetValueOrDefault(cfg.YCol))
            }).ToList();
        }

        if (string.IsNullOrWhiteSpace(cfg.XCol))
            return [];

        var groups = rows.GroupBy(r => Convert.ToString(r.GetValueOrDefault(cfg.XCol)) ?? "(empty)")
            .ToDictionary(g => g.Key, g => g.ToList());

        var points = new List<Dictionary<string, object?>>();
        foreach (var (name, gRows) in groups)
        {
            double value;
            if (string.IsNullOrWhiteSpace(cfg.YCol))
            {
                value = gRows.Count;
            }
            else
            {
                var nums = gRows.Select(r => ToDouble(r.GetValueOrDefault(cfg.YCol))).ToList();
                value = Aggregate(nums, cfg.Aggregation, gRows.Count);
            }

            points.Add(new Dictionary<string, object?>
            {
                ["name"] = name,
                ["value"] = Math.Round(value, 2)
            });
        }

        points.Sort((a, b) => ToDouble(b["value"]).CompareTo(ToDouble(a["value"])));
        if (cfg.TopN is > 0)
            points = points.Take(cfg.TopN.Value).ToList();

        return points;
    }

    public static IReadOnlyList<DataPointRow> AsPoints(IReadOnlyList<Dictionary<string, object?>> data, string type)
    {
        if (type == "scatter")
        {
            return data.Select(d => new DataPointRow
            {
                X = ToDouble(d.GetValueOrDefault("x")),
                Y = ToDouble(d.GetValueOrDefault("y"))
            }).ToList();
        }

        return data.Select(d => new DataPointRow
        {
            Name = Convert.ToString(d.GetValueOrDefault("name")) ?? "",
            Value = ToDouble(d.GetValueOrDefault("value"))
        }).ToList();
    }

    private static bool MatchFilter(Dictionary<string, object?> row, DashboardFilterModel f)
    {
        var v = row.GetValueOrDefault(f.Col);
        var sv = (Convert.ToString(v) ?? "").ToLowerInvariant();
        var fv = f.Val.ToLowerInvariant();

        return f.Op switch
        {
            "=" => sv == fv,
            "!=" => sv != fv,
            ">" => ToDouble(v) > ToDouble(f.Val),
            "<" => ToDouble(v) < ToDouble(f.Val),
            ">=" => ToDouble(v) >= ToDouble(f.Val),
            "<=" => ToDouble(v) <= ToDouble(f.Val),
            "contains" => sv.Contains(fv),
            _ => true
        };
    }

    private static double Aggregate(List<double> nums, string agg, int rowCount) =>
        agg.ToLowerInvariant() switch
        {
            "avg" => nums.Count > 0 ? nums.Sum() / nums.Count : 0,
            "count" => rowCount,
            "min" => nums.Count > 0 ? nums.Min() : 0,
            "max" => nums.Count > 0 ? nums.Max() : 0,
            _ => nums.Sum()
        };

    private static int CompareSort(Dictionary<string, object?> a, Dictionary<string, object?> b, string col, string dir)
    {
        var av = a.GetValueOrDefault(col);
        var bv = b.GetValueOrDefault(col);
        var na = ToDouble(av);
        var nb = ToDouble(bv);
        if (!double.IsNaN(na) && !double.IsNaN(nb))
            return dir == "asc" ? na.CompareTo(nb) : nb.CompareTo(na);

        var sa = Convert.ToString(av) ?? "";
        var sb = Convert.ToString(bv) ?? "";
        return dir == "asc" ? string.Compare(sa, sb, StringComparison.OrdinalIgnoreCase)
            : string.Compare(sb, sa, StringComparison.OrdinalIgnoreCase);
    }

    private static double ToDouble(object? value)
    {
        if (value is null) return 0;
        if (value is double d) return d;
        if (value is float f) return f;
        if (value is int i) return i;
        if (value is long l) return l;
        return double.TryParse(Convert.ToString(value), out var n) ? n : 0;
    }
}
