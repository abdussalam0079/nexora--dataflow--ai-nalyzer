using System.Collections.ObjectModel;
using DataFlow.Core.Interfaces;
using DataFlow.Core.Models;

namespace DataFlow.UI.Wpf.ViewModels;

public sealed class DashboardBuilderViewModel : BaseViewModel
{
    private readonly IDataFlowApiClient? _api;
    private int  _projectId;
    private int? _dashboardId;

    // ── State ─────────────────────────────────────────────────────
    private string  _dashTitle    = "New Dashboard";
    private string  _schemeName   = "Metric Flow";
    private string  _aiPrompt     = string.Empty;
    private bool    _generating   = false;
    private bool    _editMode     = true;
    private string  _saveStatus   = string.Empty;
    private string  _datasetInfo  = "Upload a file to begin";
    private bool    _hasData      = false;

    public string  DashTitle   { get => _dashTitle;   set => Set(ref _dashTitle, value); }
    public string  SchemeName  { get => _schemeName;  set => Set(ref _schemeName, value); }
    public string  AiPrompt    { get => _aiPrompt;    set => Set(ref _aiPrompt, value); }
    public bool    Generating  { get => _generating;  set { Set(ref _generating, value); GenerateCommand.RaiseCanExecuteChanged(); } }
    public bool    EditMode    { get => _editMode;    set => Set(ref _editMode, value); }
    public string  SaveStatus  { get => _saveStatus;  set => Set(ref _saveStatus, value); }
    public string  DatasetInfo { get => _datasetInfo; set => Set(ref _datasetInfo, value); }
    public bool    HasData     { get => _hasData;     set => Set(ref _hasData, value); }

    public ChartDataDto?   RawData { get; private set; }
    public DataSchemaInfo? Schema  { get; private set; }

    public ObservableCollection<DashboardWidgetModel> Widgets { get; } = [];

    // ── Commands ──────────────────────────────────────────────────
    public AsyncRelayCommand  SaveCommand     { get; }
    public AsyncRelayCommand  GenerateCommand { get; }
    public RelayCommand       ToggleEditMode  { get; }
    public RelayCommand       UploadCommand   { get; }

    public DashboardBuilderViewModel(IDataFlowApiClient? api)
    {
        _api = api;
        SaveCommand     = new AsyncRelayCommand(SaveAsync);
        GenerateCommand = new AsyncRelayCommand(GenerateAsync, () => !Generating && !string.IsNullOrWhiteSpace(AiPrompt));
        ToggleEditMode  = new RelayCommand(() => EditMode = !EditMode);
        UploadCommand   = new RelayCommand(() => UploadRequested?.Invoke(this, EventArgs.Empty));
    }

    public async Task LoadDashboardAsync(int projectId, int dashboardId)
    {
        _projectId   = projectId;
        _dashboardId = dashboardId;

        if (_api == null) return;
        try
        {
            // Load dataset
            var datasets = await _api.ListDatasetsAsync(projectId);
            var ds = datasets.FirstOrDefault();
            if (ds != null)
            {
                RawData = await _api.GetChartDataAsync(ds.Id);
                Schema  = ComputeSchema(RawData);
                DatasetInfo = $"{ds.FileName}  ·  {RawData.Rows.Count:N0} rows  ·  {RawData.Headers.Count} cols";
                HasData = true;
            }

            // Load dashboard layout
            var dashboard = await _api.GetDashboardAsync(dashboardId);
            DashTitle  = dashboard.Name;
            SchemeName = dashboard.Scheme;

            if (!string.IsNullOrEmpty(dashboard.LayoutJson))
            {
                try
                {
                    var layout = Newtonsoft.Json.JsonConvert.DeserializeObject<DashboardLayoutDocument>(dashboard.LayoutJson);
                    if (layout?.Widgets?.Count > 0)
                    {
                        Widgets.Clear();
                        foreach (var w in layout.Widgets) Widgets.Add(w);
                        EditMode = false;
                    }
                }
                catch { /* ignore */ }
            }
        }
        catch { /* ignore */ }
    }

