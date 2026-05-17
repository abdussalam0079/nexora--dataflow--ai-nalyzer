namespace DataFlow.Core.Models;

public sealed class EnterpriseHealthDto
{
    public string Status { get; set; } = "ok";
    public List<string> Modules { get; set; } = [];
}

public sealed class SemanticModelDto
{
    public List<SemanticEntityDto> Entities { get; set; } = [];
    public List<SemanticMeasureDto> Measures { get; set; } = [];
}

public sealed class SemanticEntityDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public sealed class SemanticMeasureDto
{
    public string Name { get; set; } = string.Empty;
    public string Aggregation { get; set; } = "sum";
}

public sealed class DashboardThemesDto
{
    public List<string> Themes { get; set; } = [];
}

public sealed class ShareDashboardResultDto
{
    public string Token { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public sealed class CacheStatsDto
{
    public int Sessions { get; set; }
    public int SharedDashboards { get; set; }
}
