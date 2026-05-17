using DataFlow.Core.Enums;
using DataFlow.Core.Interfaces;
using DataFlow.Core.Models;
using DataFlow.Core.Navigation;
using DataFlow.Core.Themes;
using DataFlow.UI.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace DataFlow.UI.Views;

public sealed class InsightsView : UserControl, INavigationAware
{
    private readonly IDataFlowApiClient _api;
    private readonly ILocalAnalyticsService _local;
    private readonly INavigationService _navigation;
    private readonly FlowLayoutPanel _content;
    private readonly Label _summary;
    private readonly Label _status;
    private string? _aiSessionId;
    private int _projectId;

    public InsightsView(IServiceProvider services)
    {
        _api = services.GetRequiredService<IDataFlowApiClient>();
        _local = services.GetRequiredService<ILocalAnalyticsService>();
        _navigation = services.GetRequiredService<INavigationService>();
        BackColor = Color.FromArgb(17, 19, 24);
        ForeColor = Color.White;
        Dock = DockStyle.Fill;

        var header = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.FromArgb(26, 29, 40), Padding = new Padding(16, 12, 16, 0) };
        var back = new Button { Text = "← Back", FlatStyle = FlatStyle.Flat, ForeColor = Color.WhiteSmoke, AutoSize = true };
        back.Click += (_, _) => _navigation.Navigate(NavigationArgs.For(AppView.ProjectDetail, _projectId));
        var title = new Label { Text = "Auto Insights", Location = new Point(90, 8), AutoSize = true, Font = new Font(DesignTokens.FontFamily, 14f, FontStyle.Bold), ForeColor = Color.White };
        var refresh = new Button { Text = "Refresh", Location = new Point(700, 8), Size = new Size(80, 28), FlatStyle = FlatStyle.Flat, BackColor = DesignTokens.Accent, ForeColor = Color.White };
        refresh.FlatAppearance.BorderSize = 0;
        refresh.Click += async (_, _) => await LoadInsightsAsync();
        header.Controls.AddRange([back, title, refresh]);

        _summary = new Label
        {
            Dock = DockStyle.Top,
            Height = 60,
            Padding = new Padding(20, 12, 20, 0),
            ForeColor = Color.FromArgb(180, 185, 195),
            Font = new Font(DesignTokens.FontFamily, 10f)
        };

        _status = new Label { Dock = DockStyle.Top, Height = 24, ForeColor = DesignTokens.TextDim, Padding = new Padding(20, 0, 0, 0) };

        _content = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(16)
        };

        Controls.Add(_content);
        Controls.Add(_status);
        Controls.Add(_summary);
        Controls.Add(header);
    }

    public void OnNavigatedTo(NavigationArgs args)
    {
        if (args.ProjectId is int pid) _projectId = pid;
        _ = InitAndLoadAsync();
    }

    private async Task InitAndLoadAsync()
    {
        _status.Text = "Preparing dataset session…";
        try
        {
            var datasets = await _api.ListDatasetsAsync(_projectId);
            var ds = datasets.FirstOrDefault();
            if (ds == null)
            {
                _status.Text = "Upload a dataset on the project page first.";
                return;
            }
            var revive = await _api.ReviveSessionAsync(ds.Id);
            _aiSessionId = revive.AiSessionId;
            if (string.IsNullOrEmpty(_aiSessionId))
            {
                _status.Text = "Could not revive AI session. Open project chat first.";
                return;
            }
            await LoadInsightsAsync();
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
        }
    }

    private async Task LoadInsightsAsync()
    {
        if (string.IsNullOrEmpty(_aiSessionId)) return;
        _status.Text = "Detecting insights…";
        _content.Controls.Clear();
        try
        {
            var report = await _api.GetInsightsAsync(_aiSessionId);
            if (report == null)
            {
                var datasets = await _api.ListDatasetsAsync(_projectId);
                var ds = datasets.FirstOrDefault();
                if (ds != null)
                {
                    var chartData = await _api.GetChartDataAsync(ds.Id);
                    report = _local.ComputeBasicInsights(chartData);
                    _status.Text = "Using embedded C# insights (API unavailable)";
                }
                else
                {
                    _status.Text = "No insights returned. Ensure backend session is active.";
                    return;
                }
            }

            _summary.Text = report.SummaryText ?? "";
            _status.Text = $"{report.TotalInsights} insights · {report.HighPriority} high priority";

            AddSection("📈 Trends", report.Trends);
            AddSection("🔗 Correlations", report.Correlations);
            AddSection("⚠ Anomalies", report.Anomalies);
            AddSection("🌀 Seasonality", report.Seasonality);
            AddSection("⚡ KPI Alerts", report.KpiAlerts);
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
        }
    }

    private void AddSection(string title, List<InsightItemDto> items)
    {
        if (items.Count == 0) return;

        var hdr = new Label
        {
            Text = $"{title} ({items.Count})",
            AutoSize = false,
            Width = 700,
            Height = 28,
            Font = new Font(DesignTokens.FontFamily, 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(200, 205, 215)
        };
        _content.Controls.Add(hdr);

        foreach (var item in items)
        {
            var card = new RoundedPanel
            {
                Width = 700,
                Height = 72,
                Margin = new Padding(0, 0, 0, 8),
                BackColor = Color.FromArgb(26, 29, 40),
                CornerRadius = 10
            };
            var msg = new Label
            {
                Text = item.Message ?? "",
                Location = new Point(14, 12),
                Size = new Size(670, 36),
                ForeColor = Color.FromArgb(220, 225, 235),
                Font = new Font(DesignTokens.FontFamily, 9.5f)
            };
            var meta = new Label
            {
                Text = BuildMeta(item),
                Location = new Point(14, 48),
                AutoSize = true,
                ForeColor = Color.FromArgb(120, 125, 140),
                Font = new Font(DesignTokens.FontFamily, 8.5f)
            };
            card.Controls.Add(msg);
            card.Controls.Add(meta);
            _content.Controls.Add(card);
        }
    }

    private static string BuildMeta(InsightItemDto item)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(item.Column)) parts.Add(item.Column);
        if (!string.IsNullOrEmpty(item.ColA)) parts.Add($"{item.ColA} ↔ {item.ColB}");
        if (!string.IsNullOrEmpty(item.Kpi)) parts.Add(item.Kpi);
        if (item.ChangePct.HasValue) parts.Add($"{(item.ChangePct >= 0 ? "▲" : "▼")} {Math.Abs(item.ChangePct.Value):0.#}%");
        if (item.Correlation.HasValue) parts.Add($"r={item.Correlation:0.##}");
        if (!string.IsNullOrEmpty(item.Severity)) parts.Add(item.Severity);
        return string.Join(" · ", parts);
    }
}
