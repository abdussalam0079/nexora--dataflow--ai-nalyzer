using DataFlow.Core.Navigation;
using DataFlow.Core.Themes;
using DataFlow.UI.Controls;

namespace DataFlow.UI.Views;

public sealed class PlaceholderView : UserControl, INavigationAware
{
    public PlaceholderView(string title, string subtitle)
    {
        BackColor = DesignTokens.ContentBg;
        var card = new RoundedPanel
        {
            Size = new Size(480, 200),
            Anchor = AnchorStyles.None
        };

        var titleLabel = new Label
        {
            Text = title,
            Font = new Font(DesignTokens.FontFamily, 16f, FontStyle.Bold),
            ForeColor = DesignTokens.Text,
            AutoSize = true,
            Location = new Point(24, 24)
        };
        var sub = new Label
        {
            Text = subtitle,
            Font = new Font(DesignTokens.FontFamily, 10f),
            ForeColor = DesignTokens.TextMuted,
            AutoSize = true,
            Location = new Point(24, 56),
            MaximumSize = new Size(420, 0)
        };

        card.Controls.Add(titleLabel);
        card.Controls.Add(sub);
        Controls.Add(card);

        Resize += (_, _) =>
        {
            card.Location = new Point(
                Math.Max(20, (Width - card.Width) / 2),
                Math.Max(20, (Height - card.Height) / 2));
        };
    }

    public void OnNavigatedTo(NavigationArgs args) { }
}
