using System.Linq;
using DataFlow.Application.Analytics;
using DataFlow.Core.Models;
using DataFlow.Core.Themes;
using DataFlow.UI.Helpers;
using LiveChartsCore;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WinForms;
using SkiaSharp;

namespace DataFlow.UI.Controls.Charts;

public sealed class ChartWidgetHost : UserControl
{
    private Control? _active;
    private DashboardWidgetModel? _cfg;

    public event EventHandler<ChartDrillDownEventArgs>? DrillDownRequested;

    public void Render(
        DashboardWidgetModel cfg,
        IReadOnlyList<Dictionary<string, object?>> data,
        ChartScheme scheme)
    {
        _cfg = cfg;
        Controls.Clear();
        _active?.Dispose();
        _active = null;

        if (cfg.Type is "kpi")
            _active = BuildKpi(cfg, data, scheme);
        else if (cfg.Type is "table")
            _active = BuildTable(data, scheme);
        else if (cfg.Type is "ranking")
            _active = BuildRanking(cfg, data, scheme);
        else
            _active = BuildChart(cfg, data, scheme);

        _active.Dock = DockStyle.Fill;
        Controls.Add(_active);
    }

    private void RaiseDrill(string column, string value)
    {
        if (string.IsNullOrWhiteSpace(column) || string.IsNullOrWhiteSpace(value)) return;
        DrillDownRequested?.Invoke(this, new ChartDrillDownEventArgs
        {
            Column = column,
            Value = value,
            WidgetId = _cfg?.Id
        });
    }

    private static Control BuildKpi(DashboardWidgetModel cfg, IReadOnlyList<Dictionary<string, object?>> data, ChartScheme scheme)
    {
        var value = data.FirstOrDefault()?.GetValueOrDefault("value");
        var num = value is null ? 0 : Convert.ToDouble(value);
        var panel = new Panel { BackColor = scheme.Card };
        var lbl = new Label
        {
            Text = FormatNumber(num),
            Font = new Font("Segoe UI", 28f, FontStyle.Bold),
            ForeColor = scheme.Text,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(16, 0, 0, 0)
        };
        var title = new Label
        {
            Text = (cfg.Title ?? "KPI").ToUpperInvariant(),
            Dock = DockStyle.Top,
            Height = 24,
            ForeColor = scheme.Muted,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            Padding = new Padding(16, 8, 0, 0)
        };
        panel.Controls.Add(lbl);
        panel.Controls.Add(title);
        return panel;
    }

