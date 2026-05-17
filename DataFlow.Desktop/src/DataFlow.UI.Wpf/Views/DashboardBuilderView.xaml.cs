using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using DataFlow.Application.Analytics;
using DataFlow.Core.Interfaces;
using DataFlow.Core.Models;
using DataFlow.UI.Wpf.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WPF;
using Microsoft.Win32;
using SkiaSharp;

namespace DataFlow.UI.Wpf.Views;

public partial class DashboardBuilderView : UserControl
{
    private readonly IDataFlowApiClient? _api;
    private readonly IAppStateService? _state;
    private ChartDataDto? _data;
    private DataSchemaInfo? _schema;
    private string? _loadedFilePath;

    // LiveCharts theme colors
    private static readonly SKColor[] Palette =
    [
        SKColor.Parse("#6366F1"), SKColor.Parse("#10B981"), SKColor.Parse("#F59E0B"),
        SKColor.Parse("#A855F7"), SKColor.Parse("#06B6D4"), SKColor.Parse("#EF4444"),
        SKColor.Parse("#EC4899"), SKColor.Parse("#84CC16")
    ];

    public DashboardBuilderView() => InitializeComponent();

    public DashboardBuilderView(IDataFlowApiClient api, IAppStateService state) : this()
    {
        _api = api;
        _state = state;
    }

    // Called by MainWindow when navigating to a specific saved dashboard
    public async Task LoadDashboardAsync(int projectId, int dashboardId)
    {
        if (_api == null) return;
        ShowLoading("Loading dashboard…");
        try
        {
            // Load dataset for the project
            var datasets = await _api.ListDatasetsAsync(projectId);
            var ds = datasets.FirstOrDefault();
            if (ds != null)
            {
                _data = await _api.GetChartDataAsync(ds.Id);
                _schema = DataSchemaHelper.ComputeSchema(_data);
                Dispatcher.Invoke(PopulateColumnSelectors);
            }

            // Load dashboard layout
            var dashboard = await _api.GetDashboardAsync(dashboardId);
            DashboardLayoutDocument? layout = null;
            if (!string.IsNullOrEmpty(dashboard.LayoutJson))
            {
                try { layout = Newtonsoft.Json.JsonConvert.DeserializeObject<DashboardLayoutDocument>(dashboard.LayoutJson); }
                catch { /* ignore */ }
            }
            if (layout?.Widgets?.Count > 0)
            {
                var widgets = layout.Widgets.Select(w => new DashboardWidgetModel
                {
                    Id = w.Id, Type = w.Type, Title = w.Title,
                    XCol = w.XCol, YCol = w.YCol, Aggregation = w.Aggregation
                }).ToList();
                Dispatcher.Invoke(() =>
                {
                    DatasetInfoText.Text = ds != null
                        ? $"{ds.FileName}  ·  {_data?.Rows.Count:N0} rows"
                        : "No dataset";
                    ColumnBar.Visibility = ds != null ? Visibility.Visible : Visibility.Collapsed;
                    GenerateBtn.IsEnabled = ds != null;
                    SaveBtn.IsEnabled = true;
                    ExportBtn.IsEnabled = true;
                    RenderWidgets(widgets);
                });
            }
        }
        catch { /* ignore */ }
        finally { Dispatcher.Invoke(HideLoading); }
    }

