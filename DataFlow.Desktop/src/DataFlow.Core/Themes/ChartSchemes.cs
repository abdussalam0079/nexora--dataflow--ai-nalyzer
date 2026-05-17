using System.Drawing;

namespace DataFlow.Core.Themes;

public sealed class ChartScheme
{
    public required string Name { get; init; }
    public required Color Background { get; init; }
    public required Color Card { get; init; }
    public required Color Border { get; init; }
    public required Color Text { get; init; }
    public required Color Muted { get; init; }
    public required Color Accent { get; init; }
    public required Color Accent2 { get; init; }
    public required Color Positive { get; init; }
    public required Color Negative { get; init; }
    public required Color[] Palette { get; init; }
}

public static class ChartSchemes
{
    public static IReadOnlyList<ChartScheme> All { get; } =
    [
        new ChartScheme
        {
            Name = "Metric Flow",
            Background = Color.FromArgb(17, 19, 24),
            Card = Color.FromArgb(26, 29, 40),
            Border = Color.FromArgb(40, 40, 45),
            Text = Color.FromArgb(230, 230, 235),
            Muted = Color.FromArgb(120, 120, 130),
            Accent = Color.FromArgb(224, 92, 45),
            Accent2 = Color.FromArgb(99, 102, 241),
            Positive = Color.FromArgb(16, 185, 129),
            Negative = Color.FromArgb(224, 92, 45),
            Palette =
            [
                Color.FromArgb(224, 92, 45), Color.FromArgb(99, 102, 241), Color.FromArgb(245, 158, 11),
                Color.FromArgb(16, 185, 129), Color.FromArgb(236, 72, 153), Color.FromArgb(6, 182, 212),
                Color.FromArgb(132, 204, 22), Color.FromArgb(249, 115, 22)
            ]
        },
        new ChartScheme
        {
            Name = "Neon Dark",
            Background = Color.FromArgb(13, 15, 26),
            Card = Color.FromArgb(19, 22, 38),
            Border = Color.FromArgb(35, 35, 45),
            Text = Color.FromArgb(230, 230, 235),
            Muted = Color.FromArgb(120, 120, 130),
            Accent = Color.FromArgb(0, 255, 180),
            Accent2 = Color.FromArgb(79, 140, 255),
            Positive = Color.FromArgb(0, 255, 180),
            Negative = Color.FromArgb(255, 107, 107),
            Palette =
            [
                Color.FromArgb(0, 255, 180), Color.FromArgb(79, 140, 255), Color.FromArgb(255, 217, 61),
                Color.FromArgb(255, 107, 107), Color.FromArgb(199, 125, 255), Color.FromArgb(0, 212, 255),
                Color.FromArgb(67, 233, 123), Color.FromArgb(250, 130, 49)
            ]
        },
        new ChartScheme
        {
            Name = "Ocean Blue",
            Background = Color.FromArgb(5, 13, 26),
            Card = Color.FromArgb(9, 20, 40),
            Border = Color.FromArgb(30, 50, 70),
            Text = Color.FromArgb(235, 240, 250),
            Muted = Color.FromArgb(120, 130, 150),
            Accent = Color.FromArgb(99, 179, 237),
            Accent2 = Color.FromArgb(79, 209, 197),
            Positive = Color.FromArgb(104, 211, 145),
            Negative = Color.FromArgb(252, 129, 129),
            Palette =
            [
                Color.FromArgb(99, 179, 237), Color.FromArgb(79, 209, 197), Color.FromArgb(246, 173, 85),
                Color.FromArgb(252, 129, 129), Color.FromArgb(183, 148, 244), Color.FromArgb(104, 211, 145),
                Color.FromArgb(251, 211, 141), Color.FromArgb(154, 230, 180)
            ]
        },
        new ChartScheme
        {
            Name = "Solar Gold",
            Background = Color.FromArgb(18, 16, 10),
            Card = Color.FromArgb(28, 24, 16),
            Border = Color.FromArgb(50, 45, 30),
            Text = Color.FromArgb(230, 225, 215),
            Muted = Color.FromArgb(130, 125, 115),
            Accent = Color.FromArgb(245, 163, 26),
            Accent2 = Color.FromArgb(240, 192, 64),
            Positive = Color.FromArgb(61, 214, 140),
            Negative = Color.FromArgb(249, 114, 114),
            Palette =
            [
                Color.FromArgb(245, 163, 26), Color.FromArgb(240, 192, 64), Color.FromArgb(62, 207, 178),
                Color.FromArgb(91, 140, 255), Color.FromArgb(166, 140, 255), Color.FromArgb(61, 214, 140),
                Color.FromArgb(249, 114, 114), Color.FromArgb(226, 232, 240)
            ]
        },
        new ChartScheme
        {
            Name = "Rose Quartz",
            Background = Color.FromArgb(17, 13, 18),
            Card = Color.FromArgb(26, 18, 32),
            Border = Color.FromArgb(50, 30, 45),
            Text = Color.FromArgb(235, 230, 240),
            Muted = Color.FromArgb(130, 120, 135),
            Accent = Color.FromArgb(236, 72, 153),
            Accent2 = Color.FromArgb(167, 139, 250),
            Positive = Color.FromArgb(16, 185, 129),
            Negative = Color.FromArgb(249, 115, 22),
            Palette =
            [
                Color.FromArgb(236, 72, 153), Color.FromArgb(167, 139, 250), Color.FromArgb(245, 158, 11),
                Color.FromArgb(16, 185, 129), Color.FromArgb(6, 182, 212), Color.FromArgb(249, 115, 22),
                Color.FromArgb(132, 204, 22), Color.FromArgb(251, 146, 60)
            ]
        },
        new ChartScheme
        {
            Name = "Cyberpunk",
            Background = Color.FromArgb(8, 5, 15),
            Card = Color.FromArgb(16, 13, 26),
            Border = Color.FromArgb(40, 40, 20),
            Text = Color.White,
            Muted = Color.FromArgb(130, 130, 140),
            Accent = Color.Yellow,
            Accent2 = Color.Magenta,
            Positive = Color.FromArgb(0, 255, 136),
            Negative = Color.FromArgb(255, 0, 102),
            Palette =
            [
                Color.Yellow, Color.Magenta, Color.Cyan, Color.FromArgb(255, 102, 0),
                Color.FromArgb(0, 255, 136), Color.FromArgb(255, 0, 102), Color.FromArgb(102, 0, 255),
                Color.FromArgb(255, 170, 0)
            ]
        }
    ];

    public static ChartScheme Get(string name) =>
        All.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? All[0];
}
