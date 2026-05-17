using System.Windows;
using System.Windows.Controls;
using DataFlow.Core.Interfaces;
using DataFlow.UI.Wpf.Dialogs;
using DataFlow.UI.Wpf.ViewModels;

namespace DataFlow.UI.Wpf.Views;

public partial class ProjectsHomeView : UserControl
{
    private readonly ProjectsHomeViewModel? _vm;

    public ProjectsHomeView() => InitializeComponent();

    public ProjectsHomeView(IDataFlowApiClient api, IAppStateService state, INavigationService nav) : this()
    {
        _vm = new ProjectsHomeViewModel(api, state, nav);
        _vm.NewProjectRequested += OnNewProjectRequested;
        DataContext = _vm;
        Loaded += async (_, _) => await _vm.LoadCommand.ExecuteAsync();
    }

    private async void OnNewProjectRequested(object? sender, EventArgs e)
    {
        var dlg = new NewProjectDialog { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true && dlg.Result != null && _vm != null)
            await _vm.ConfirmCreateAsync(dlg.Result);
    }
}

// Extension to allow awaiting AsyncRelayCommand
file static class CommandExtensions
{
    public static Task ExecuteAsync(this DataFlow.UI.Wpf.ViewModels.AsyncRelayCommand cmd)
    {
        cmd.Execute(null);
        return Task.CompletedTask;
    }
}
