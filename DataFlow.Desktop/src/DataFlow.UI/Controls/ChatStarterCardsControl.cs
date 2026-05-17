using DataFlow.Core.Themes;
using DataFlow.UI.Helpers;

namespace DataFlow.UI.Controls;

public sealed class ChatStarterCardsControl : Panel
{
    public event EventHandler<string>? PromptSelected;
    public event EventHandler? CreateDashboardRequested;

    private static readonly (string Icon, string Label, string Prompt, bool IsNav)[] Cards =
    [
        ("📄", "Summarize Dataset", "Please give me a clear summary of this dataset — key columns, distributions, and any notable patterns.", false),
        ("▦", "Create a Dashboard", "", true),
        ("📈", "Trend Analysis", "Analyze trends over time in this dataset. Highlight seasonality and growth patterns.", false),
        ("🔍", "Find Anomalies", "Identify outliers, missing values, and unusual patterns in this dataset.", false)
    ];

    public ChatStarterCardsControl()
    {
        Size = new Size(520, 200);
        BackColor = Color.Transparent;

        var grid = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 2,
            Dock = DockStyle.Fill,
            Padding = new Padding(0)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        foreach (var (icon, label, prompt, isNav) in Cards)
        {
            var card = new Panel
            {
                Margin = new Padding(6),
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                Tag = (prompt, isNav)
            };
            card.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var r = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using var path = GraphicsExtensions.CreateRoundedRect(r, DesignTokens.CornerRadius);
                e.Graphics.FillPath(Brushes.White, path);
                using var pen = new Pen(DesignTokens.Border);
                e.Graphics.DrawPath(pen, path);
            };

            card.Controls.Add(new Label
            {
                Text = icon,
                Location = new Point(16, 16),
                AutoSize = true,
                Font = new Font(DesignTokens.FontFamily, 16f),
                BackColor = Color.Transparent
            });
            card.Controls.Add(new Label
            {
                Text = label,
                Location = new Point(16, 48),
                AutoSize = true,
                Font = new Font(DesignTokens.FontFamily, 10f, FontStyle.Bold),
                ForeColor = DesignTokens.Text,
                BackColor = Color.Transparent
            });

            var captured = (prompt, isNav);
            void OnCardClick()
            {
                if (captured.isNav) CreateDashboardRequested?.Invoke(this, EventArgs.Empty);
                else PromptSelected?.Invoke(this, captured.prompt);
            }
            card.Click += (_, _) => OnCardClick();
            foreach (Control c in card.Controls) c.Click += (_, _) => OnCardClick();

            grid.Controls.Add(card);
        }

        Controls.Add(grid);
    }
}
