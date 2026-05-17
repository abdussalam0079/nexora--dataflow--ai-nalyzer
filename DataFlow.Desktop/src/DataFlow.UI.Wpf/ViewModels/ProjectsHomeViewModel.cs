using System.Collections.ObjectModel;
using DataFlow.Core.Enums;
using DataFlow.Core.Interfaces;
using DataFlow.Core.Models;
using DataFlow.Core.Navigation;

namespace DataFlow.UI.Wpf.ViewModels;

public sealed class ProjectsHomeViewModel : BaseViewModel
{
    private readonly IDataFlowApiClient _api;
    private readonly IAppStateService   _state;
    private readonly INavigationService _nav;

    // ── Observable state ──────────────────────────────────────────
    private bool   _loading = true;
    private string _totalProjects   = "—";
    private string _totalDatasets   = "—";
    private string _totalDashboards = "—";

    public bool   Loading          { get => _loading;          set => Set(ref _loading, value); }
    public string TotalProjects    { get => _totalProjects;    set => Set(ref _totalProjects, value); }
    public string TotalDatasets    { get => _totalDatasets;    set => Set(ref _totalDatasets, value); }
    public string TotalDashboards  { get => _totalDashboards;  set => Set(ref _totalDashboards, value); }

    public ObservableCollection<ProjectDto> Projects { get; } = [];

    // ── Commands ──────────────────────────────────────────────────
    public AsyncRelayCommand LoadCommand       { get; }
    public AsyncRelayCommand NewProjectCommand { get; }
    public RelayCommand<ProjectDto> OpenCommand   { get; }
    public RelayCommand<ProjectDto> DeleteCommand { get; }

    public ProjectsHomeViewModel(IDataFlowApiClient api, IAppStateService state, INavigationService nav)
    {
        _api   = api;
        _state = state;
        _nav   = nav;

        LoadCommand       = new AsyncRelayCommand(LoadAsync);
        NewProjectCommand = new AsyncRelayCommand(CreateProjectAsync);
        OpenCommand       = new RelayCommand<ProjectDto>(OpenProject);
        DeleteCommand     = new RelayCommand<ProjectDto>(p => _ = DeleteProjectAsync(p));
    }

    public async Task LoadAsync()
    {
        Loading = true;
        try
        {
            var projects = await _api.ListProjectsAsync();
            Projects.Clear();
            foreach (var p in projects) Projects.Add(p);

            TotalProjects    = projects.Count.ToString();
            TotalDatasets    = projects.Sum(p => p.DatasetCount).ToString();
            TotalDashboards  = projects.Sum(p => p.DashboardCount).ToString();
        }
        catch
        {
            // Load demo data when API is offline
            LoadDemoProjects();
        }
        finally { Loading = false; }
    }

    private void LoadDemoProjects()
    {
        var demo = new[]
        {
            new ProjectDto { Id=1, Name="Quarterly Sales",   Icon="trending", Color="#F59E0B", Description="Revenue, churn, marketing insights.", DashboardCount=12, DatasetCount=8 },
            new ProjectDto { Id=2, Name="Customer Growth",   Icon="users",    Color="#10B981", Description="Acquisition, retention, LTV metrics.", DashboardCount=6,  DatasetCount=4 },
            new ProjectDto { Id=3, Name="Product Analytics", Icon="flask",    Color="#6366F1", Description="Usage, funnels, retention analysis.",  DashboardCount=8,  DatasetCount=5 },
        };
        Projects.Clear();
        foreach (var p in demo) Projects.Add(p);
        TotalProjects   = demo.Length.ToString();
        TotalDatasets   = demo.Sum(p => p.DatasetCount).ToString();
        TotalDashboards = demo.Sum(p => p.DashboardCount).ToString();
    }

    private void OpenProject(ProjectDto p)
    {
        _state.SetProject(p.Id);
        _nav.Navigate(NavigationArgs.For(AppView.ProjectDetail, p.Id));
    }

    private async Task CreateProjectAsync()
    {
        // Dialog is shown by the View; ViewModel exposes the request
        NewProjectRequested?.Invoke(this, EventArgs.Empty);
        await Task.CompletedTask;
    }

    public async Task ConfirmCreateAsync(ProjectCreateRequest req)
    {
        try
        {
            await _api.CreateProjectAsync(req);
            await LoadAsync();
        }
        catch (Exception ex) { Error = ex.Message; }
    }

    private async Task DeleteProjectAsync(ProjectDto p)
    {
        try
        {
            await _api.DeleteProjectAsync(p.Id);
            Projects.Remove(p);
            TotalProjects = Projects.Count.ToString();
        }
        catch (Exception ex) { Error = ex.Message; }
    }

    // ── Events (View subscribes to show dialogs) ──────────────────
    public event EventHandler? NewProjectRequested;

    private string _error = string.Empty;
    public string Error { get => _error; set => Set(ref _error, value); }
}
