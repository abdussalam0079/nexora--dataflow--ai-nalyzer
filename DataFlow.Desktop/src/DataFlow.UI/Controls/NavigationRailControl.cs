using DataFlow.Core.Themes;

namespace DataFlow.UI.Controls;

public sealed class NavigationRailControl : Control
{
    private const int SlotCount = 4;
    private int _activeIndex;
    private float _pillTop = 90;
    private float _targetTop = 90;
    private readonly System.Windows.Forms.Timer _animTimer;

    public event EventHandler<int>? RailItemClicked;

    public int ActiveIndex
    {
        get => _activeIndex;
        set
        {
            if (_activeIndex == value) return;
            _activeIndex = Math.Clamp(value, 0, SlotCount - 1);
            _targetTop = 90 + _activeIndex * DesignTokens.SlotHeight;
            _animTimer.Start();
            Invalidate();
        }
    }

    public NavigationRailControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        Width = DesignTokens.RailWidth;
        Dock = DockStyle.Left;
        BackColor = DesignTokens.RailBg;
        _targetTop = 90;

        _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _animTimer.Tick += (_, _) =>
        {
            var diff = _targetTop - _pillTop;
            if (Math.Abs(diff) < 0.5f) { _pillTop = _targetTop; _animTimer.Stop(); }
            else _pillTop += diff * 0.18f;
            Invalidate();
        };
        Click += OnClick;
    }

    private void OnClick(object? sender, EventArgs e)
    {
        var y = PointToClient(Cursor.Position).Y;
        if (y < 72) return;
        if (y > Height - 48) return;
        var slot = (y - 90) / DesignTokens.SlotHeight;
        if (slot is >= 0 and < SlotCount)
            RailItemClicked?.Invoke(this, slot);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(DesignTokens.RailBg);

        using (var logoFont = new Font(DesignTokens.FontFamily, 14f, FontStyle.Bold))
            g.DrawString("📊", logoFont, Brushes.White, 20, 18);

        DrawCutoutPill(g, (int)_pillTop);

        var icons = new[] { "💬", "▦", "?", "⚙" };
        for (var i = 0; i < SlotCount; i++)
        {
            var slotY = 90 + i * DesignTokens.SlotHeight;
            using var font = new Font(DesignTokens.FontFamily, i == 1 ? 13f : 14f);
            var size = g.MeasureString(icons[i], font);
            g.DrawString(icons[i], font, Brushes.White,
                (Width - size.Width) / 2f,
                slotY + (DesignTokens.SlotHeight - size.Height) / 2f);
        }

        using var collapseFont = new Font(DesignTokens.FontFamily, 10f);
        g.DrawString("«", collapseFont, Brushes.White, 24, Height - 36);
    }

    private static void DrawCutoutPill(Graphics g, int slotTop)
    {
        const int inset = 10;
        const int lr = 18;
        const int cr = 18;
        var w = DesignTokens.RailWidth;
        var pt = slotTop + (DesignTokens.SlotHeight - DesignTokens.PillHeight) / 2;
        var pb = pt + DesignTokens.PillHeight;

        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.StartFigure();
        path.AddLine(inset + lr, pt, w - cr, pt);
        path.AddBezier(w - cr, pt, w, pt, w, pt - cr, w, pt - cr);
        path.AddLine(w, pb + cr, w, pb + cr);
        path.AddBezier(w, pb + cr, w, pb, w - cr, pb, w - cr, pb);
        path.AddLine(inset + lr, pb, inset + lr, pb);
        path.AddArc(inset, pb - lr * 2, lr * 2, lr * 2, 90, 90);
        path.AddLine(inset, pt + lr, inset, pt + lr);
        path.AddArc(inset, pt, lr * 2, lr * 2, 180, 90);
        path.CloseFigure();
        using var brush = new SolidBrush(DesignTokens.PageBg);
        g.FillPath(brush, path);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _animTimer.Dispose();
        base.Dispose(disposing);
    }
}
