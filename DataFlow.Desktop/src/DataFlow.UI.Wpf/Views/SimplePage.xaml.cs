using System.Windows.Controls;

namespace DataFlow.UI.Wpf.Views;

public partial class SimplePage : UserControl
{
    public SimplePage(string title, string subtitle, string body)
    {
        InitializeComponent();
        TitleText.Text = title;
        SubtitleText.Text = subtitle;
        BodyText.Text = body;
    }
}
