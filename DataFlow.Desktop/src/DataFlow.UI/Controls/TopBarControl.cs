using DataFlow.Core.Themes;
using DataFlow.UI.Helpers;

namespace DataFlow.UI.Controls;

public sealed class TopBarControl : Panel
{
    private readonly TextBox _searchBox;
    private bool _searchFocused;

    public TopBarControl()
    {
        Height = DesignTokens.TopbarHeight;
        Dock = DockStyle.Top;
        BackColor = DesignTokens.TopbarBg;
        Padding = new Padding(16, 0, 20, 0);

        var searchWrap = new Panel
        {
            Height = 36,
            BackColor = DesignTokens.InputBg,
            Location = new Point(16, 8),
            Width = 480
        };

        searchWrap.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var path = GraphicsExtensions.CreateRoundedRect(new Rectangle(0, 0, searchWrap.Width - 1, searchWrap.Height - 1), 8);
            using var bg = new SolidBrush(_searchFocused ? Color.White : DesignTokens.InputBg);
            e.Graphics.FillPath(bg, path);
            using var pen = new Pen(_searchFocused ? DesignTokens.Accent : DesignTokens.Border);
            e.Graphics.DrawPath(pen, path);
        };

        var searchIcon = new Label
        {
            Text = "🔍",
            Location = new Point(10, 8),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        _searchBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = DesignTokens.InputBg,
            ForeColor = DesignTokens.Text,
            Font = new Font(DesignTokens.FontFamily, 10f),
            PlaceholderText = "Search projects, dashboards, chats…",
            Location = new Point(36, 9),
            Width = 360,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        _searchBox.GotFocus += (_, _) => { _searchFocused = true; searchWrap.Invalidate(); };
        _searchBox.LostFocus += (_, _) => { _searchFocused = false; searchWrap.Invalidate(); };

        var kbdHint = new Label
        {
            Text = "Ctrl+K",
            AutoSize = true,
            ForeColor = DesignTokens.TextDim,
            Font = new Font(DesignTokens.FontFamily, 8f),
            BackColor = Color.Transparent,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        searchWrap.Controls.Add(kbdHint);
        searchWrap.Controls.Add(_searchBox);
        searchWrap.Controls.Add(searchIcon);

        var bell = new Label
        {
            Text = "🔔",
            Size = new Size(28, 28),
            TextAlign = ContentAlignment.MiddleCenter,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };

        var dateLabel = new Label
        {
            AutoSize = true,
            ForeColor = DesignTokens.TextDim,
            Font = new Font(DesignTokens.FontFamily, 9f),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        dateLabel.Text = DateTime.Now.ToString("ddd, MMM d");

        var avatar = new Panel
        {
            Size = new Size(32, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = DesignTokens.AccentBg
        };
        avatar.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var path = GraphicsExtensions.CreateRoundedRect(new Rectangle(0, 0, 31, 31), 16);
            e.Graphics.FillPath(new SolidBrush(DesignTokens.AccentBg), path);
            TextRenderer.DrawText(e.Graphics, "MH", new Font(DesignTokens.FontFamily, 9f, FontStyle.Bold),
                new Rectangle(0, 0, 32, 32), DesignTokens.Accent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };

        Controls.Add(searchWrap);
        Controls.Add(bell);
        Controls.Add(dateLabel);
        Controls.Add(avatar);

        Resize += (_, _) =>
        {
            searchWrap.Width = Math.Min(520, Width - 320);
            kbdHint.Location = new Point(searchWrap.Width - 52, 10);
            _searchBox.Width = searchWrap.Width - 100;
            avatar.Location = new Point(Width - 48, 10);
            bell.Location = new Point(Width - 88, 12);
            dateLabel.Location = new Point(Width - 200, 18);
        };
    }

    public void FocusSearch()
    {
        _searchBox.Focus();
        _searchBox.SelectAll();
    }
}
