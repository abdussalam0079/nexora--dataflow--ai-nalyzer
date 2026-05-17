namespace DataFlow.Core.Models;

public sealed class DashboardLayoutDocument
{
    public string? Title { get; set; }
    public string? Scheme { get; set; }
    public List<DashboardWidgetModel> Widgets { get; set; } = [];
}

public sealed class DashboardWidgetModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Type { get; set; } = "bar";
    public string? Title { get; set; }
    public string? XCol { get; set; }
    public string? YCol { get; set; }
    public string Aggregation { get; set; } = "sum";
    public int? TopN { get; set; }
    public double? Change { get; set; }
    public double? Threshold { get; set; }
    public string? SortBy { get; set; }
    public string SortDir { get; set; } = "desc";
    public int Gx { get; set; }
    public int Gy { get; set; }
    public int Gw { get; set; } = 6;
    public int Gh { get; set; } = 5;
}

public sealed class DashboardFilterModel
{
    public long Id { get; set; } = DateTime.UtcNow.Ticks;
    public string Col { get; set; } = string.Empty;
    public string Op { get; set; } = "=";
    public string Val { get; set; } = string.Empty;
}

public sealed class DataPointRow
{
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
}

public sealed class DataSchemaInfo
{
    public List<string> Numeric { get; set; } = [];
    public List<string> Dates { get; set; } = [];
    public List<string> Categorical { get; set; } = [];
    public List<string> All { get; set; } = [];
}
