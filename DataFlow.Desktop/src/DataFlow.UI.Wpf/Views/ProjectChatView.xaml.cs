using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DataFlow.Core.Interfaces;
using DataFlow.UI.Wpf.ViewModels;
using Microsoft.Win32;

namespace DataFlow.UI.Wpf.Views;

public partial class ProjectChatView : UserControl
{
    private readonly ProjectChatViewModel? _vm;

    public ProjectChatView() => InitializeComponent();

    public ProjectChatView(IDataFlowApiClient api, IAppStateService state) : this()
    {
        _vm = new ProjectChatViewModel(api);
        _vm.AttachRequested += OnAttachRequested;
        DataContext = _vm;
    }

    public async Task LoadAsync(int projectId, int? chatSessionId = null)
    {
        if (_vm != null) await _vm.LoadAsync(projectId, chatSessionId);
    }

    private void OnAttachRequested(object? sender, EventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Attach a data file",
            Filter = "Data files|*.csv;*.tsv;*.xlsx;*.xls;*.pdf;*.txt|All files|*.*"
        };
        if (dlg.ShowDialog() == true) _vm?.SetAttachment(dlg.FileName);
    }

    // ── Thin event handlers ────────────────────────────────────────
    private void MessageBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (InputPlaceholder != null)
            InputPlaceholder.Visibility = string.IsNullOrEmpty(MessageBox.Text)
                ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MessageBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
        {
            e.Handled = true;
            _vm?.SendCommand.Execute(null);
        }
    }

    private void SendBtn_Click(object sender, RoutedEventArgs e)
        => _vm?.SendCommand.Execute(null);

    private void NewChatBtn_Click(object sender, MouseButtonEventArgs e)
        => _vm?.NewChatCommand.Execute(null);

    private void CollapseBtn_Click(object sender, MouseButtonEventArgs e)
        => _vm?.TogglePanelCommand.Execute(null);

    private void AttachFile_Click(object sender, RoutedEventArgs e)
        => _vm?.AttachCommand.Execute(null);

    private void RemoveAttachment_Click(object sender, RoutedEventArgs e)
        => _vm?.RemoveAttachment.Execute(null);

    private void RenameConfirm_Click(object sender, RoutedEventArgs e)
        => _vm?.RenameConfirmCommand.Execute(null);

    private void RenameCancel_Click(object sender, RoutedEventArgs e)
        => _vm?.RenameCancelCommand.Execute(null);

    private void RenameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)  _vm?.RenameConfirmCommand.Execute(null);
        if (e.Key == Key.Escape) _vm?.RenameCancelCommand.Execute(null);
    }
}
