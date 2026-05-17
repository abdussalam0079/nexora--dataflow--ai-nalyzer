using System.Drawing;

namespace DataFlow.Core.Themes;

public static class DesignTokens
{
    public static readonly Color PageBg = Color.FromArgb(255, 255, 255);
    public static readonly Color TopbarBg = Color.FromArgb(255, 255, 255);
    public static readonly Color NavSecondaryBg = Color.FromArgb(249, 250, 251);
    public static readonly Color Border = Color.FromArgb(235, 235, 237);
    public static readonly Color Text = Color.FromArgb(15, 17, 23);
    public static readonly Color TextMuted = Color.FromArgb(107, 114, 128);
    public static readonly Color TextDim = Color.FromArgb(161, 161, 170);
    public static readonly Color InputBg = Color.FromArgb(244, 244, 246);
    public static readonly Color Accent = Color.FromArgb(99, 102, 241);
    public static readonly Color AccentHover = Color.FromArgb(79, 70, 229);
    public static readonly Color AccentBg = Color.FromArgb(238, 242, 255);
    public static readonly Color AccentBorder = Color.FromArgb(199, 210, 254);
    public static readonly Color RailBg = Color.FromArgb(99, 102, 241);
    public static readonly Color ContentBg = Color.FromArgb(249, 250, 251);
    public static readonly Color ProjectDot = Color.FromArgb(249, 115, 22);
    public static readonly Color DashboardBg = Color.FromArgb(15, 17, 24);

    public const string FontFamily = "Segoe UI";
    public const int RailWidth = 64;
    public const int NavPanelWidth = 232;
    public const int ConversationsWidth = 200;
    public const int TopbarHeight = 52;
    public const int SlotHeight = 52;
    public const int PillHeight = 44;
    public const int CornerRadius = 10;
}
