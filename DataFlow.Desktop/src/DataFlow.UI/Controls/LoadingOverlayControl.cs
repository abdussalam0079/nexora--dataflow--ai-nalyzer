using DataFlow.Core.Themes;

namespace DataFlow.UI.Controls;

public sealed class LoadingOverlayControl : Panel
{
    private readonly Label _label;

    public LoadingOverlayControl()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(180, 255, 255, 255);
        Visible = false;

        _label = new Label
        {
            Text = "Loading…",
            AutoSize = true,
            Font = new Font(DesignTokens.FontFamily, 11f, FontStyle.Bold),
            ForeColor = DesignTokens.Text
        };
        Controls.Add(_label);
        Resize += (_, _) => CenterLabel();
    }

    public void Show(string message = "Loading…")
    {
        _label.Text = message;
        CenterLabel();
        Visible = true;
        BringToFront();
    }

    public void HideOverlay() => Visible = false;

    private void CenterLabel()
    {
        _label.Location = new Point(
            Math.Max(0, (Width - _label.Width) / 2),
            Math.Max(0, (Height - _label.Height) / 2));
    }
}
