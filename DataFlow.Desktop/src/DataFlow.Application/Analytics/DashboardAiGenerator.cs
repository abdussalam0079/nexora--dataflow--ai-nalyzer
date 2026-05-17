using DataFlow.Core.Models;

namespace DataFlow.Application.Analytics;

/// <summary>Client-side dashboard layout generation from natural language (mirrors React handleGenerate).</summary>
public static class DashboardAiGenerator
{
    public static List<DashboardWidgetModel> GenerateFromPrompt(string prompt, DataSchemaInfo schema)
    {
        var p = prompt.ToLowerInvariant();
        var widgets = new List<DashboardWidgetModel>();
        var row = 0;
        var col = 0;

        var numKpi = ParseKpiCount(p);
        var kpiLabels = new[] { "Total Revenue", "Total Orders", "Active Users", "Conversion Rate", "Avg Order Value", "Profit Margin" };
        var numCols = schema.Numeric;

        for (var i = 0; i < numKpi && i < 6; i++)
        {
            AddWidget(widgets, ref row, ref col, "kpi", kpiLabels[i], schema, new DashboardWidgetModel
            {
                YCol = numCols.ElementAtOrDefault(i) ?? numCols.FirstOrDefault(),
                Aggregation = i == 3 ? "avg" : "sum"
            });
        }

        if (RegexMatch(p, @"line|trend|time|monthly|weekly|daily|over"))
            AddWidget(widgets, ref row, ref col, "line", "Trend Over Time", schema);
        if (RegexMatch(p, @"area|filled|shaded"))
            AddWidget(widgets, ref row, ref col, "area", "Performance Area", schema);
        if (RegexMatch(p, @"bar|column|categ|product|region|compar"))
            AddWidget(widgets, ref row, ref col, "bar", "Category Comparison", schema);
        if (RegexMatch(p, @"pie|donut|ring|distribution|share"))
            AddWidget(widgets, ref row, ref col, "pie", "Distribution", schema);
        if (RegexMatch(p, @"radar|spider|performance"))
            AddWidget(widgets, ref row, ref col, "radar", "Performance Radar", schema);
        if (RegexMatch(p, @"scatter|correl|vs\b"))
            AddWidget(widgets, ref row, ref col, "scatter", "Correlation", schema, new DashboardWidgetModel
            {
                XCol = numCols.ElementAtOrDefault(0),
                YCol = numCols.ElementAtOrDefault(1)
            });
        if (RegexMatch(p, @"rank|top|list"))
            AddWidget(widgets, ref row, ref col, "ranking", "Top Rankings", schema);
        if (RegexMatch(p, @"table"))
            AddWidget(widgets, ref row, ref col, "table", "Data Table", schema, new DashboardWidgetModel
            {
                SortBy = numCols.FirstOrDefault(),
                SortDir = "desc"
            });

        if (widgets.Count == 0)
        {
            AddWidget(widgets, ref row, ref col, "kpi", "Key Metric", schema, new DashboardWidgetModel { YCol = numCols.FirstOrDefault() });
            AddWidget(widgets, ref row, ref col, "bar", "Category Comparison", schema);
            AddWidget(widgets, ref row, ref col, "line", "Trend Over Time", schema);
        }

        return widgets;
    }

    private static int ParseKpiCount(string p)
    {
        var m = System.Text.RegularExpressions.Regex.Match(p, @"(\d+)\s*kpi");
        if (m.Success) return int.Parse(m.Groups[1].Value);
        return RegexMatch(p, @"kpi|card|metric|score") ? 4 : 0;
    }

    private static bool RegexMatch(string p, string pattern) =>
        System.Text.RegularExpressions.Regex.IsMatch(p, pattern);

    private static void AddWidget(
        List<DashboardWidgetModel> list,
        ref int row,
        ref int col,
        string type,
        string title,
        DataSchemaInfo schema,
        DashboardWidgetModel? extra = null)
    {
        var info = ChartTypeCatalog.Get(type);
        var gw = info?.DefaultWidth ?? 6;
        var gh = info?.DefaultHeight ?? 5;
        if (col + gw > 12) { col = 0; row += gh; }

        var w = extra ?? new DashboardWidgetModel();
        w.Id = $"w_{DateTime.UtcNow.Ticks}_{list.Count}";
        w.Type = type;
        w.Title = title;
        w.XCol ??= schema.Categorical.FirstOrDefault() ?? schema.All.FirstOrDefault();
        w.YCol ??= schema.Numeric.FirstOrDefault();
        w.Aggregation ??= "sum";
        w.Gx = col;
        w.Gy = row;
        w.Gw = gw;
        w.Gh = gh;
        list.Add(w);
        col += gw;
        if (col >= 12) { col = 0; row += gh; }
    }
}
