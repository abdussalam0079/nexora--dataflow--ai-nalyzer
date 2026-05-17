namespace DataFlow.Application.Analytics;

public sealed record ChartTypeInfo(string Id, string Label, int DefaultWidth, int DefaultHeight);

public static class ChartTypeCatalog
{
    public static IReadOnlyList<ChartTypeInfo> All { get; } =
    [
        new("kpi", "KPI Card", 3, 3),
        new("bar", "Bar", 6, 5),
        new("line", "Line", 6, 5),
        new("area", "Area", 6, 5),
        new("pie", "Pie", 5, 5),
        new("donut", "Donut", 5, 5),
        new("scatter", "Scatter", 6, 5),
        new("radar", "Radar", 5, 5),
        new("table", "Table", 12, 7),
        new("ranking", "Ranking", 4, 5),
    ];

    public static ChartTypeInfo? Get(string id) =>
        All.FirstOrDefault(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}
