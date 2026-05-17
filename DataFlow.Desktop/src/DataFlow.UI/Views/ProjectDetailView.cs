using DataFlow.Core.Enums;
using DataFlow.Core.Interfaces;
using DataFlow.Core.Models;
using DataFlow.Core.Navigation;
using DataFlow.Core.Themes;
using DataFlow.UI.Controls;
using DataFlow.UI.Controls.Styled;
using DataFlow.UI.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace DataFlow.UI.Views;

public sealed class ProjectDetailView : UserControl, INavigationAware
{
    private readonly IDataFlowApiClient _api;
    private readonly INavigationService _navigation;
    private readonly Panel _header;
    private readonly Label _title;
    private readonly Label _datasetInfo;
    private readonly FlowLayoutPanel _dashboardList;
    private readonly Label _status;
    private int _projectId;
    private ProjectDto? _project;
    private DatasetDto? _dataset;
    private List<DashboardDto> _dashboards = [];

    public ProjectDetailView(IServiceProvider services)
    {
        _api = services.GetRequiredService<IDataFlowApiClient>();
        _navigation = services.GetRequiredService<INavigationService>();
        BackColor = Color.FromArgb(247, 247, 248);
        Dock = DockStyle.Fill;

        _header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 64,
            BackColor = Color.White,
            Padding = new Padding(24, 12, 24, 12)
        };

        var back = new Button
        {
            Text = "← Projects",
            FlatStyle = FlatStyle.Flat,
            Location = new Point(24, 16),
            AutoSize = true,
            Cursor = Cursors.Hand
        };
        back.FlatAppearance.BorderColor = DesignTokens.Border;
        back.Click += (_, _) => _navigation.Navigate(NavigationArgs.For(AppView.ProjectsHome));

        _title = new Label
        {
            Location = new Point(140, 12),
            AutoSize = true,
            Font = new Font(DesignTokens.FontFamily, 14f, FontStyle.Bold),
            ForeColor = DesignTokens.Text
        };