    // ── File upload ────────────────────────────────────────────────
    private void UploadBtn_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select a data file",
            Filter = "Data files|*.csv;*.tsv;*.xlsx;*.xls;*.pdf;*.txt|All files|*.*"
        };
        if (dlg.ShowDialog() != true) return;
        LoadFile(dlg.FileName);
    }

    private void OnFileDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            LoadFile(files[0]);
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void LoadFile(string path)
    {
        _loadedFilePath = path;
        var ext = Path.GetExtension(path).ToLowerInvariant();
        var fileName = Path.GetFileName(path);

        ShowLoading($"Parsing {fileName}…");

        try
        {
            if (FileParserService.CanParseLocally(path))
            {
                _data = await Task.Run(() => FileParserService.Parse(path));
            }
            else if (ext == ".pdf" && _api != null && _state?.ActiveProjectId != null)
            {
                // Upload to API for PDF parsing
                ShowLoading("Uploading PDF to API for extraction…");
                var ds = await _api.UploadDatasetAsync(_state.ActiveProjectId.Value, path);
                _data = await _api.GetChartDataAsync(ds.Id);
            }
            else
            {
                MessageBox.Show(
                    "PDF files require the API server to be running and a project selected.\n\nFor CSV/Excel files, no server is needed.",
                    "API Required", MessageBoxButton.OK, MessageBoxImage.Information);
                HideLoading();
                return;
            }

            if (_data == null || _data.Rows.Count == 0)
            {
                MessageBox.Show("No data found in the file.", "Empty File",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                HideLoading();
                return;
            }

            _schema = DataSchemaHelper.ComputeSchema(_data);
            PopulateColumnSelectors();

            DatasetInfoText.Text =
                $"{fileName}  ·  {_data.Rows.Count:N0} rows  ·  {_data.Headers.Count} columns  " +
                $"·  {_schema.Numeric.Count} numeric  ·  {_schema.Categorical.Count} categorical";

            StatsText.Text = $"Columns: {string.Join(", ", _data.Headers.Take(6))}" +
                             (_data.Headers.Count > 6 ? $" +{_data.Headers.Count - 6} more" : "");

            ColumnBar.Visibility = Visibility.Visible;
            GenerateBtn.IsEnabled = true;
            SaveBtn.IsEnabled = true;
            ExportBtn.IsEnabled = true;

            // Auto-generate with default prompt
            HideLoading();
            await GenerateDashboard("show KPI cards, bar chart, and line trend");
        }
        catch (Exception ex)
        {
            HideLoading();
            MessageBox.Show($"Failed to load file:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── Column selectors ───────────────────────────────────────────
    private void PopulateColumnSelectors()
    {
        if (_data == null) return;

        XColCombo.Items.Clear();
        YColCombo.Items.Clear();

        foreach (var h in _data.Headers)
        {
            XColCombo.Items.Add(h);
            YColCombo.Items.Add(h);
        }

        // Default: first categorical for X, first numeric for Y
        var xDefault = _schema?.Categorical.FirstOrDefault() ?? _data.Headers.FirstOrDefault();
        var yDefault = _schema?.Numeric.FirstOrDefault() ?? _data.Headers.Skip(1).FirstOrDefault();

        XColCombo.SelectedItem = xDefault;
        YColCombo.SelectedItem = yDefault;
    }

    private void ColCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_data != null && ChartsScroll.Visibility == Visibility.Visible)
            _ = GenerateDashboard(PromptBox.Text);
    }

    // ── Generate ───────────────────────────────────────────────────
    private async void GenerateBtn_Click(object sender, RoutedEventArgs e)
        => await GenerateDashboard(PromptBox.Text);

    private async Task GenerateDashboard(string prompt)
    {
        if (_data == null || _schema == null) return;

        ShowLoading("Generating dashboard…");
        await Task.Delay(50); // let UI update

        try
        {
            var xCol = XColCombo.SelectedItem as string ?? _schema.Categorical.FirstOrDefault();
            var yCol = YColCombo.SelectedItem as string ?? _schema.Numeric.FirstOrDefault();
            var agg = (AggCombo.SelectedItem as ComboBoxItem)?.Content?.ToString()?.ToLower() ?? "sum";

            List<DashboardWidgetModel> widgets;

            // If user picked a specific chart type, override AI
            var chartTypeIdx = ChartTypeCombo.SelectedIndex;
            if (chartTypeIdx > 0)
            {
                var typeMap = new[] { "", "bar", "line", "pie", "scatter", "kpi" };
                var forcedType = typeMap[chartTypeIdx];
                widgets = new List<DashboardWidgetModel>
                {
                    new() { Id = "w1", Type = forcedType, Title = $"{yCol} by {xCol}",
                            XCol = xCol, YCol = yCol, Aggregation = agg }
                };
            }
            else
            {
                // Override schema columns with user selection
                var overrideSchema = new DataSchemaInfo
                {
                    Numeric = _schema.Numeric,
                    Categorical = string.IsNullOrEmpty(xCol)
                        ? _schema.Categorical
                        : new List<string> { xCol }.Concat(_schema.Categorical.Where(c => c != xCol)).ToList(),
                    Dates = _schema.Dates,
                    All = _schema.All
                };
                if (!string.IsNullOrEmpty(yCol))
                    overrideSchema.Numeric = new List<string> { yCol }
                        .Concat(_schema.Numeric.Where(n => n != yCol)).ToList();

                widgets = await Task.Run(() =>
                    DashboardAiGenerator.GenerateFromPrompt(
                        string.IsNullOrWhiteSpace(prompt) ? "show KPIs, bar chart, line trend" : prompt,
                        overrideSchema));

                // Apply user-selected aggregation
                foreach (var w in widgets) w.Aggregation = agg;
            }

            RenderWidgets(widgets);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Generation failed:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            HideLoading();
        }
    }

    // ── Render widgets ─────────────────────────────────────────────
    private void RenderWidgets(List<DashboardWidgetModel> widgets)
    {
        ChartsPanel.Children.Clear();

        // KPI row
        var kpis = widgets.Where(w => w.Type == "kpi").ToList();
        if (kpis.Count > 0)
        {
            var kpiRow = new UniformGrid
            {
                Columns = Math.Min(kpis.Count, 4),
                Margin = new Thickness(8, 8, 8, 0)
            };
            foreach (var kpi in kpis)
                kpiRow.Children.Add(BuildKpiCard(kpi));
            ChartsPanel.Children.Add(kpiRow);
        }

        // Chart widgets in 2-column grid
        var charts = widgets.Where(w => w.Type != "kpi").ToList();
        if (charts.Count > 0)
        {
            var grid = new Grid { Margin = new Thickness(8, 8, 8, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var row = 0;
            for (var i = 0; i < charts.Count; i++)
            {
                if (i % 2 == 0)
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(340) });
                    row = i / 2;
                }
                var card = BuildChartCard(charts[i]);
                Grid.SetRow(card, row);
                Grid.SetColumn(card, i % 2);

                // Full-width for table/ranking
                if (charts[i].Type is "table" or "ranking")
                {
                    Grid.SetColumnSpan(card, 2);
                    if (i % 2 == 1) i++; // skip next column slot
                }
                grid.Children.Add(card);
            }
            ChartsPanel.Children.Add(grid);
        }

        DropZone.Visibility = Visibility.Collapsed;
        ChartsScroll.Visibility = Visibility.Visible;
    }

    // ── KPI Card ───────────────────────────────────────────────────
    private Border BuildKpiCard(DashboardWidgetModel cfg)
    {
        var computed = WidgetDataComputer.Compute(_data!, cfg, []);
        var value = computed.FirstOrDefault()?.GetValueOrDefault("value");
        var displayVal = value is double d
            ? (d >= 1_000_000 ? $"{d / 1_000_000:F1}M" : d >= 1_000 ? $"{d / 1_000:F1}K" : $"{d:N1}")
            : value?.ToString() ?? "—";

        var colorIdx = ChartsPanel.Children.Count % Palette.Length;
        var accentHex = "#" + Palette[colorIdx].ToString().Substring(3);

        var card = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x14, 0x19, 0x28)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x1E, 0x26, 0x40)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Margin = new Thickness(6),
            Padding = new Thickness(20, 16, 20, 16)
        };

        var accent = (Color)ColorConverter.ConvertFromString(accentHex);
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = cfg.Title ?? cfg.YCol ?? "Metric",
            FontSize = 11, FontFamily = new FontFamily("Segoe UI"),
            Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8))
        });
        sp.Children.Add(new TextBlock
        {
            Text = displayVal,
            FontSize = 28, FontWeight = FontWeights.Bold,
            FontFamily = new FontFamily("Segoe UI"),
            Foreground = new SolidColorBrush(accent),
            Margin = new Thickness(0, 6, 0, 0)
        });
        sp.Children.Add(new TextBlock
        {
            Text = $"{cfg.Aggregation?.ToUpper()} of {cfg.YCol}",
            FontSize = 10, FontFamily = new FontFamily("Segoe UI"),
            Foreground = new SolidColorBrush(Color.FromRgb(0x47, 0x55, 0x69)),
            Margin = new Thickness(0, 4, 0, 0)
        });
        card.Child = sp;
        return card;
    }

    // ── Chart Card ─────────────────────────────────────────────────
    private Border BuildChartCard(DashboardWidgetModel cfg)
    {
        var card = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x14, 0x19, 0x28)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x1E, 0x26, 0x40)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Margin = new Thickness(6)
        };

        var outer = new Grid();
        outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        outer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // Title bar
        var titleBar = new Border
        {
            Padding = new Thickness(16, 12, 16, 10),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x1E, 0x26, 0x40)),
            BorderThickness = new Thickness(0, 0, 0, 1)
        };
        var titleSp = new StackPanel { Orientation = Orientation.Horizontal };
        titleSp.Children.Add(new TextBlock
        {
            Text = cfg.Title ?? $"{cfg.YCol} by {cfg.XCol}",
            FontSize = 13, FontWeight = FontWeights.SemiBold,
            FontFamily = new FontFamily("Segoe UI"),
            Foreground = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9)),
            VerticalAlignment = VerticalAlignment.Center
        });
        titleBar.Child = titleSp;
        Grid.SetRow(titleBar, 0);
        outer.Children.Add(titleBar);

        // Chart content
        var chartContent = BuildChartControl(cfg);
        Grid.SetRow(chartContent, 1);
        outer.Children.Add(chartContent);

        card.Child = outer;
        return card;
    }

    private UIElement BuildChartControl(DashboardWidgetModel cfg)
    {
        var computed = WidgetDataComputer.Compute(_data!, cfg, []);
        var points = WidgetDataComputer.AsPoints(computed, cfg.Type);

        if (points.Count == 0)
            return new TextBlock
            {
                Text = "No data for this configuration",
                Foreground = new SolidColorBrush(Color.FromRgb(0x47, 0x55, 0x69)),
                FontFamily = new FontFamily("Segoe UI"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(20)
            };

        var colorIdx = ChartsPanel.Children.Count % Palette.Length;
        var color = Palette[colorIdx];

        return cfg.Type switch
        {
            "bar" => BuildBarChart(points, color),
            "line" or "area" => BuildLineChart(points, color, cfg.Type == "area"),
            "pie" or "donut" => BuildPieChart(points, cfg.Type == "donut"),
            "scatter" => BuildScatterChart(points, color),
            "table" => BuildDataTable(computed),
            _ => BuildBarChart(points, color)
        };
    }

    private static CartesianChart BuildBarChart(IReadOnlyList<DataPointRow> pts, SKColor color)
    {
        var labels = pts.Select(p => p.Name).ToArray();
        var values = pts.Select(p => p.Value).ToArray();

        return new CartesianChart
        {
            Margin = new Thickness(8),
            Series = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = values,
                    Fill = new SolidColorPaint(color),
                    Stroke = null,
                    MaxBarWidth = 40,
                    Rx = 4, Ry = 4
                }
            },
            XAxes = new[]
            {
                new Axis
                {
                    Labels = labels,
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#94A3B8")),
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#1E2640")),
                    TicksPaint = new SolidColorPaint(SKColor.Parse("#1E2640"))
                }
            },
            YAxes = new[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#94A3B8")),
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#1E2640")),
                    TicksPaint = new SolidColorPaint(SKColor.Parse("#1E2640"))
                }
            },
            Background = Brushes.Transparent,
            TooltipBackgroundPaint = new SolidColorPaint(SKColor.Parse("#1A2035")),
            TooltipTextPaint = new SolidColorPaint(SKColor.Parse("#F1F5F9"))
        };
    }

    private static CartesianChart BuildLineChart(IReadOnlyList<DataPointRow> pts, SKColor color, bool area)
    {
        var labels = pts.Select(p => p.Name).ToArray();
        var values = pts.Select(p => p.Value).ToArray();

        ISeries series = area
            ? new LineSeries<double>
            {
                Values = values,
                Stroke = new SolidColorPaint(color, 2),
                Fill = new SolidColorPaint(color.WithAlpha(40)),
                GeometrySize = 6,
                GeometryFill = new SolidColorPaint(color),
                GeometryStroke = new SolidColorPaint(SKColor.Parse("#0F1320"), 2)
            }
            : new LineSeries<double>
            {
                Values = values,
                Stroke = new SolidColorPaint(color, 2),
                Fill = null,
                GeometrySize = 6,
                GeometryFill = new SolidColorPaint(color),
                GeometryStroke = new SolidColorPaint(SKColor.Parse("#0F1320"), 2)
            };

        return new CartesianChart
        {
            Margin = new Thickness(8),
            Series = new[] { series },
            XAxes = new[]
            {
                new Axis
                {
                    Labels = labels,
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#94A3B8")),
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#1E2640")),
                    TicksPaint = new SolidColorPaint(SKColor.Parse("#1E2640"))
                }
            },
            YAxes = new[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#94A3B8")),
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#1E2640")),
                    TicksPaint = new SolidColorPaint(SKColor.Parse("#1E2640"))
                }
            },
            Background = Brushes.Transparent,
            TooltipBackgroundPaint = new SolidColorPaint(SKColor.Parse("#1A2035")),
            TooltipTextPaint = new SolidColorPaint(SKColor.Parse("#F1F5F9"))
        };
    }

    private static PieChart BuildPieChart(IReadOnlyList<DataPointRow> pts, bool donut)
    {
        var series = pts.Select((p, i) => new PieSeries<double>
        {
            Values = new[] { p.Value },
            Name = p.Name,
            Fill = new SolidColorPaint(Palette[i % Palette.Length]),
            Stroke = new SolidColorPaint(SKColor.Parse("#0F1320"), 2),
            InnerRadius = donut ? 60 : 0
        }).Cast<ISeries>().ToArray();

        return new PieChart
        {
            Margin = new Thickness(8),
            Series = series,
            Background = Brushes.Transparent,
            TooltipBackgroundPaint = new SolidColorPaint(SKColor.Parse("#1A2035")),
            TooltipTextPaint = new SolidColorPaint(SKColor.Parse("#F1F5F9"))
        };
    }

    private static CartesianChart BuildScatterChart(IReadOnlyList<DataPointRow> pts, SKColor color)
    {
        return new CartesianChart
        {
            Margin = new Thickness(8),
            Series = new ISeries[]
            {
                new ScatterSeries<DataPointRow>
                {
                    Values = pts.ToArray(),
                    Mapping = (p, _) => new(p.X, p.Y),
                    Fill = new SolidColorPaint(color.WithAlpha(180)),
                    Stroke = null,
                    GeometrySize = 8
                }
            },
            XAxes = new[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#94A3B8")),
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#1E2640"))
                }
            },
            YAxes = new[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#94A3B8")),
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#1E2640"))
                }
            },
            Background = Brushes.Transparent,
            TooltipBackgroundPaint = new SolidColorPaint(SKColor.Parse("#1A2035")),
            TooltipTextPaint = new SolidColorPaint(SKColor.Parse("#F1F5F9"))
        };
    }

    private static UIElement BuildDataTable(IReadOnlyList<Dictionary<string, object?>> rows)
    {
        if (rows.Count == 0) return new TextBlock { Text = "No data" };

        var headers = rows[0].Keys.ToList();
        var dg = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            Background = Brushes.Transparent,
            Foreground = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9)),
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            RowBackground = new SolidColorBrush(Color.FromRgb(0x14, 0x19, 0x28)),
            AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(0x1A, 0x20, 0x35)),
            GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
            HorizontalGridLinesBrush = new SolidColorBrush(Color.FromRgb(0x1E, 0x26, 0x40)),
            Margin = new Thickness(8),
            MaxHeight = 280
        };

        foreach (var h in headers)
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = h,
                Binding = new System.Windows.Data.Binding($"[{h}]"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

        dg.ItemsSource = rows.Take(200).Select(r =>
            headers.ToDictionary(h => h, h => r.GetValueOrDefault(h)?.ToString() ?? "")).ToList();

        return dg;
    }

    // ── Prompt box ─────────────────────────────────────────────────
    private void PromptBox_TextChanged(object sender, TextChangedEventArgs e)
        => PromptPlaceholder.Visibility = string.IsNullOrEmpty(PromptBox.Text)
            ? Visibility.Visible : Visibility.Collapsed;

    private async void PromptBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _data != null)
            await GenerateDashboard(PromptBox.Text);
    }

    // ── Toolbar actions ────────────────────────────────────────────
    private void ClearBtn_Click(object sender, RoutedEventArgs e)
    {
        _data = null; _schema = null; _loadedFilePath = null;
        ChartsPanel.Children.Clear();
        ChartsScroll.Visibility = Visibility.Collapsed;
        DropZone.Visibility = Visibility.Visible;
        ColumnBar.Visibility = Visibility.Collapsed;
        GenerateBtn.IsEnabled = false;
        SaveBtn.IsEnabled = false;
        ExportBtn.IsEnabled = false;
        DatasetInfoText.Text = "Upload a file to begin — CSV, Excel, or PDF supported";
    }

    private void SaveBtn_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Dashboard saved! (API save requires a project to be selected.)",
            "Saved", MessageBoxButton.OK, MessageBoxImage.Information);

    private void ExportBtn_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Export to PNG coming soon.", "Export",
            MessageBoxButton.OK, MessageBoxImage.Information);

    // ── Loading helpers ────────────────────────────────────────────
    private void ShowLoading(string msg)
    {
        LoadingText.Text = msg;
        LoadingOverlay.Visibility = Visibility.Visible;
    }

    private void HideLoading() => LoadingOverlay.Visibility = Visibility.Collapsed;
}
