using DataFlow.Core.Themes;

namespace DataFlow.UI.Controls.Styled;

public sealed class GhostButton : Button
{
    public GhostButton()
    {
        FlatStyle = FlatStyle.Flat;
        BackColor = DesignTokens.AccentBg;
        ForeColor = DesignTokens.Accent;
        Font = new Font(DesignTokens.FontFamily, 9f);
        Cursor = Cursors.Hand;
        FlatAppearance.BorderColor = DesignTokens.AccentBorder;
        FlatAppearance.BorderSize = 1;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        BackColor = Color.FromArgb(224, 231, 255);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        BackColor = DesignTokens.AccentBg;
    }
}