    private static Control BuildTable(IReadOnlyList<Dictionary<string, object?>> data, ChartScheme scheme)
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            BackgroundColor = scheme.Card,
            GridColor = scheme.Border,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false
        };
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 30, 35);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = scheme.Text;
        grid.DefaultCellStyle.BackColor = scheme.Card;
        grid.DefaultCellStyle.ForeColor = scheme.Text;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 60, 70);

        if (data.Count == 0) return grid;

        foreach (var key in data[0].Keys)
            grid.Columns.Add(key, key);

        foreach (var row in data.Take(100))
        {
            var values = row.Values.Select(v => Convert.ToString(v) ?? "").Cast<object>().ToArray();
            grid.Rows.Add(values);
        }

        return grid;
    }

    private Control BuildRanking(DashboardWidgetModel cfg, IReadOnlyList<Dictionary<string, object?>> data, ChartScheme scheme)
    {
        var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = scheme.Card };
        var points = WidgetDataComputer.AsPoints(data, "ranking").Take(10).ToList();
        var max = points.Count > 0 ? points.Max(p => p.Value) : 1;
        var col = cfg.XCol ?? "category";
        var y = 8;
        for (var i = 0; i < points.Count; i++)
        {
            var p = points[i];
            var row = new Panel
            {
                Location = new Point(8, y),
                Width = 280,
                Height = 32,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            var rank = new Label { Text = $"{i + 1}", Location = new Point(0, 8), Width = 20, ForeColor = scheme.Muted, Font = new Font("Segoe UI", 9f) };
            var name = new Label { Text = p.Name, Location = new Point(24, 4), Width = 160, ForeColor = scheme.Text, Font = new Font("Segoe UI", 9f) };
            var val = new Label { Text = FormatNumber(p.Value), Location = new Point(190, 8), AutoSize = true, ForeColor = scheme.Muted, Font = new Font("Consolas", 9f) };
            var barBg = new Panel { Location = new Point(24, 22), Size = new Size(200, 4), BackColor = scheme.Border };
            var pct = max > 0 ? (int)(p.Value / max * 200) : 0;
            var bar = new Panel { Location = new Point(0, 0), Size = new Size(Math.Max(4, pct), 4), BackColor = scheme.Palette[i % scheme.Palette.Length] };
            barBg.Controls.Add(bar);
            row.Controls.AddRange([rank, name, val, barBg]);

            var captured = p.Name;
            row.Click += (_, _) => RaiseDrill(col, captured);
            foreach (Control c in row.Controls)
                c.Click += (_, _) => RaiseDrill(col, captured);

            panel.Controls.Add(row);
            y += 36;
        }
        return panel;
    }

    private Control BuildChart(
        DashboardWidgetModel cfg,
        IReadOnlyList<Dictionary<string, object?>> data,
        ChartScheme scheme)
    {
        var points = WidgetDataComputer.AsPoints(data, cfg.Type);
        if (points.Count == 0)
        {
            return new Label
            {
                Text = "Configure columns in widget settings",
                ForeColor = scheme.Muted,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
        }

        if (cfg.Type is "pie" or "donut")
        {
            var pie = new PieChart { Dock = DockStyle.Fill, BackColor = scheme.Card };
            pie.Series = points.Select((p, i) => new PieSeries<double>
            {
                Values = new[] { p.Value },
                Name = p.Name,
                Fill = new SolidColorPaint(ToSkColor(scheme.Palette[i % scheme.Palette.Length]))
            }).Cast<ISeries>().ToArray();

            if (!string.IsNullOrEmpty(cfg.XCol))
            {
                pie.DataPointerDown += (_, pts) =>
                {
                    var pt = pts.FirstOrDefault();
                    if (pt?.Context.Series is ISeries s && !string.IsNullOrEmpty(s.Name))
                        RaiseDrill(cfg.XCol!, s.Name!);
                };
            }
            return pie;
        }

        if (cfg.Type == "radar")
        {
            var polar = new PolarChart { Dock = DockStyle.Fill, BackColor = scheme.Card };
            polar.Series =
            [
                new PolarLineSeries<double>
                {
                    Values = points.Select(p => p.Value).ToArray(),
                    Fill = new SolidColorPaint(ToSkColor(scheme.Palette[0], 60)),
                    Stroke = new SolidColorPaint(ToSkColor(scheme.Palette[0]), 2)
                }
            ];
            polar.RadiusAxes = [new PolarAxis { MinLimit = 0 }];
            return polar;
        }

        var chart = new CartesianChart { Dock = DockStyle.Fill, BackColor = scheme.Card };
        var labels = points.Select(p => p.Name).ToArray();
        var values = points.Select(p => p.Value).ToArray();
        var color = ToSkColor(scheme.Palette[0]);

        if (cfg.Type == "scatter")
        {
            chart.Series =
            [
                new ScatterSeries<LiveChartsCore.Defaults.ObservablePoint>
                {
                    Values = points.Select(p => new LiveChartsCore.Defaults.ObservablePoint(p.X, p.Y)).ToArray(),
                    Fill = new SolidColorPaint(color)
                }
            ];
            chart.XAxes = [new Axis { Name = cfg.XCol, LabelsPaint = new SolidColorPaint(SKColors.LightGray) }];
            chart.YAxes = [new Axis { Name = cfg.YCol, LabelsPaint = new SolidColorPaint(SKColors.LightGray) }];
            return chart;
        }

        ISeries series = cfg.Type switch
        {
            "line" => new LineSeries<double>
            {
                Values = values,
                Fill = null,
                Stroke = new SolidColorPaint(color, 2),
                GeometryFill = new SolidColorPaint(color),
                GeometryStroke = null
            },
            "area" => new LineSeries<double>
            {
                Values = values,
                Fill = new SolidColorPaint(ToSkColor(scheme.Palette[0], 80)),
                Stroke = new SolidColorPaint(color, 2),
                GeometryFill = null,
                GeometryStroke = null
            },
            _ => new ColumnSeries<double>
            {
                Values = values,
                Fill = new SolidColorPaint(color),
                Stroke = null
            }
        };

        chart.Series = [series];
        chart.XAxes = [new Axis { Labels = labels, LabelsRotation = labels.Length > 8 ? 30 : 0, TextSize = 10, LabelsPaint = new SolidColorPaint(SKColors.LightGray) }];
        chart.YAxes = [new Axis { Labeler = v => FormatNumber(v), TextSize = 10, LabelsPaint = new SolidColorPaint(SKColors.LightGray) }];

        if (!string.IsNullOrEmpty(cfg.XCol))
        {
            chart.DataPointerDown += (_, pts) =>
            {
                var pt = pts.FirstOrDefault();
                if (pt is null) return;
                var idx = (int)pt.Index;
                if (idx >= 0 && idx < labels.Length)
                    RaiseDrill(cfg.XCol!, labels[idx]);
            };
        }

        return chart;
    }

    private static SKColor ToSkColor(Color c, byte alpha = 255) => new(c.R, c.G, c.B, alpha);

    private static string FormatNumber(double n)
    {
        var abs = Math.Abs(n);
        if (abs >= 1e9) return (n / 1e9).ToString("0.#") + "B";
        if (abs >= 1e6) return (n / 1e6).ToString("0.#") + "M";
        if (abs >= 1e3) return (n / 1e3).ToString("0.#") + "K";
        return n % 1 == 0 ? n.ToString("0") : n.ToString("0.##");
    }
}
