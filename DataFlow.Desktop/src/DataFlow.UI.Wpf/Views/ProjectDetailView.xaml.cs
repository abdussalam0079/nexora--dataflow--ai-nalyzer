using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DataFlow.Core.Interfaces;
using DataFlow.Core.Navigation;
using DataFlow.UI.Wpf.Dialogs;
using DataFlow.UI.Wpf.ViewModels;
using Microsoft.Win32;

namespace DataFlow.UI.Wpf.Views;

public partial class ProjectDetailView : UserControl
{
    private readonly ProjectDetailViewModel? _vm;

    public ProjectDetailView() => InitializeComponent();

    public ProjectDetailView(IDataFlowApiClient api, IAppStateService state, INavigationService nav) : this()
    {
        _vm = new ProjectDetailViewModel(api, nav, state);
        _vm.UploadRequested      += OnUploadRequested;
        _vm.SchemaRequested      += OnSchemaRequested;
        _vm.NewDashboardRequested += OnNewDashboardRequested;
        DataContext = _vm;
    }

    public async Task LoadAsync(int projectId)
    {
        if (_vm != null) await _vm.LoadAsync(projectId);
    }

    // ── File upload ────────────────────────────────────────────────
    private void OnUploadRequested(object? sender, EventArgs e) => PickAndUpload();

    private void PickAndUpload()
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Select a dataset",
            Filter = "Data files|*.csv;*.tsv;*.xlsx;*.xls;*.json;*.parquet|All files|*.*"
        };
        if (dlg.ShowDialog() == true) _ = _vm?.UploadFileAsync(dlg.FileName);
    }

    private void OnFileDrop_Handler(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            _ = _vm?.UploadFileAsync(files[0]);
    }

    private void OnDragOver_Handler(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    // ── Schema modal ───────────────────────────────────────────────
    private void OnSchemaRequested(object? sender, EventArgs e)
    {
        // Schema dialog shown here — View responsibility
        if (_vm?.Dataset == null) return;
        System.Windows.MessageBox.Show(
            $"File: {_vm.Dataset.FileName}\nRows: {_vm.Dataset.RowCount:N0}\nColumns: {_vm.Dataset.ColCount}",
            "Schema", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ── New dashboard dialog ───────────────────────────────────────
    private async void OnNewDashboardRequested(object? sender, EventArgs e)
    {
        if (_vm == null) return;
        var dlg = new NewDashboardDialog { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true && dlg.Result != null)
            await _vm.ConfirmCreateDashboardAsync(dlg.Result);
    }

    // ── Thin XAML event handlers ───────────────────────────────────
    private void BackBtn_Click(object sender, RoutedEventArgs e)
        => _vm?.BackCommand.Execute(null);

    private void UploadZone_Click(object sender, MouseButtonEventArgs e)
        => _vm?.UploadCommand.Execute(null);

    private void ReplaceBtn_Click(object sender, RoutedEventArgs e)
        => _vm?.ReplaceCommand.Execute(null);

    private void SchemaBtn_Click(object sender, RoutedEventArgs e)
        => _vm?.SchemaCommand.Execute(null);

    private void DeleteDatasetBtn_Click(object sender, RoutedEventArgs e)
        => _vm?.DeleteDatasetCommand.Execute(null);

    private void NewDashboardBtn_Click(object sender, RoutedEventArgs e)
        => _vm?.NewDashboardCommand.Execute(null);

    private void AddDashboardRow_Click(object sender, MouseButtonEventArgs e)
        => _vm?.NewDashboardCommand.Execute(null);
}
