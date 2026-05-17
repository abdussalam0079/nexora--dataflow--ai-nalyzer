using DataFlow.Core.Models;
using DataFlow.Core.Themes;
using DataFlow.UI.Helpers;

namespace DataFlow.UI.Controls.Dashboard;

public sealed class DashboardCanvasControl : Panel
{
    public const int Columns = 12;
    public const int RowHeight = 80;
    public const int MarginPx = 8;

    private readonly Panel _surface;
    private readonly Dictionary<string, WidgetCardControl> _cards = new();
    private ChartDataDto? _rawData;
    private ChartScheme _scheme = ChartSchemes.Get("Metric Flow");
    private List<DashboardFilterModel> _filters = [];
    private bool _editMode = true;
    private WidgetCardControl? _dragging;
    private Point _dragOffset;
    private bool _resizing;
    private string? _pendingChartType;

    public event EventHandler? LayoutChanged;
    public event EventHandler<WidgetCardControl>? WidgetConfigure;
    public event EventHandler<WidgetCardControl>? WidgetRemove;
    public event EventHandler<ChartDrillDownEventArgs>? DrillDownRequested;

    public Control ExportSurface => _surface;

    public IReadOnlyList<DashboardWidgetModel> Widgets =>
        _cards.Values.Select(c => c.Widget).ToList();

