using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using DataFlow.Core.Interfaces;
using DataFlow.Core.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace DataFlow.Infrastructure.Api;

public sealed class DataFlowApiClient : IDataFlowApiClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerSettings _json = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        }
    };

    public DataFlowApiClient(HttpClient http, IOptions<DataFlowApiOptions> options)
    {
        _http = http;
        var cfg = options.Value;
        _http.BaseAddress = new Uri(cfg.BaseUrl.TrimEnd('/') + "/");
        _http.Timeout = TimeSpan.FromSeconds(cfg.TimeoutSeconds);
    }

    public async Task<bool> HealthCheckAsync(CancellationToken ct = default)
    {
        try
        {
            var root = new Uri(_http.BaseAddress!, "/");
            using var response = await _http.GetAsync(root, ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<ProjectDto>> ListProjectsAsync(CancellationToken ct = default)
    {
        var list = await GetAsync<List<ProjectDto>>("projects/", ct).ConfigureAwait(false);
        return list ?? [];
    }

    public Task<ProjectDto> CreateProjectAsync(ProjectCreateRequest request, CancellationToken ct = default) =>
        PostAsync<ProjectDto>("projects/", request, ct)!;

    public Task<ProjectDto> GetProjectAsync(int id, CancellationToken ct = default) =>
        GetAsync<ProjectDto>($"projects/{id}", ct)!;

    public async Task DeleteProjectAsync(int id, CancellationToken ct = default)
    {
        using var response = await _http.DeleteAsync($"projects/{id}", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public Task<ProjectSummaryDto> GetProjectSummaryAsync(int id, CancellationToken ct = default) =>
        GetAsync<ProjectSummaryDto>($"projects/{id}/summary", ct)!;

    public async Task<IReadOnlyList<DatasetDto>> ListDatasetsAsync(int projectId, CancellationToken ct = default)
    {
        var list = await GetAsync<List<DatasetDto>>($"datasets/project/{projectId}", ct).ConfigureAwait(false);
        return list ?? [];
    }

    public async Task<DatasetDto> UploadDatasetAsync(int projectId, string filePath, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(filePath);
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(filePath));
        content.Add(fileContent, "file", Path.GetFileName(filePath));

        using var response = await _http.PostAsync($"datasets/project/{projectId}/upload", content, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return JsonConvert.DeserializeObject<DatasetDto>(json, _json)
               ?? throw new InvalidOperationException("Empty dataset response.");
    }

    public Task<ChartDataDto> GetChartDataAsync(int datasetId, CancellationToken ct = default) =>
        GetAsync<ChartDataDto>($"datasets/{datasetId}/chart-data", ct)!;

    public async Task<IReadOnlyList<DashboardDto>> ListDashboardsAsync(int projectId, CancellationToken ct = default)
    {
        var list = await GetAsync<List<DashboardDto>>($"dashboards/project/{projectId}", ct).ConfigureAwait(false);
        return list ?? [];
    }

    public Task<DashboardDto> GetDashboardAsync(int id, CancellationToken ct = default) =>
        GetAsync<DashboardDto>($"dashboards/{id}", ct)!;

    public Task<DashboardDto> CreateDashboardAsync(DashboardCreateRequest request, CancellationToken ct = default) =>
        PostAsync<DashboardDto>("dashboards/", MapDashboardCreate(request), ct)!;

    public Task<DashboardDto> UpdateDashboardAsync(int id, DashboardUpdateRequest request, CancellationToken ct = default) =>
        PatchAsync<DashboardDto>($"dashboards/{id}", request, ct)!;

    public async Task DeleteDashboardAsync(int id, CancellationToken ct = default)
    {
        using var response = await _http.DeleteAsync($"dashboards/{id}", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteDatasetAsync(int id, CancellationToken ct = default)
    {
        using var response = await _http.DeleteAsync($"datasets/{id}", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public Task<ReviveSessionDto> ReviveSessionAsync(int datasetId, CancellationToken ct = default) =>
        PostEmptyAsync<ReviveSessionDto>($"datasets/{datasetId}/revive-session", ct)!;

    public async Task<IReadOnlyList<ChatSessionDto>> ListChatSessionsAsync(int projectId, CancellationToken ct = default)
    {
        var list = await GetAsync<List<ChatSessionDto>>($"history/sessions/{projectId}", ct).ConfigureAwait(false);
        return list ?? [];
    }

    public Task<ChatSessionDto> CreateChatSessionAsync(ChatSessionCreateRequest request, CancellationToken ct = default) =>
        PostAsync<ChatSessionDto>("history/sessions", request, ct)!;

    public async Task UpdateChatSessionTitleAsync(int sessionId, string title, CancellationToken ct = default)
    {
        using var response = await _http.PatchAsync($"history/sessions/{sessionId}/title?title={Uri.EscapeDataString(title)}", null, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteChatSessionAsync(int sessionId, CancellationToken ct = default)
    {
        using var response = await _http.DeleteAsync($"history/sessions/{sessionId}", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<ChatMessageDto>> GetChatMessagesAsync(int sessionId, CancellationToken ct = default)
    {
        var list = await GetAsync<List<ChatMessageDto>>($"history/sessions/{sessionId}/messages", ct).ConfigureAwait(false);
        return list ?? [];
    }

    public Task AddChatMessageAsync(int sessionId, ChatMessageCreateRequest request, CancellationToken ct = default) =>
        PostAsync<ChatMessageDto>($"history/sessions/{sessionId}/messages", request, ct)!;

    public async Task<InsightsReportDto?> GetInsightsAsync(string aiSessionId, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(aiSessionId), "session_id");
        using var response = await _http.PostAsync("enterprise/insights", content, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var root = JObject.Parse(json);
        var ins = root["insights"];
        return ins == null ? null : JsonConvert.DeserializeObject<InsightsReportDto>(ins.ToString(), _json);
    }

    public async Task<DashboardLayoutDocument?> AutoGenerateDashboardAsync(string aiSessionId, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(aiSessionId), "session_id");
        using var response = await _http.PostAsync("enterprise/auto-dashboard", content, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var dash = JObject.Parse(json)["dashboard"];
        if (dash == null) return null;
        return MapEnterpriseDashboard(dash);
    }

    public async Task<IReadOnlyDictionary<string, object>> GetRealtimeStreamTypesAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync("enterprise/realtime/types", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var types = JObject.Parse(json)["types"] as JObject;
        if (types == null) return new Dictionary<string, object>();
        return types.Properties().ToDictionary(p => p.Name, p => (object)(p.Value.ToString() ?? ""));
    }

    public async Task<EnterpriseHealthDto?> GetEnterpriseHealthAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync("enterprise/health", ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return JsonConvert.DeserializeObject<EnterpriseHealthDto>(json, _json);
    }

    public Task<SemanticModelDto?> GetSemanticModelAsync(string aiSessionId, CancellationToken ct = default) =>
        PostFormAsync<SemanticModelDto>("enterprise/semantic-model", "session_id", aiSessionId, "model", ct);

    public Task<DashboardLayoutDocument?> BuildEnterpriseDashboardAsync(string aiSessionId, string layoutType = "executive", CancellationToken ct = default) =>
        PostFormDashboardAsync("enterprise/dashboard/build", aiSessionId, layoutType, ct);

    public async Task<DashboardThemesDto?> GetDashboardThemesAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync("enterprise/dashboard/themes", ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var root = JObject.Parse(json);
        return new DashboardThemesDto
        {
            Themes = root["themes"]?.ToObject<List<string>>() ?? []
        };
    }

    public async Task<ShareDashboardResultDto?> ShareDashboardAsync(string dashboardJson, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(dashboardJson), "dashboard_json");
        using var response = await _http.PostAsync("enterprise/dashboard/share", content, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var root = JObject.Parse(json);
        return new ShareDashboardResultDto
        {
            Token = root["token"]?.ToString() ?? "",
            Url = root["url"]?.ToString() ?? ""
        };
    }

    public async Task<DashboardLayoutDocument?> GetSharedDashboardAsync(string token, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"enterprise/dashboard/shared/{token}", ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var dash = JObject.Parse(json)["dashboard"];
        return dash == null ? null : MapEnterpriseDashboard(dash);
    }

    public async Task<ChartDataDto?> GetAdvancedChartsAsync(string aiSessionId, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(aiSessionId), "session_id");
        using var response = await _http.PostAsync("enterprise/advanced-charts", content, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var cd = JObject.Parse(json)["chart_data"];
        return cd == null ? null : JsonConvert.DeserializeObject<ChartDataDto>(cd.ToString(), _json);
    }

    public async Task<string?> ExportDashboardReportAsync(string dashboardJson, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(dashboardJson), "dashboard_json");
        using var response = await _http.PostAsync("enterprise/reports/export", content, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return JObject.Parse(json)["json"]?.ToString(Formatting.None);
    }

    public async Task<CacheStatsDto?> GetCacheStatsAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync("enterprise/cache/stats", ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var cache = JObject.Parse(json)["cache"];
        if (cache == null) return null;
        return new CacheStatsDto
        {
            Sessions = cache["sessions"]?.Value<int>() ?? 0,
            SharedDashboards = cache["shared_dashboards"]?.Value<int>() ?? 0
        };
    }

    private async Task<T?> PostFormAsync<T>(string path, string fieldName, string value, string resultKey, CancellationToken ct)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(value), fieldName);
        using var response = await _http.PostAsync(path, content, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return default;
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var token = JObject.Parse(json)[resultKey];
        return token == null ? default : JsonConvert.DeserializeObject<T>(token.ToString(), _json);
    }

    private async Task<DashboardLayoutDocument?> PostFormDashboardAsync(string path, string sessionId, string layoutType, CancellationToken ct)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(sessionId), "session_id");
        content.Add(new StringContent(layoutType), "layout_type");
        using var response = await _http.PostAsync(path, content, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var dash = JObject.Parse(json)["dashboard"];
        return dash == null ? null : MapEnterpriseDashboard(dash);
    }

    public async Task<ChatResponseDto> SendChatAsync(string message, string? sessionId, string? filePath, string? historyJson, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(message ?? ""), "message");
        if (!string.IsNullOrEmpty(sessionId))
            content.Add(new StringContent(sessionId), "session_id");
        if (!string.IsNullOrEmpty(historyJson))
            content.Add(new StringContent(historyJson), "history");

        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            await using var stream = File.OpenRead(filePath);
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(filePath));
            content.Add(fileContent, "file", Path.GetFileName(filePath));
        }

        using var response = await _http.PostAsync("chat/", content, ct).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return new ChatResponseDto
            {
                Status = "error",
                Errors = [new ApiErrorDto { Code = "HTTP", Message = body }]
            };
        }

        return JsonConvert.DeserializeObject<ChatResponseDto>(body, _json) ?? new ChatResponseDto();
    }

    private static object MapDashboardCreate(DashboardCreateRequest r) => new
    {
        project_id = r.ProjectId,
        dataset_id = r.DatasetId,
        name = r.Name,
        description = r.Description,
        scheme = r.Scheme,
        layout = r.Layout
    };

    private async Task<T?> GetAsync<T>(string path, CancellationToken ct)
    {
        using var response = await _http.GetAsync(path, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return DeserializeDashboardAware<T>(json);
    }

    private async Task<T?> PostEmptyAsync<T>(string path, CancellationToken ct)
    {
        using var response = await _http.PostAsync(path, null, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return JsonConvert.DeserializeObject<T>(json, _json);
    }

    private static DashboardLayoutDocument MapEnterpriseDashboard(JToken dash)
    {
        var doc = new DashboardLayoutDocument
        {
            Title = dash["title"]?.ToString(),
            Scheme = dash["scheme"]?.ToString() ?? "Metric Flow"
        };
        if (dash["widgets"] is not JArray widgets) return doc;

        foreach (var w in widgets)
        {
            var pos = w["position"];
            var widget = new DashboardWidgetModel
            {
                Id = w["id"]?.ToString() ?? Guid.NewGuid().ToString("N")[..8],
                Type = w["type"]?.ToString() ?? "bar",
                Title = w["title"]?.ToString() ?? w["metric"]?.ToString(),
                XCol = w["x_col"]?.ToString() ?? w["metric"]?.ToString(),
                YCol = w["y_col"]?.ToString() ?? w["metric"]?.ToString(),
                Gx = pos?["x"]?.Value<int>() ?? w["gx"]?.Value<int>() ?? 0,
                Gy = pos?["y"]?.Value<int>() ?? w["gy"]?.Value<int>() ?? 0,
                Gw = pos?["w"]?.Value<int>() ?? w["gw"]?.Value<int>() ?? 6,
                Gh = pos?["h"]?.Value<int>() ?? w["gh"]?.Value<int>() ?? 5
            };
            doc.Widgets.Add(widget);
        }
        return doc;
    }

    private async Task<T?> PostAsync<T>(string path, object body, CancellationToken ct)
    {
        var payload = JsonConvert.SerializeObject(body);
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync(path, content, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return DeserializeDashboardAware<T>(json);
    }

    private async Task<T?> PatchAsync<T>(string path, object body, CancellationToken ct)
    {
        var payload = JsonConvert.SerializeObject(body, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
        using var request = new HttpRequestMessage(HttpMethod.Patch, path)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return DeserializeDashboardAware<T>(json);
    }

    private T? DeserializeDashboardAware<T>(string json)
    {
        if (typeof(T) == typeof(DashboardDto) || typeof(T) == typeof(List<DashboardDto>))
        {
            var token = JToken.Parse(json);
            MapLayoutFields(token);
            json = token.ToString();
        }

        return JsonConvert.DeserializeObject<T>(json, _json);
    }

    private static void MapLayoutFields(JToken token)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
                var obj = (JObject)token;
                if (obj["layout"] != null && obj["layout_json"] == null)
                    obj["layout_json"] = obj["layout"]?.ToString(Formatting.None);
                foreach (var prop in obj.Properties().ToList())
                    MapLayoutFields(prop.Value);
                break;
            case JTokenType.Array:
                foreach (var item in token.Children())
                    MapLayoutFields(item);
                break;
        }
    }

    private static string GetMimeType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".csv" => "text/csv",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xls" => "application/vnd.ms-excel",
            ".json" => "application/json",
            ".tsv" => "text/tab-separated-values",
            ".parquet" => "application/octet-stream",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };
    }
}