        var uploadBtn = new Button
        {
            Text = "Upload dataset",
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Size = new Size(120, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = DesignTokens.AccentBg,
            ForeColor = DesignTokens.Accent,
            Cursor = Cursors.Hand
        };
        uploadBtn.FlatAppearance.BorderColor = DesignTokens.AccentBorder;
        uploadBtn.Click += async (_, _) => await UploadDatasetAsync();

        var chatBtn = new Button
        {
            Text = "AI Chat",
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Size = new Size(88, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = DesignTokens.AccentBg,
            ForeColor = DesignTokens.Accent,
            Cursor = Cursors.Hand
        };
        chatBtn.FlatAppearance.BorderColor = DesignTokens.AccentBorder;
        chatBtn.Click += (_, _) => _navigation.Navigate(NavigationArgs.For(AppView.ProjectChat, _projectId));

        var insightsBtn = new Button
        {
            Text = "Insights",
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Size = new Size(88, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = DesignTokens.AccentBg,
            ForeColor = DesignTokens.Accent,
            Cursor = Cursors.Hand
        };
        insightsBtn.FlatAppearance.BorderColor = DesignTokens.AccentBorder;
        insightsBtn.Click += (_, _) => _navigation.Navigate(NavigationArgs.For(AppView.Insights, _projectId));

        var realtimeBtn = new Button
        {
            Text = "Realtime",
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Size = new Size(88, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = DesignTokens.AccentBg,
            ForeColor = DesignTokens.Accent,
            Cursor = Cursors.Hand
        };
        realtimeBtn.FlatAppearance.BorderColor = DesignTokens.AccentBorder;
        realtimeBtn.Click += (_, _) => _navigation.Navigate(NavigationArgs.For(AppView.Realtime, _projectId));

        var schemaBtn = new GhostButton { Text = "Schema", Size = new Size(72, 32), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        schemaBtn.Click += (_, _) => ShowSchema();

        var auditBtn = new GhostButton { Text = "API Audit", Size = new Size(80, 32), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        auditBtn.Click += (_, _) => _navigation.Navigate(NavigationArgs.For(AppView.EnterpriseAudit, _projectId));

        var newDashBtn = new AccentButton
        {
            Text = "+ New Dashboard",
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Size = new Size(140, 32)
        };
        newDashBtn.Click += async (_, _) => await CreateDashboardAsync();

        _header.Resize += (_, _) =>
        {
            uploadBtn.Location = new Point(_header.Width - 680, 16);
            schemaBtn.Location = new Point(_header.Width - 600, 16);
            chatBtn.Location = new Point(_header.Width - 520, 16);
            insightsBtn.Location = new Point(_header.Width - 426, 16);
            realtimeBtn.Location = new Point(_header.Width - 332, 16);
            auditBtn.Location = new Point(_header.Width - 240, 16);
            newDashBtn.Location = new Point(_header.Width - 150, 16);
        };

        _header.Controls.AddRange([back, _title, uploadBtn, schemaBtn, chatBtn, insightsBtn, realtimeBtn, auditBtn, newDashBtn]);

        _status = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            ForeColor = DesignTokens.TextMuted,
            Padding = new Padding(28, 0, 0, 0),
            Text = "Loading…"
        };

        _datasetInfo = new Label
        {
            Dock = DockStyle.Top,
            Height = 72,
            Padding = new Padding(28, 8, 28, 8),
            ForeColor = DesignTokens.TextMuted,
            Font = new Font(DesignTokens.FontFamily, 9.5f)
        };

        var sectionLbl = new Label
        {
            Text = "DASHBOARDS",
            Dock = DockStyle.Top,
            Height = 32,
            Padding = new Padding(28, 8, 0, 0),
            Font = new Font(DesignTokens.FontFamily, 8f, FontStyle.Bold),
            ForeColor = DesignTokens.TextDim
        };

        _dashboardList = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(24),
            WrapContents = false,
            FlowDirection = FlowDirection.TopDown
        };

        Controls.Add(_dashboardList);
        Controls.Add(sectionLbl);
        Controls.Add(_datasetInfo);
        Controls.Add(_status);
        Controls.Add(_header);
    }

    public void OnNavigatedTo(NavigationArgs args)
    {
        if (args.ProjectId is not int id) return;
        _projectId = id;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        _status.Text = "Loading project…";
        try
        {
            _project = await _api.GetProjectAsync(_projectId);
            _title.Text = $"{_project.Icon}  {_project.Name}";

            var datasets = await _api.ListDatasetsAsync(_projectId);
            _dataset = datasets.FirstOrDefault();
            _dashboards = (await _api.ListDashboardsAsync(_projectId)).ToList();

            if (_dataset != null)
            {
                _datasetInfo.Text = $"Dataset: {_dataset.FileName}  ·  {_dataset.RowCount:N0} rows  ·  {_dataset.ColCount} columns  ·  {FormatBytes(_dataset.SizeBytes)}";
                _datasetInfo.ForeColor = Color.FromArgb(16, 185, 129);
            }
            else
            {
                _datasetInfo.Text = "No dataset uploaded. Upload CSV, XLSX, JSON, TSV, or Parquet to build dashboards.";
                _datasetInfo.ForeColor = DesignTokens.TextMuted;
            }

            RenderDashboards();
            _status.Text = $"{_dashboards.Count} dashboard(s)";
        }
        catch (Exception ex)
        {
            _status.Text = "Failed to load project.";
            _datasetInfo.Text = ex.Message;
        }
    }

    private void RenderDashboards()
    {
        _dashboardList.Controls.Clear();
        if (_dashboards.Count == 0)
        {
            _dashboardList.Controls.Add(new Label
            {
                Text = _dataset == null ? "Upload a dataset first, then create a dashboard." : "No dashboards yet. Click \"+ New Dashboard\".",
                AutoSize = true,
                ForeColor = DesignTokens.TextMuted,
                Padding = new Padding(4)
            });
            return;
        }

        foreach (var d in _dashboards)
        {
            var row = new Panel
            {
                Width = Math.Max(500, _dashboardList.ClientSize.Width - 48),
                Height = 52,
                Margin = new Padding(0, 0, 0, 6),
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            row.Paint += (_, e) =>
            {
                using var pen = new Pen(DesignTokens.Border);
                e.Graphics.DrawRectangle(pen, 0, 0, row.Width - 1, row.Height - 1);
            };

            var scheme = ChartSchemes.Get(d.Scheme);
            var stripe = new Panel { Width = 4, Dock = DockStyle.Left, BackColor = scheme.Accent };
            var pinMark = d.IsPinned ? "📌 " : "";
            var name = new Label
            {
                Text = pinMark + d.Name,
                Location = new Point(16, 10),
                AutoSize = true,
                Font = new Font(DesignTokens.FontFamily, 11f, FontStyle.Bold),
                ForeColor = DesignTokens.Text
            };
            var meta = new Label
            {
                Text = $"{d.Scheme}  ·  Updated {d.UpdatedAt:g}",
                Location = new Point(16, 30),
                AutoSize = true,
                ForeColor = DesignTokens.TextMuted,
                Font = new Font(DesignTokens.FontFamily, 8.5f)
            };

            var dash = d;
            var pinBtn = new GhostButton { Text = d.IsPinned ? "Unpin" : "Pin", Size = new Size(48, 24), Location = new Point(row.Width - 200, 14) };
            pinBtn.Click += async (_, _) => await TogglePinAsync(dash);

            var delBtn = new Button { Text = "×", Size = new Size(28, 24), Location = new Point(row.Width - 140, 14), FlatStyle = FlatStyle.Flat, ForeColor = Color.IndianRed };
            delBtn.Click += async (_, _) => await DeleteDashboardAsync(dash);

            var open = new Label { Text = "Open →", Location = new Point(row.Width - 90, 18), AutoSize = true, ForeColor = DesignTokens.Accent, Font = new Font(DesignTokens.FontFamily, 9f, FontStyle.Bold) };

            void OpenDash(object? s, EventArgs e) => _navigation.Navigate(NavigationArgs.For(AppView.DashboardBuilder, _projectId, dash.Id));
            row.Click += OpenDash;
            stripe.Click += OpenDash;
            name.Click += OpenDash;
            meta.Click += OpenDash;
            open.Click += OpenDash;

            row.Controls.AddRange([stripe, name, meta, pinBtn, delBtn, open]);
            _dashboardList.Controls.Add(row);
        }
    }

    private async Task UploadDatasetAsync()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Data files|*.csv;*.tsv;*.xlsx;*.xls;*.json;*.parquet|All files|*.*"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        _status.Text = "Uploading…";
        try
        {
            _dataset = await _api.UploadDatasetAsync(_projectId, dlg.FileName);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Upload failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _status.Text = "Upload failed.";
        }
    }

    private async Task CreateDashboardAsync()
    {
        if (_dataset == null)
        {
            MessageBox.Show("Upload a dataset before creating a dashboard.", "DataFlow AI", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new NewDashboardDialog();
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK || string.IsNullOrWhiteSpace(dlg.DashboardName)) return;

        try
        {
            var created = await _api.CreateDashboardAsync(new DashboardCreateRequest
            {
                ProjectId = _projectId,
                DatasetId = _dataset.Id,
                Name = dlg.DashboardName,
                Description = dlg.Description,
                Scheme = dlg.Scheme,
                Layout = new DashboardLayoutDocument { Title = dlg.DashboardName, Scheme = dlg.Scheme, Widgets = [] }
            });
            _navigation.Navigate(NavigationArgs.For(AppView.DashboardBuilder, _projectId, created.Id));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Create dashboard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ShowSchema()
    {
        if (_dataset == null)
        {
            MessageBox.Show("Upload a dataset first.", "Schema", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var dlg = new SchemaDialog(_dataset);
        dlg.ShowDialog(FindForm());
    }

    private async Task TogglePinAsync(DashboardDto dash)
    {
        try
        {
            await _api.UpdateDashboardAsync(dash.Id, new DashboardUpdateRequest { IsPinned = !dash.IsPinned });
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Pin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task DeleteDashboardAsync(DashboardDto dash)
    {
        if (MessageBox.Show($"Delete dashboard \"{dash.Name}\"?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;
        try
        {
            await _api.DeleteDashboardAsync(dash.Id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static string FormatBytes(int bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1048576) return $"{bytes / 1024.0:0.#} KB";
        return $"{bytes / 1048576.0:0.#} MB";
    }
}