    public DashboardCanvasControl()
    {
        AutoScroll = true;
        BackColor = Color.FromArgb(17, 19, 24);
        Dock = DockStyle.Fill;

        _surface = new Panel
        {
            Location = new Point(MarginPx, MarginPx),
            BackColor = Color.FromArgb(17, 19, 24)
        };
        Controls.Add(_surface);

        AllowDrop = true;
        DragEnter += (_, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.Text) == true)
                e.Effect = DragDropEffects.Copy;
        };
        DragDrop += OnDragDrop;
        Resize += (_, _) => LayoutWidgets();
    }

    public void SetChartScheme(ChartScheme scheme)
    {
        _scheme = scheme;
        BackColor = scheme.Background;
        _surface.BackColor = scheme.Background;
        RefreshAll();
    }

    public void SetData(ChartDataDto? data) { _rawData = data; RefreshAll(); }
    public void SetFilters(List<DashboardFilterModel> filters) { _filters = filters; RefreshAll(); }
    public void SetEditMode(bool edit) { _editMode = edit; RefreshAll(); }

    public void BeginDragNewChart(string chartType) => _pendingChartType = chartType;

    public void LoadWidgets(IEnumerable<DashboardWidgetModel> widgets)
    {
        foreach (var card in _cards.Values.ToList())
        {
            _surface.Controls.Remove(card);
            card.Dispose();
        }
        _cards.Clear();

        foreach (var w in widgets)
            AddWidget(w, false);

        LayoutWidgets();
    }

    public DashboardWidgetModel AddWidget(string type, string? title = null)
    {
        var info = Application.Analytics.ChartTypeCatalog.Get(type);
        var w = new DashboardWidgetModel
        {
            Id = $"w_{DateTime.UtcNow.Ticks}",
            Type = type,
            Title = title ?? info?.Label ?? type,
            Gw = info?.DefaultWidth ?? 6,
            Gh = info?.DefaultHeight ?? 5,
            Gx = 0,
            Gy = NextFreeRow()
        };
        AddWidget(w, true);
        return w;
    }

    private int NextFreeRow() =>
        _cards.Count == 0 ? 0 : _cards.Values.Max(c => c.Widget.Gy + c.Widget.Gh);

    private void AddWidget(DashboardWidgetModel w, bool notify)
    {
        var card = new WidgetCardControl();
        card.ConfigureRequested += (_, _) => WidgetConfigure?.Invoke(this, card);
        card.RemoveRequested += (_, _) => RemoveWidget(card.Widget.Id);
        card.DragHandleMouseDown += (_, _) => StartDrag(card);
        card.DrillDownRequested += (_, e) => DrillDownRequested?.Invoke(this, e);
        card.MouseDown += OnCardMouseDown;
        card.MouseMove += OnCardMouseMove;
        card.MouseUp += OnCardMouseUp;

        _cards[w.Id] = card;
        _surface.Controls.Add(card);
        card.Bind(w, _rawData, _filters, _scheme, _editMode);
        if (notify) LayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveWidget(string id)
    {
        if (!_cards.TryGetValue(id, out var card)) return;
        _surface.Controls.Remove(card);
        _cards.Remove(id);
        card.Dispose();
        LayoutWidgets();
        WidgetRemove?.Invoke(this, card);
        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateWidget(DashboardWidgetModel model)
    {
        if (_cards.TryGetValue(model.Id, out var card))
            card.Bind(model, _rawData, _filters, _scheme, _editMode);
    }

    private void RefreshAll()
    {
        foreach (var card in _cards.Values)
            card.Bind(card.Widget, _rawData, _filters, _scheme, _editMode);
    }

    private void LayoutWidgets()
    {
        var width = Math.Max(800, ClientSize.Width - MarginPx * 2 - SystemInformation.VerticalScrollBarWidth);
        var colWidth = width / (double)Columns;
        var maxRow = _cards.Count == 0 ? 4 : _cards.Values.Max(c => c.Widget.Gy + c.Widget.Gh);

        _surface.Width = width;
        _surface.Height = (int)(maxRow * RowHeight + MarginPx * 2);

        foreach (var card in _cards.Values)
        {
            var w = card.Widget;
            var x = (int)(w.Gx * colWidth) + MarginPx / 2;
            var y = w.Gy * RowHeight + MarginPx / 2;
            var cw = (int)(w.Gw * colWidth) - MarginPx;
            var ch = w.Gh * RowHeight - MarginPx;
            card.Bounds = new Rectangle(x, y, Math.Max(80, cw), Math.Max(60, ch));
        }
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        var type = _pendingChartType ?? e.Data?.GetData(DataFormats.Text) as string;
        _pendingChartType = null;
        if (string.IsNullOrEmpty(type)) return;

        var pt = _surface.PointToClient(PointToClient(new Point(e.X, e.Y)));
        var w = AddWidget(type);
        var colWidth = _surface.Width / (double)Columns;
        w.Gx = Math.Clamp((int)(pt.X / colWidth), 0, Columns - w.Gw);
        w.Gy = Math.Max(0, pt.Y / RowHeight);
        LayoutWidgets();
        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    private void StartDrag(WidgetCardControl card)
    {
        if (!_editMode) return;
        _dragging = card;
        _dragOffset = card.Location;
    }

    private void OnCardMouseDown(object? sender, MouseEventArgs e)
    {
        if (sender is not WidgetCardControl card || !_editMode) return;
        if (e.Button != MouseButtons.Left) return;

        var resizeZone = new Rectangle(card.Width - 16, card.Height - 16, 16, 16);
        if (resizeZone.Contains(e.Location))
        {
            _resizing = true;
            _dragging = card;
            return;
        }
    }

    private void OnCardMouseMove(object? sender, MouseEventArgs e)
    {
        if (_dragging is null || sender is not WidgetCardControl card) return;
        if (!_editMode) return;

        if (_resizing)
        {
            var colWidth = _surface.Width / (double)Columns;
            var newW = Math.Clamp((int)Math.Ceiling((card.Width + e.X - card.Width) / colWidth), 2, Columns - card.Widget.Gx);
            var newH = Math.Max(2, (int)Math.Ceiling(card.Height / (double)RowHeight));
            card.Widget.Gw = newW;
            card.Widget.Gh = newH;
            LayoutWidgets();
            return;
        }

        var newLoc = new Point(card.Left + e.X - card.Width / 2, card.Top + e.Y - 20);
        var colWidth2 = _surface.Width / (double)Columns;
        card.Widget.Gx = Math.Clamp((int)(newLoc.X / colWidth2), 0, Columns - 1);
        card.Widget.Gy = Math.Max(0, newLoc.Y / RowHeight);
        LayoutWidgets();
    }

    private void OnCardMouseUp(object? sender, MouseEventArgs e)
    {
        if (_dragging is null) return;
        _dragging = null;
        _resizing = false;
        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        LayoutWidgets();
    }
}
