using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DataFlow.Core.Models;

namespace DataFlow.UI.Wpf.Dialogs;

public partial class NewProjectDialog : Window
{
    public ProjectCreateRequest? Result { get; private set; }

    private string _selectedIcon = "dashboard";
    private string _selectedColor = "#6366f1";

    private static readonly (string Id, string Emoji)[] Icons =
    [
        ("dashboard", "📊"), ("trending", "📈"), ("bar", "📉"), ("dollar", "💰"),
        ("globe", "🌐"), ("cpu", "⚙"), ("target", "🎯"), ("flask", "🧪"),
        ("users", "👥"), ("zap", "⚡")
    ];

    private static readonly string[] Colors =
    [
        "#6366f1", "#e05c2d", "#3ecfb2", "#f0c040",
        "#ec4899", "#5b8cff", "#34d399", "#f97272",
        "#a855f7", "#06b6d4"
    ];

    public NewProjectDialog()
    {
        InitializeComponent();
        BuildIconPicker();
        BuildColorPicker();
        UpdatePreview();
    }

    private void BuildIconPicker()
    {
        IconPanel.Children.Clear();
        foreach (var (id, emoji) in Icons)
        {
            var btn = new Border
            {
                Width = 36, Height = 36, CornerRadius = new CornerRadius(9),
                Margin = new Thickness(0, 0, 6, 6),
                Background = new SolidColorBrush(Color.FromRgb(0xF9, 0xFA, 0xFB)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xE5, 0xE7, 0xEB)),
                BorderThickness = new Thickness(1.5),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = id,
                Child = new TextBlock
                {
                    Text = emoji, FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            btn.MouseLeftButtonDown += (_, _) => SelectIcon(id, btn);
            IconPanel.Children.Add(btn);
        }
        SelectIcon(_selectedIcon, null);
    }

    private void SelectIcon(string id, Border? clicked)
    {
        _selectedIcon = id;
        foreach (Border b in IconPanel.Children.OfType<Border>())
        {
            var isActive = (string)b.Tag == id;
            b.BorderBrush = isActive
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(_selectedColor))
                : new SolidColorBrush(Color.FromRgb(0xE5, 0xE7, 0xEB));
            b.Background = isActive
                ? new SolidColorBrush(Color.FromArgb(24,
                    ((Color)ColorConverter.ConvertFromString(_selectedColor)).R,
                    ((Color)ColorConverter.ConvertFromString(_selectedColor)).G,
                    ((Color)ColorConverter.ConvertFromString(_selectedColor)).B))
                : new SolidColorBrush(Color.FromRgb(0xF9, 0xFA, 0xFB));
        }
        UpdatePreview();
    }

    private void BuildColorPicker()
    {
        ColorPanel.Children.Clear();
        foreach (var hex in Colors)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            var btn = new Border
            {
                Width = 22, Height = 22, CornerRadius = new CornerRadius(6),
                Margin = new Thickness(3),
                Background = new SolidColorBrush(color),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = hex
            };
            btn.MouseLeftButtonDown += (_, _) => SelectColor(hex);
            ColorPanel.Children.Add(btn);
        }
        SelectColor(_selectedColor);
    }

    private void SelectColor(string hex)
    {
        _selectedColor = hex;
        var color = (Color)ColorConverter.ConvertFromString(hex);
        foreach (Border b in ColorPanel.Children.OfType<Border>())
        {
            b.BorderThickness = (string)b.Tag == hex ? new Thickness(2) : new Thickness(0);
            b.BorderBrush = (string)b.Tag == hex
                ? new SolidColorBrush(System.Windows.Media.Colors.White)
                : Brushes.Transparent;
        }
        // Update icon selection border color
        SelectIcon(_selectedIcon, null);
        // Update create button color
        CreateBtn.Background = new SolidColorBrush(color);
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        var name = NameBox?.Text?.Trim();
        var desc = DescBox?.Text?.Trim();
        var emoji = Icons.FirstOrDefault(i => i.Id == _selectedIcon).Emoji ?? "📊";
        var color = (Color)ColorConverter.ConvertFromString(_selectedColor);

        if (PreviewName != null)
        {
            PreviewName.Text = string.IsNullOrEmpty(name) ? "Project name…" : name;
            PreviewName.Foreground = string.IsNullOrEmpty(name)
                ? new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF))
                : new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27));
        }
        if (PreviewDesc != null)
            PreviewDesc.Text = string.IsNullOrEmpty(desc) ? "No description" : desc;
        if (PreviewIconText != null)
            PreviewIconText.Text = emoji;
        if (PreviewIconBorder != null)
        {
            PreviewIconBorder.Background = new SolidColorBrush(Color.FromArgb(24, color.R, color.G, color.B));
            PreviewIconBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(68, color.R, color.G, color.B));
            PreviewIconBorder.BorderThickness = new Thickness(1);
        }
    }

    private void NameBox_TextChanged(object sender, TextChangedEventArgs e) => UpdatePreview();
    private void DescBox_TextChanged(object sender, TextChangedEventArgs e) => UpdatePreview();

    private void CreateBtn_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            NameBox.BorderBrush = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
            return;
        }
        Result = new ProjectCreateRequest
        {
            Name = name,
            Description = DescBox.Text.Trim().NullIfEmpty(),
            Icon = _selectedIcon,
            Color = _selectedColor
        };
        DialogResult = true;
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}

file static class StringExtensions
{
    public static string? NullIfEmpty(this string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}
