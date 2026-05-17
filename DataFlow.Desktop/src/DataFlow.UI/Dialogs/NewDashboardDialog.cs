using DataFlow.Core.Themes;

namespace DataFlow.UI.Dialogs;

public sealed class NewDashboardDialog : Form
{
    private readonly TextBox _name;
    private readonly TextBox _description;
    private readonly ComboBox _scheme;

    public string DashboardName => _name.Text.Trim();
    public string? Description => string.IsNullOrWhiteSpace(_description.Text) ? null : _description.Text.Trim();
    public string Scheme => _scheme.SelectedItem?.ToString() ?? "Metric Flow";

    public NewDashboardDialog()
    {
        Text = "New Dashboard";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(420, 240);
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;
        Font = new Font(DesignTokens.FontFamily, 9.5f);

        Controls.Add(new Label { Text = "Dashboard name", Location = new Point(20, 20), AutoSize = true, ForeColor = DesignTokens.TextMuted });
        _name = new TextBox { Location = new Point(20, 42), Width = 380, Text = "My Dashboard" };

        Controls.Add(new Label { Text = "Description (optional)", Location = new Point(20, 78), AutoSize = true, ForeColor = DesignTokens.TextMuted });
        _description = new TextBox { Location = new Point(20, 100), Width = 380 };

        Controls.Add(new Label { Text = "Color scheme", Location = new Point(20, 136), AutoSize = true, ForeColor = DesignTokens.TextMuted });
        _scheme = new ComboBox { Location = new Point(20, 158), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        _scheme.Items.AddRange(ChartSchemes.All.Select(s => s.Name).Cast<object>().ToArray());
        _scheme.SelectedIndex = 0;

        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(220, 190), Size = new Size(84, 32) };
        var ok = new Button
        {
            Text = "Create",
            DialogResult = DialogResult.OK,
            Location = new Point(316, 190),
            Size = new Size(84, 32),
            BackColor = DesignTokens.Accent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        ok.FlatAppearance.BorderSize = 0;
        AcceptButton = ok;
        CancelButton = cancel;

        Controls.AddRange([_name, _description, _scheme, cancel, ok]);
    }
}
