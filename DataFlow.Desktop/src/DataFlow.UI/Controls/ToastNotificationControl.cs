using DataFlow.Core.Themes;

namespace DataFlow.UI.Controls;

public sealed class ToastNotificationControl : Panel
{
    private readonly Label _text;
    private readonly System.Windows.Forms.Timer _timer;
    private float _opacity = 1f;

    public ToastNotificationControl()
    {
        Size = new Size(320, 44);
        BackColor = Color.FromArgb(240, 17, 24, 39);
        Anchor = AnchorStyles.Top | AnchorStyles.Right;
        Visible = false;

        _text = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = new Font(DesignTokens.FontFamily, 9.5f),
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(12, 0, 12, 0)
        };
        Controls.Add(_text);

        _timer = new System.Windows.Forms.Timer { Interval = 16 };
        _timer.Tick += (_, _) =>
        {
            _opacity -= 0.04f;
            if (_opacity <= 0)
            {
                _timer.Stop();
                Visible = false;
                return;
            }
            BackColor = Color.FromArgb((int)(_opacity * 240), 17, 24, 39);
        };

        Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(60, 255, 255, 255));
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        };
    }

    public void Show(string message, int durationMs = 2800)
    {
        _text.Text = message;
        _opacity = 1f;
        BackColor = Color.FromArgb(240, 17, 24, 39);
        Visible = true;
        BringToFront();

        _timer.Stop();
        var hide = new System.Windows.Forms.Timer { Interval = durationMs };
        hide.Tick += (_, _) =>
        {
            hide.Stop();
            hide.Dispose();
            _timer.Start();
        };
        hide.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }
}
