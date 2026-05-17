using DataFlow.Application.Analytics;
using DataFlow.Core.Enums;
using DataFlow.Core.Interfaces;
using DataFlow.Core.Models;
using DataFlow.Core.Navigation;
using DataFlow.Core.Themes;
using DataFlow.UI.Controls;
using DataFlow.UI.Controls.Dashboard;
using DataFlow.UI.Dialogs;
using DataFlow.UI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace DataFlow.UI.Views;

public sealed class DashboardBuilderView : UserControl, INavigationAware
{
    private readonly IDataFlowApiClient _api;
    private readonly INavigationService _navigation;
    private readonly DashboardCanvasControl _canvas;
    private readonly LoadingOverlayControl _loading;
    private readonly Panel _leftPanel;
    private readonly Panel _filterBar;
    private readonly FlowLayoutPanel _filterRow;
    private bool _filterVisible;
    private readonly TextBox _titleBox;
    private readonly Label _saveStatus;
    private readonly System.Windows.Forms.Timer _autoSaveTimer;

    private int _projectId;
    private int _dashboardId;
    private DatasetDto? _dataset;
    private ChartDataDto? _chartData;
    private DataSchemaInfo _schema = new();
    private ChartScheme _scheme = ChartSchemes.Get("Metric Flow");
    private bool _editMode = true;
    private readonly List<DashboardFilterModel> _filters = [];

    public DashboardBuilderView(IServiceProvider services)
    {
        _api = services.GetRequiredService<IDataFlowApiClient>();
        _navigation = services.GetRequiredService<INavigationService>();
        Dock = DockStyle.Fill;

        var topBar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.FromArgb(26, 29, 40), Padding = new Padding(12, 0, 12, 0) };

        var back = new Button { Text = "← Back", FlatStyle = FlatStyle.Flat, ForeColor = Color.WhiteSmoke, Location = new Point(8, 12), AutoSize = true };
        back.Click += (_, _) => _navigation.Navigate(NavigationArgs.For(AppView.ProjectDetail, _projectId));

        _titleBox = new TextBox
        {
            Location = new Point(100, 14),
            Width = 240,
            BackColor = Color.FromArgb(26, 29, 40),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.None,
            Font = new Font(DesignTokens.FontFamily, 12f, FontStyle.Bold),
            Text = "Dashboard"
        };

        var editBtn = new Button { Text = "Edit", Location = new Point(360, 12), Size = new Size(64, 28), FlatStyle = FlatStyle.Flat, ForeColor = Color.WhiteSmoke };
        editBtn.Click += (_, _) => { _editMode = !_editMode; editBtn.Text = _editMode ? "Editing" : "View"; _canvas.SetEditMode(_editMode); };

        var filterBtn = new Button { Text = "Filters", Location = new Point(430, 12), Size = new Size(64, 28), FlatStyle = FlatStyle.Flat, ForeColor = Color.WhiteSmoke };
        filterBtn.Click += (_, _) => ToggleFilters();

        var saveBtn = new Button { Text = "Save", Location = new Point(500, 12), Size = new Size(72, 28), FlatStyle = FlatStyle.Flat, BackColor = DesignTokens.Accent, ForeColor = Color.White };
        saveBtn.FlatAppearance.BorderSize = 0;
        saveBtn.Click += async (_, _) => await SaveAsync(false);

        var exportJsonBtn = new Button { Text = "JSON", Location = new Point(580, 12), Size = new Size(56, 28), FlatStyle = FlatStyle.Flat, ForeColor = Color.WhiteSmoke };
        exportJsonBtn.Click += (_, _) => ExportLayoutJson();

        var exportPngBtn = new Button { Text = "PNG", Location = new Point(642, 12), Size = new Size(56, 28), FlatStyle = FlatStyle.Flat, ForeColor = Color.WhiteSmoke };
        exportPngBtn.Click += (_, _) => ExportPng();

        _saveStatus = new Label { Location = new Point(708, 16), AutoSize = true, ForeColor = Color.FromArgb(160, 165, 175), Text = "" };

        topBar.Controls.AddRange([back, _titleBox, editBtn, filterBtn, saveBtn, exportJsonBtn, exportPngBtn, _saveStatus]);