    public void LoadFile(ChartDataDto data, string fileName)
    {
        RawData     = data;
        Schema      = ComputeSchema(data);
        DatasetInfo = $"{fileName}  ·  {data.Rows.Count:N0} rows  ·  {data.Headers.Count} cols";
        HasData     = true;
        GenerateCommand.RaiseCanExecuteChanged();
    }

    private async Task GenerateAsync()
    {
        if (Schema == null) return;
        Generating = true;
        await Task.Delay(50);

        try
        {
            var p = AiPrompt.ToLower();
            var newWidgets = new List<DashboardWidgetModel>();

            void Add(string type, string title, string? xCol = null, string? yCol = null)
            {
                newWidgets.Add(new DashboardWidgetModel
                {
                    Id = $"w_{DateTime.Now.Ticks}_{newWidgets.Count}",
                    Type = type, Title = title,
                    XCol = xCol ?? Schema.Categorical.FirstOrDefault() ?? "",
                    YCol = yCol ?? Schema.Numeric.FirstOrDefault() ?? "",
                    Aggregation = "sum"
                });
            }

            var kpiMatch = System.Text.RegularExpressions.Regex.Match(AiPrompt, @"(\d+)\s*kpi", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            int kpiCount = kpiMatch.Success ? int.Parse(kpiMatch.Groups[1].Value) : (p.Contains("kpi") || p.Contains("card") ? 4 : 0);
            var kpiLabels = new[] { "Total Revenue", "Total Orders", "Active Users", "Conversion Rate" };
            for (int i = 0; i < Math.Min(kpiCount, 4); i++)
                Add("kpi", kpiLabels[i], null, Schema.Numeric.ElementAtOrDefault(i) ?? Schema.Numeric.FirstOrDefault());

            if (p.Contains("line") || p.Contains("trend"))  Add("line",    "Trend Over Time");
            if (p.Contains("area"))                          Add("area",    "Performance Area");
            if (p.Contains("bar")  || p.Contains("column")) Add("bar",     "Category Comparison");
            if (p.Contains("pie")  || p.Contains("donut"))  Add("pie",     "Distribution");
            if (p.Contains("scatter"))                       Add("scatter", "Correlation", Schema.Numeric.FirstOrDefault(), Schema.Numeric.ElementAtOrDefault(1));
            if (p.Contains("rank") || p.Contains("top"))    Add("ranking", "Top Rankings");
            if (p.Contains("table"))                         Add("table",   "Data Table");

            if (newWidgets.Count == 0)
            {
                Add("kpi",  "Key Metric");
                Add("bar",  "Category Comparison");
                Add("line", "Trend Over Time");
            }

            Widgets.Clear();
            foreach (var w in newWidgets) Widgets.Add(w);
            EditMode = false;
        }
        finally { Generating = false; }
    }

    private async Task SaveAsync()
    {
        if (_api == null || Widgets.Count == 0) return;
        SaveStatus = "saving";
        try
        {
            var layout = new DashboardLayoutDocument
            {
                Title = DashTitle, Scheme = SchemeName,
                Widgets = Widgets.ToList()
            };
            var req = new DashboardUpdateRequest
            {
                Name   = DashTitle,
                Scheme = SchemeName,
                Layout = layout
            };
            if (_dashboardId.HasValue)
                await _api.UpdateDashboardAsync(_dashboardId.Value, req);
            SaveStatus = "saved";
            await Task.Delay(2500);
            SaveStatus = string.Empty;
        }
        catch { SaveStatus = "error"; }
    }

    private static DataSchemaInfo ComputeSchema(ChartDataDto data)
    {
        var numeric = data.Headers.Where(h =>
            data.Rows.Take(20).Any(r => r.GetValueOrDefault(h) is double or int or long)).ToList();
        var categorical = data.Headers.Except(numeric).ToList();
        return new DataSchemaInfo { Numeric = numeric, Categorical = categorical, All = data.Headers };
    }

    // ── Events ────────────────────────────────────────────────────
    public event EventHandler? UploadRequested;
}
