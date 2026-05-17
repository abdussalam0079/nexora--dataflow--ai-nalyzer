namespace DataFlow.UI.Dialogs;

public sealed class InputDialog : Form
{
    private readonly TextBox _textBox;

    public string Value => _textBox.Text;

    public InputDialog(string title, string prompt, string defaultValue = "")
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(360, 130);
        ShowInTaskbar = false;

        var label = new Label { Text = prompt, Location = new Point(16, 16), AutoSize = true };
        _textBox = new TextBox { Text = defaultValue, Location = new Point(16, 44), Width = 328 };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(168, 88), Width = 80 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(256, 88), Width = 80 };
        AcceptButton = ok;
        CancelButton = cancel;

        Controls.AddRange([label, _textBox, ok, cancel]);
    }

    public static string? Prompt(IWin32Window? owner, string title, string prompt, string defaultValue = "")
    {
        using var dlg = new InputDialog(title, prompt, defaultValue);
        return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.Value : null;
    }
}