        _filterRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Padding = new Padding(8) };
        var addFilter = new Button { Text = "+ Add filter", AutoSize = true, FlatStyle = FlatStyle.Flat, ForeColor = DesignTokens.Accent };
        addFilter.Click += (_, _) => AddFilter();
        var clearFilters = new Button { Text = "Clear all", AutoSize = true, FlatStyle = FlatStyle.Flat, ForeColor = Color.WhiteSmoke };
        clearFilters.Click += (_, _) => { _filters.Clear(); RefreshFilters(); _canvas.SetFilters(_filters); };
        _filterRow.Controls.Add(addFilter);
        _filterRow.Controls.Add(clearFilters);

        _filterBar = new Panel { Dock = DockStyle.Top, Height = 0, Visible = false, BackColor = Color.FromArgb(22, 25, 34) };
        _filterBar.Controls.Add(_filterRow);

        _leftPanel = BuildLeftPanel();

        _canvas = new DashboardCanvasControl { Dock = DockStyle.Fill };
        _canvas.LayoutChanged += (_, _) => ScheduleAutoSave();
        _canvas.WidgetConfigure += OnWidgetConfigure;
        _canvas.WidgetRemove += (_, _) => ScheduleAutoSave();
        _canvas.DrillDownRequested += OnDrillDown;

        _loading = new LoadingOverlayControl();

        var body = new Panel { Dock = DockStyle.Fill };
        body.Controls.Add(_loading);
        body.Controls.Add(_canvas);
        body.Controls.Add(_leftPanel);

        Controls.Add(body);
        Controls.Add(_filterBar);
        Controls.Add(topBar);

        _autoSaveTimer = new System.Windows.Forms.Timer { Interval = 2000 };
        _autoSaveTimer.Tick += async (_, _) =>
        {
            _autoSaveTimer.Stop();
            await SaveAsync(true);
        };

        BackColor = _scheme.Background;
    }

    public void OnNavigatedTo(NavigationArgs args)
    {
        if (args.ProjectId is not int pid || args.DashboardId is not int did) return;
        _projectId = pid;
        _dashboardId = did;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        _saveStatus.Text = "Loading…";
        _loading.Show("Loading dashboard…");
        try
        {
            var dash = await _api.GetDashboardAsync(_dashboardId);
            _titleBox.Text = dash.Name;
            _scheme = ChartSchemes.Get(dash.Scheme);
            BackColor = _scheme.Background;

            var datasets = await _api.ListDatasetsAsync(_projectId);
            _dataset = datasets.FirstOrDefault(d => d.Id == dash.DatasetId) ?? datasets.FirstOrDefault();
            if (_dataset != null)
            {
                _chartData = await _api.GetChartDataAsync(_dataset.Id);
                _schema = DataSchemaHelper.ComputeSchema(_chartData);
                var revive = await _api.ReviveSessionAsync(_dataset.Id);
                _aiSessionId = revive.AiSessionId;
            }

            _canvas.SetChartScheme(_scheme);
            _canvas.SetData(_chartData);

            var layout = ParseLayout(dash);
            if (layout?.Widgets.Count > 0)
            {
                foreach (var w in layout.Widgets)
                {
                    if (string.IsNullOrWhiteSpace(w.XCol)) w.XCol = _schema.Categorical.FirstOrDefault() ?? _schema.All.FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(w.YCol)) w.YCol = _schema.Numeric.FirstOrDefault();
                }
                _canvas.LoadWidgets(layout.Widgets);
                _editMode = false;
            }

            _saveStatus.Text = "";
        }
        catch (Exception ex)
        {
            _saveStatus.Text = "Load error";
            MessageBox.Show(ex.Message, "Dashboard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            _loading.HideOverlay();
        }
    }

    private void OnDrillDown(object? sender, ChartDrillDownEventArgs e)
    {
        if (_filters.Any(f => f.Col == e.Column && f.Op == "=" && f.Val == e.Value))
            return;

        _filters.Add(new DashboardFilterModel { Col = e.Column, Op = "=", Val = e.Value });
        _filterVisible = true;
        _filterBar.Visible = true;
        _filterBar.Height = 48;
        RefreshFilters();
        ApplyFilters();
    }

    private void ExportLayoutJson()
    {
        using var dlg = new SaveFileDialog
        {
            Filter = "JSON layout|*.json",
            FileName = $"{_titleBox.Text.Trim()}.json"
        };
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

        var doc = new DashboardLayoutDocument
        {
            Title = _titleBox.Text,
            Scheme = _scheme.Name,
            Widgets = _canvas.Widgets.ToList()
        };
        DashboardExportHelper.ExportLayoutJson(doc, dlg.FileName);
        _saveStatus.Text = "Exported JSON";
    }

    private void ExportPng()
    {
        using var dlg = new SaveFileDialog
        {
            Filter = "PNG image|*.png",
            FileName = $"{_titleBox.Text.Trim()}.png"
        };
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

        DashboardExportHelper.ExportPng(_canvas.ExportSurface, dlg.FileName);
        _saveStatus.Text = "Exported PNG";
    }

    private static DashboardLayoutDocument? ParseLayout(DashboardDto dash)
    {
        if (string.IsNullOrWhiteSpace(dash.LayoutJson)) return null;
        return JsonConvert.DeserializeObject<DashboardLayoutDocument>(dash.LayoutJson);
    }

    private Panel BuildLeftPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 200,
            BackColor = Color.FromArgb(26, 29, 40),
            Padding = new Padding(8)
        };

        var lbl = new Label
        {
            Text = "ADD CHARTS",
            Dock = DockStyle.Top,
            Height = 24,
            ForeColor = Color.FromArgb(140, 145, 155),
            Font = new Font(DesignTokens.FontFamily, 8f, FontStyle.Bold)
        };
        panel.Controls.Add(lbl);

        var list = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        foreach (var t in ChartTypeCatalog.All)
        {
            var btn = new Button
            {
                Text = t.Label,
                Width = 180,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(35, 38, 50),
                ForeColor = Color.FromArgb(200, 205, 215),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                Tag = t.Id,
                Cursor = Cursors.Hand
            };
            btn.MouseDown += (_, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                btn.DoDragDrop(t.Id, DragDropEffects.Copy);
            };
            btn.Click += (_, _) => _canvas.AddWidget(t.Id, t.Label);
            list.Controls.Add(btn);
        }

        var schemeLbl = new Label { Text = "THEME", Dock = DockStyle.Bottom, Height = 20, ForeColor = Color.Gray, Font = new Font(DesignTokens.FontFamily, 8f, FontStyle.Bold) };
        var schemeBox = new ComboBox { Dock = DockStyle.Bottom, DropDownStyle = ComboBoxStyle.DropDownList };
        schemeBox.Items.AddRange(ChartSchemes.All.Select(s => s.Name).Cast<object>().ToArray());
        schemeBox.SelectedIndex = 0;
        schemeBox.SelectedIndexChanged += (_, _) =>
        {
            _scheme = ChartSchemes.Get(schemeBox.SelectedItem?.ToString() ?? "Metric Flow");
            _canvas.SetChartScheme(_scheme);
            BackColor = _scheme.Background;
            ScheduleAutoSave();
        };

        var aiLbl = new Label { Text = "AI GENERATE", Dock = DockStyle.Bottom, Height = 20, ForeColor = Color.Gray, Font = new Font(DesignTokens.FontFamily, 8f, FontStyle.Bold) };
        var aiBox = new TextBox
        {
            Dock = DockStyle.Bottom,
            Height = 72,
            Multiline = true,
            BackColor = Color.FromArgb(35, 38, 50),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font(DesignTokens.FontFamily, 8.5f),
            Text = "4 KPI cards, monthly line chart, bar chart top 5 products…"
        };
        var genBtn = new Button
        {
            Text = "Generate Dashboard",
            Dock = DockStyle.Bottom,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(40, 44, 55),
            ForeColor = DesignTokens.Accent,
            Cursor = Cursors.Hand
        };
        genBtn.Click += async (_, _) => await GenerateFromPromptAsync(aiBox.Text);

        panel.Controls.Add(list);
        panel.Controls.Add(genBtn);
        panel.Controls.Add(aiBox);
        panel.Controls.Add(aiLbl);
        panel.Controls.Add(schemeBox);
        panel.Controls.Add(schemeLbl);
        return panel;
    }

    private void ToggleFilters()
    {
        _filterVisible = !_filterVisible;
        _filterBar.Visible = _filterVisible;
        _filterBar.Height = _filterVisible ? 48 : 0;
    }

    private void AddFilter()
    {
        if (_schema.All.Count == 0) return;
        _filters.Add(new DashboardFilterModel { Col = _schema.All[0] });
        RefreshFilters();
        _canvas.SetFilters(_filters);
    }

    private void RefreshFilters()
    {
        _filterRow.SuspendLayout();
        while (_filterRow.Controls.Count > 2)
            _filterRow.Controls.RemoveAt(2);

        foreach (var f in _filters)
        {
            var col = new ComboBox { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
            col.Items.AddRange(_schema.All.Cast<object>().ToArray());
            col.SelectedItem = f.Col;
            col.SelectedIndexChanged += (_, _) => { f.Col = col.SelectedItem?.ToString() ?? ""; ApplyFilters(); };

            var op = new ComboBox { Width = 56, DropDownStyle = ComboBoxStyle.DropDownList };
            op.Items.AddRange(["=", "!=", ">", "<", ">=", "<=", "contains"]);
            op.SelectedItem = f.Op;
            op.SelectedIndexChanged += (_, _) => { f.Op = op.SelectedItem?.ToString() ?? "="; ApplyFilters(); };

            var val = new TextBox { Width = 80, Text = f.Val };
            val.TextChanged += (_, _) => { f.Val = val.Text; ApplyFilters(); };

            var remove = new Button { Text = "×", Width = 24, FlatStyle = FlatStyle.Flat, ForeColor = Color.IndianRed, Tag = f };
            remove.Click += (_, _) => { _filters.Remove((DashboardFilterModel)remove.Tag!); RefreshFilters(); ApplyFilters(); };

            _filterRow.Controls.Add(col);
            _filterRow.Controls.Add(op);
            _filterRow.Controls.Add(val);
            _filterRow.Controls.Add(remove);
        }
        _filterRow.ResumeLayout();
    }

    private void ApplyFilters()
    {
        _canvas.SetFilters(_filters);
        ScheduleAutoSave();
    }

    private async Task GenerateFromPromptAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return;
        _saveStatus.Text = "Generating…";
        try
        {
            List<DashboardWidgetModel> widgets;
            if (!string.IsNullOrEmpty(_dataset?.SessionId) || !string.IsNullOrEmpty(_aiSessionId))
            {
                var sid = _aiSessionId;
                if (string.IsNullOrEmpty(sid) && _dataset != null)
                {
                    var revive = await _api.ReviveSessionAsync(_dataset.Id);
                    sid = revive.AiSessionId;
                    _aiSessionId = sid;
                }
                if (!string.IsNullOrEmpty(sid))
                {
                    var doc = await _api.AutoGenerateDashboardAsync(sid);
                    widgets = doc?.Widgets ?? DashboardAiGenerator.GenerateFromPrompt(prompt, _schema);
                }
                else
                    widgets = DashboardAiGenerator.GenerateFromPrompt(prompt, _schema);
            }
            else
                widgets = DashboardAiGenerator.GenerateFromPrompt(prompt, _schema);

            _canvas.LoadWidgets(widgets);
            _editMode = true;
            _canvas.SetEditMode(true);
            ScheduleAutoSave();
            _saveStatus.Text = "Generated";
            await Task.Delay(1200);
            _saveStatus.Text = "";
        }
        catch (Exception ex)
        {
            _saveStatus.Text = "Error";
            MessageBox.Show(ex.Message, "Generate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private string? _aiSessionId;

    private void OnWidgetConfigure(object? sender, WidgetCardControl card)
    {
        using var dlg = new WidgetConfigDialog(card.Widget, _schema);
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
        _canvas.UpdateWidget(dlg.Result);
        ScheduleAutoSave();
    }

    private void ScheduleAutoSave()
    {
        _autoSaveTimer.Stop();
        _autoSaveTimer.Start();
    }

    private async Task SaveAsync(bool silent)
    {
        if (!silent) _saveStatus.Text = "Saving…";
        var doc = new DashboardLayoutDocument
        {
            Title = _titleBox.Text,
            Scheme = _scheme.Name,
            Widgets = _canvas.Widgets.ToList()
        };

        try
        {
            await _api.UpdateDashboardAsync(_dashboardId, new DashboardUpdateRequest
            {
                Name = _titleBox.Text,
                Scheme = _scheme.Name,
                Layout = doc
            });
            _saveStatus.Text = silent ? "" : "Saved";
            if (!silent)
            {
                await Task.Delay(1500);
                _saveStatus.Text = "";
            }
        }
        catch (Exception ex)
        {
            _saveStatus.Text = "Error";
            if (!silent) MessageBox.Show(ex.Message, "Save", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
