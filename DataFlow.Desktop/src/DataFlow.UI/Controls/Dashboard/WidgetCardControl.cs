using DataFlow.Application.Analytics;
using DataFlow.Core.Models;
using DataFlow.Core.Themes;
using DataFlow.UI.Controls.Charts;
using DataFlow.UI.Helpers;

namespace DataFlow.UI.Controls.Dashboard;

public sealed class WidgetCardControl : Panel
{
    private readonly Label _titleLabel;
    private readonly ChartWidgetHost _chartHost;
    private readonly Panel _header;
    private readonly Button _configBtn;
    private readonly Button _removeBtn;

    public DashboardWidgetModel Widget { get; set; } = new();
    public event EventHandler? ConfigureRequested;
    public event EventHandler? RemoveRequested;
    public event EventHandler? DragHandleMouseDown;
    public event EventHandler<ChartDrillDownEventArgs>? DrillDownRequested;

    public WidgetCardControl()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        Padding = new Padding(0);
        Margin = new Padding(4);

        _header = new Panel { Dock = DockStyle.Top, Height = 36, Padding = new Padding(8, 6, 8, 0) };
        _titleLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _configBtn = new Button { Text = "⚙", Width = 28, Height = 24, FlatStyle = FlatStyle.Flat, Dock = DockStyle.Right };
        _removeBtn = new Button { Text = "×", Width = 28, Height = 24, FlatStyle = FlatStyle.Flat, Dock = DockStyle.Right };
        _configBtn.Click += (_, _) => ConfigureRequested?.Invoke(this, EventArgs.Empty);
        _removeBtn.Click += (_, _) => RemoveRequested?.Invoke(this, EventArgs.Empty);
        _header.Controls.Add(_titleLabel);
        _header.Controls.Add(_configBtn);
        _header.Controls.Add(_removeBtn);
        _header.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) DragHandleMouseDown?.Invoke(this, EventArgs.Empty); };
        _titleLabel.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) DragHandleMouseDown?.Invoke(this, EventArgs.Empty); };

        _chartHost = new ChartWidgetHost { Dock = DockStyle.Fill };
        _chartHost.DrillDownRequested += (_, e) => DrillDownRequested?.Invoke(this, e);
        Controls.Add(_chartHost);
        Controls.Add(_header);
    }

    public void Bind(
        DashboardWidgetModel widget,
        ChartDataDto? rawData,
        IReadOnlyList<DashboardFilterModel> filters,
        ChartScheme scheme,
        bool editMode)
    {
        Widget = widget;
        BackColor = scheme.Card;
        _titleLabel.ForeColor = scheme.Muted;
        _titleLabel.Text = (widget.Title ?? widget.Type).ToUpperInvariant();
        _configBtn.Visible = editMode;
        _removeBtn.Visible = editMode;

        var data = WidgetDataComputer.Compute(rawData, widget, filters);
        _chartHost.Render(widget, data, scheme);
    }
}
