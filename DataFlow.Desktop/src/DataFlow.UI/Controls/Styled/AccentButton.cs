using DataFlow.Core.Themes;

namespace DataFlow.UI.Controls.Styled;

/// <summary>Modern flat button (Guna/Bunifu-style) without third-party deps.</summary>
public sealed class AccentButton : Button
{
    public AccentButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = DesignTokens.Accent;
        ForeColor = Color.White;
        Font = new Font(DesignTokens.FontFamily, 9.5f, FontStyle.Bold);
        Cursor = Cursors.Hand;
        Height = 32;
        Padding = new Padding(12, 0, 12, 0);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        BackColor = Color.FromArgb(67, 56, 202);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        BackColor = DesignTokens.Accent;
    }
}
