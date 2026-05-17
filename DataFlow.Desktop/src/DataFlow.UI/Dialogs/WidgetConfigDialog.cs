using DataFlow.Application.Analytics;
using DataFlow.Core.Models;
using DataFlow.Core.Themes;

namespace DataFlow.UI.Dialogs;

public sealed class WidgetConfigDialog : Form
{
    private readonly DashboardWidgetModel _model;
    private readonly DataSchemaInfo _schema;
    private readonly TextBox _title;
    private readonly ComboBox _type;
    private readonly ComboBox _xCol;
    private readonly ComboBox _yCol;
    private readonly ComboBox _agg;
    private readonly NumericUpDown _topN;

    public DashboardWidgetModel Result { get; private set; }

    public WidgetConfigDialog(DashboardWidgetModel model, DataSchemaInfo schema)
    {
        _model = model;
        _schema = schema;
        Result = model;

        Text = "Configure Widget";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(380, 360);
        BackColor = Color.FromArgb(26, 29, 40);
        ForeColor = Color.White;
        Font = new Font(DesignTokens.FontFamily, 9.5f);

        var y = 16;
        Controls.Add(MkLabel("Title", 16, y)); y += 22;
        _title = new TextBox { Location = new Point(16, y), Width = 348, Text = model.Title ?? "", BackColor = Color.FromArgb(40, 44, 55), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        Controls.Add(_title); y += 36;

        Controls.Add(MkLabel("Chart type", 16, y)); y += 22;
        _type = new ComboBox { Location = new Point(16, y), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(40, 44, 55), ForeColor = Color.White };
        foreach (var t in ChartTypeCatalog.All) _type.Items.Add(t.Label);
        _type.SelectedIndex = Math.Max(0, ChartTypeCatalog.All.ToList().FindIndex(c => c.Id == model.Type));
        Controls.Add(_type); y += 36;

        Controls.Add(MkLabel("Dimension (X)", 16, y)); y += 22;
        _xCol = MkCombo(schema.All, model.XCol, 16, y); Controls.Add(_xCol); y += 36;

        Controls.Add(MkLabel("Value (Y)", 16, y)); y += 22;
        _yCol = MkCombo(schema.Numeric.Concat(schema.All).Distinct().ToList(), model.YCol, 16, y); Controls.Add(_yCol); y += 36;

        Controls.Add(MkLabel("Aggregation", 16, y)); y += 22;
        _agg = new ComboBox { Location = new Point(16, y), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(40, 44, 55), ForeColor = Color.White };
        _agg.Items.AddRange(["sum", "avg", "count", "min", "max"]);
        _agg.SelectedItem = model.Aggregation;
        Controls.Add(_agg);

        Controls.Add(MkLabel("Top N", 160, y)); y += 22;
        _topN = new NumericUpDown { Location = new Point(160, y - 22), Width = 80, Maximum = 100, Minimum = 0, Value = model.TopN ?? 0, BackColor = Color.FromArgb(40, 44, 55), ForeColor = Color.White };
        Controls.Add(_topN); y += 20;

        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(180, 300), Size = new Size(84, 32) };
        var ok = new Button { Text = "Apply", DialogResult = DialogResult.OK, Location = new Point(280, 300), Size = new Size(84, 32), BackColor = DesignTokens.Accent, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        ok.FlatAppearance.BorderSize = 0;
        ok.Click += (_, _) =>
        {
            var typeId = ChartTypeCatalog.All[_type.SelectedIndex].Id;
            Result = new DashboardWidgetModel
            {
                Id = _model.Id,
                Type = typeId,
                Title = _title.Text,
                XCol = _xCol.SelectedItem?.ToString(),
                YCol = _yCol.SelectedItem?.ToString(),
                Aggregation = _agg.SelectedItem?.ToString() ?? "sum",
                TopN = _topN.Value > 0 ? (int)_topN.Value : null,
                Gx = _model.Gx,
                Gy = _model.Gy,
                Gw = _model.Gw,
                Gh = _model.Gh,
                Change = _model.Change,
                Threshold = _model.Threshold,
                SortBy = _model.SortBy,
                SortDir = _model.SortDir
            };
        };
        AcceptButton = ok;
        CancelButton = cancel;
        Controls.AddRange([cancel, ok]);
    }

    private static Label MkLabel(string text, int x, int y) =>
        new() { Text = text, Location = new Point(x, y), AutoSize = true, ForeColor = Color.FromArgb(160, 165, 175), Font = new Font(DesignTokens.FontFamily, 8f, FontStyle.Bold) };

    private static ComboBox MkCombo(List<string> items, string? selected, int x, int y)
    {
        var c = new ComboBox { Location = new Point(x, y), Width = 348, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(40, 44, 55), ForeColor = Color.White };
        c.Items.Add("");
        foreach (var i in items) c.Items.Add(i);
        c.SelectedItem = selected ?? "";
        return c;
    }
}
