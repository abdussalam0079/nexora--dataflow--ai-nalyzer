using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DataFlow.Core.Interfaces;
using DataFlow.UI.Wpf.ViewModels;
using Microsoft.Win32;

namespace DataFlow.UI.Wpf.Views;

public partial class ChatWorkspaceView : UserControl
{
    private readonly ChatWorkspaceViewModel? _vm;

    public ChatWorkspaceView() => InitializeComponent();

    public ChatWorkspaceView(IDataFlowApiClient api, IAppStateService state) : this()
    {
        _vm = new ChatWorkspaceViewModel(api);
        _vm.AttachFileRequested += OnAttachFile;
        DataContext = _vm;
    }

    private void OnAttachFile(object? sender, EventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Attach a data file",
            Filter = "Data files|*.csv;*.tsv;*.xlsx;*.xls;*.pdf;*.txt|All files|*.*"
        };
        if (dlg.ShowDialog() == true) _vm?.SetAttachment(dlg.FileName);
    }

    // ── Thin event handlers — delegate to ViewModel ───────────────
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

    private void NewChatBtn_Click(object sender, RoutedEventArgs e)
        => _vm?.NewChatCommand.Execute(null);

    private void AttachFile_Click(object sender, RoutedEventArgs e)
        => _vm?.AttachFileCommand.Execute(null);

    private void RemoveAttachment_Click(object sender, RoutedEventArgs e)
        => _vm?.RemoveAttachment.Execute(null);

    private void SuggestionCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.Border b && b.Tag is string prompt)
            _vm?.SendSuggestion(prompt);
    }
}
