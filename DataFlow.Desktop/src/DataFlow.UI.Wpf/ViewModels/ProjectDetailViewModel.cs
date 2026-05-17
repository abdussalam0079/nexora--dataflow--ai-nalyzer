using System.Collections.ObjectModel;
using DataFlow.Core.Enums;
using DataFlow.Core.Interfaces;
using DataFlow.Core.Models;
using DataFlow.Core.Navigation;

namespace DataFlow.UI.Wpf.ViewModels;

public sealed class ProjectDetailViewModel : BaseViewModel
{
    private readonly IDataFlowApiClient _api;
    private readonly INavigationService _nav;
    private readonly IAppStateService   _state;

    private int _projectId;

    // ── State ─────────────────────────────────────────────────────
    private ProjectDto?  _project;
    private DatasetDto?  _dataset;
    private bool         _loading       = true;
    private bool         _uploading     = false;
    private double       _uploadPct     = 0;
    private string       _uploadError   = string.Empty;
    private bool         _hasUploadError = false;

    public ProjectDto?  Project       { get => _project;       set => Set(ref _project, value); }
    public DatasetDto?  Dataset       { get => _dataset;       set { Set(ref _dataset, value); Notify(nameof(HasDataset)); } }
    public bool         Loading       { get => _loading;       set => Set(ref _loading, value); }
    public bool         Uploading     { get => _uploading;     set => Set(ref _uploading, value); }
    public double       UploadPct     { get => _uploadPct;     set => Set(ref _uploadPct, value); }
    public string       UploadError   { get => _uploadError;   set => Set(ref _uploadError, value); }
    public bool         HasUploadError{ get => _hasUploadError;set => Set(ref _hasUploadError, value); }
    public bool         HasDataset    => _dataset != null;

    public ObservableCollection<DashboardDto> Dashboards { get; } = [];

    // ── Commands ──────────────────────────────────────────────────
    public RelayCommand       BackCommand           { get; }
    public RelayCommand       UploadCommand         { get; }
    public RelayCommand       ReplaceCommand        { get; }
    public RelayCommand       SchemaCommand         { get; }
    public AsyncRelayCommand  DeleteDatasetCommand  { get; }
    public RelayCommand       NewDashboardCommand   { get; }
    public RelayCommand<DashboardDto> OpenDashboardCommand   { get; }
    public RelayCommand<DashboardDto> DeleteDashboardCommand { get; }

    public ProjectDetailViewModel(IDataFlowApiClient api, INavigationService nav, IAppStateService state)
    {
        _api   = api;
        _nav   = nav;
        _state = state;

        BackCommand           = new RelayCommand(() => _nav.Navigate(NavigationArgs.For(AppView.ProjectsHome)));
        UploadCommand         = new RelayCommand(() => UploadRequested?.Invoke(this, EventArgs.Empty));
        ReplaceCommand        = new RelayCommand(() => UploadRequested?.Invoke(this, EventArgs.Empty));
        SchemaCommand         = new RelayCommand(() => SchemaRequested?.Invoke(this, EventArgs.Empty));
        DeleteDatasetCommand  = new AsyncRelayCommand(DeleteDatasetAsync);
        NewDashboardCommand   = new RelayCommand(() => NewDashboardRequested?.Invoke(this, EventArgs.Empty));
        OpenDashboardCommand  = new RelayCommand<DashboardDto>(d => _nav.Navigate(NavigationArgs.For(AppView.DashboardBuilder, _projectId, d.Id)));
        DeleteDashboardCommand= new RelayCommand<DashboardDto>(d => _ = DeleteDashboardAsync(d));
    }

    public async Task LoadAsync(int projectId)
    {
        _projectId = projectId;
        Loading    = true;
        try
        {
            var projTask  = _api.GetProjectAsync(projectId);
            var dsTask    = _api.ListDatasetsAsync(projectId);
            var dashTask  = _api.ListDashboardsAsync(projectId);
            await Task.WhenAll(projTask, dsTask, dashTask);

            Project = projTask.Result;
            Dataset = dsTask.Result.FirstOrDefault();
            Dashboards.Clear();
            foreach (var d in dashTask.Result) Dashboards.Add(d);
        }
        catch { /* ignore */ }
        finally { Loading = false; }
    }

    public async Task UploadFileAsync(string path)
    {
        var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        var allowed = new[] { ".csv", ".tsv", ".xlsx", ".xls", ".json", ".parquet" };
        if (!allowed.Contains(ext)) { SetUploadError($"Unsupported type: {ext}"); return; }
        if (new System.IO.FileInfo(path).Length > 100 * 1024 * 1024) { SetUploadError("File too large (max 100 MB)."); return; }

        HasUploadError = false;
        Uploading      = true;
        UploadPct      = 0;

        try
        {
            var progress = new Progress<double>(p => UploadPct = p);
            Dataset = await _api.UploadDatasetAsync(_projectId, path, progress);
            UploadPct = 100;
        }
        catch (Exception ex) { SetUploadError(ex.Message); }
        finally { Uploading = false; }
    }

    private void SetUploadError(string msg)
    {
        UploadError    = msg;
        HasUploadError = true;
    }

    private async Task DeleteDatasetAsync()
    {
        if (Dataset == null) return;
        try
        {
            await _api.DeleteDatasetAsync(Dataset.Id);
            Dataset = null;
            Dashboards.Clear();
        }
        catch { /* ignore */ }
    }

    public async Task ConfirmCreateDashboardAsync(DashboardCreateRequest req)
    {
        try
        {
            req.ProjectId = _projectId;
            req.DatasetId = Dataset?.Id;
            var created = await _api.CreateDashboardAsync(req);
            _nav.Navigate(NavigationArgs.For(AppView.DashboardBuilder, _projectId, created.Id));
        }
        catch (Exception ex) { UploadError = ex.Message; }
    }

    private async Task DeleteDashboardAsync(DashboardDto d)
    {
        try
        {
            await _api.DeleteDashboardAsync(d.Id);
            Dashboards.Remove(d);
        }
        catch { /* ignore */ }
    }

    // ── Events (View shows dialogs) ───────────────────────────────
    public event EventHandler? UploadRequested;
    public event EventHandler? SchemaRequested;
    public event EventHandler? NewDashboardRequested;
}
