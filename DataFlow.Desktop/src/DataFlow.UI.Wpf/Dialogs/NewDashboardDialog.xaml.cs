using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DataFlow.Core.Models;

namespace DataFlow.UI.Wpf.Dialogs;

public partial class NewDashboardDialog : Window
{
    public DashboardCreateRequest? Result { get; private set; }
    public string SelectedScheme { get; private set; } = "Metric Flow";
    public string? AiPrompt { get; private set; }

    private static readonly (string Name, string Accent, string Desc)[] Schemes =
    [
        ("Metric Flow",  "#e05c2d", "Clean dark orange"),
        ("Neon Dark",    "#00ffb4", "Cyberpunk green"),
        ("Ocean Blue",   "#63b3ed", "Deep blue calm"),
        ("Solar Gold",   "#f5a31a", "Warm amber"),
        ("Rose Quartz",  "#ec4899", "Bold pink"),
        ("Cyberpunk",    "#a855f7", "Max contrast"),
    ];

    private static readonly (string Label, string Text)[] Presets =
    [
        ("Sales",      "Sales dashboard — KPI cards for revenue, orders, customers. Monthly trend line chart, bar chart for top products, pie chart for region distribution."),
        ("Operations", "Operations dashboard — uptime, throughput, error rate KPIs. Time-series line chart, activity ranking table."),
        ("Executive",  "Executive summary — 4 KPI cards, large area trend, donut distribution, radar performance, ranking table."),
        ("Financial",  "Financial overview — revenue, profit, expenses, margin KPIs. Bar chart, budget vs actual radar, cost center ranking."),
    ];

    public NewDashboardDialog()
    {
        InitializeComponent();
        BuildSchemePicker();
        BuildPresetChips();
    }

    private void BuildSchemePicker()
    {
        SchemePanel.Children.Clear();
        foreach (var (name, accent, desc) in Schemes)
        {
            var accentColor = (Color)ColorConverter.ConvertFromString(accent);
            var isActive = name == SelectedScheme;
            var card = new Border
            {
                Margin = new Thickness(4),
                Padding = new Thickness(10),
                CornerRadius = new CornerRadius(11),
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = isActive
                    ? new SolidColorBrush(Color.FromRgb(0xEE, 0xF2, 0xFF))
                    : new SolidColorBrush(Color.FromRgb(0xF9, 0xFA, 0xFB)),
                BorderBrush = isActive
                    ? new SolidColorBrush(Color.FromRgb(0x63, 0x66, 0xF1))
                    : new SolidColorBrush(Color.FromRgb(0xE5, 0xE7, 0xEB)),
                BorderThickness = new Thickness(1.5),
                Tag = name
            };
            var sp = new StackPanel();
            var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 3) };
            headerRow.Children.Add(new Ellipse
            {
                Width = 11, Height = 11,
                Fill = new SolidColorBrush(accentColor),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            headerRow.Children.Add(new TextBlock
            {
                Text = name, FontSize = 12, FontWeight = FontWeights.SemiBold,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = isActive
                    ? new SolidColorBrush(Color.FromRgb(0x43, 0x38, 0xCA))
                    : new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27)),
                VerticalAlignment = VerticalAlignment.Center
            });
            sp.Children.Add(headerRow);
            sp.Children.Add(new TextBlock
            {
                Text = desc, FontSize = 10, FontFamily = new FontFamily("Segoe UI"),
                Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80))
            });
            card.Child = sp;
            card.MouseLeftButtonDown += (_, _) => SelectScheme(name);
            SchemePanel.Children.Add(card);
        }
    }

    private void SelectScheme(string name)
    {
        SelectedScheme = name;
        BuildSchemePicker();
    }

    private void BuildPresetChips()
    {
        PresetPanel.Children.Clear();
        foreach (var (label, text) in Presets)
        {
            var chip = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xF9, 0xFA, 0xFB)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xE5, 0xE7, 0xEB)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(99),
                Padding = new Thickness(12, 5, 12, 5),
                Margin = new Thickness(0, 0, 6, 6),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = text,
                Child = new TextBlock
                {
                    Text = label, FontSize = 11, FontFamily = new FontFamily("Segoe UI"),
                    Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80))
                }
            };
            chip.MouseLeftButtonDown += (_, _) => PromptBox.Text = (string)chip.Tag;
            chip.MouseEnter += (_, _) =>
            {
                chip.Background = new SolidColorBrush(Color.FromRgb(0xEE, 0xF2, 0xFF));
                chip.BorderBrush = new SolidColorBrush(Color.FromRgb(0xC7, 0xD2, 0xFE));
            };
            chip.MouseLeave += (_, _) =>
            {
                chip.Background = new SolidColorBrush(Color.FromRgb(0xF9, 0xFA, 0xFB));
                chip.BorderBrush = new SolidColorBrush(Color.FromRgb(0xE5, 0xE7, 0xEB));
            };
            PresetPanel.Children.Add(chip);
        }
    }

    private void CreateBtn_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            NameError.Visibility = Visibility.Visible;
            return;
        }
        AiPrompt = PromptBox.Text.Trim().NullIfEmpty();
        Result = new DashboardCreateRequest
        {
            Name = name,
            Description = DescBox.Text.Trim().NullIfEmpty(),
            Scheme = SelectedScheme
        };
        DialogResult = true;
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}

file static class DashboardStringExtensions
{
    public static string? NullIfEmpty(this string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}
