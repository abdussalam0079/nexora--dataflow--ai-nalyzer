using DataFlow.Core.Enums;
using DataFlow.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using DataFlow.Core.Models;
using DataFlow.Core.Navigation;
using DataFlow.Core.Themes;
using DataFlow.UI.Controls;
using DataFlow.UI.Dialogs;

namespace DataFlow.UI.Views;

public sealed class ProjectsHomeView : UserControl, INavigationAware
{
    private readonly IDataFlowApiClient _api;
    private readonly INavigationService _navigation;
    private readonly FlowLayoutPanel _grid;
    private readonly Label _status;

    public ProjectsHomeView(IServiceProvider services)
    {
        _api = services.GetRequiredService<IDataFlowApiClient>();
        _navigation = services.GetRequiredService<INavigationService>();
        BackColor = DesignTokens.ContentBg;
        Dock = DockStyle.Fill;

        var header = new Panel { Dock = DockStyle.Top, Height = 72, Padding = new Padding(28, 24, 28, 0) };
        header.Controls.Add(new Label
        {
            Text = "Projects",
            Font = new Font(DesignTokens.FontFamily, 20f, FontStyle.Bold),
            ForeColor = DesignTokens.Text,
            AutoSize = true
        });

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(28, 0, 28, 0) };
        var newBtn = new Button
        {
            Text = "  + New Project",
            Size = new Size(140, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = DesignTokens.Accent,
            ForeColor = Color.White,
            Font = new Font(DesignTokens.FontFamily, 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        newBtn.FlatAppearance.BorderSize = 0;
        newBtn.Click += async (_, _) => await CreateProjectAsync();
        toolbar.Controls.Add(newBtn);

        _status = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            ForeColor = DesignTokens.TextMuted,
            Font = new Font(DesignTokens.FontFamily, 9f),
            Padding = new Padding(28, 0, 0, 0),
            Text = "Loading…"
        };

        _grid = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(24),
            WrapContents = true
        };

        Controls.Add(_grid);
        Controls.Add(_status);
        Controls.Add(toolbar);
        Controls.Add(header);
    }

    public void OnNavigatedTo(NavigationArgs args) => _ = LoadProjectsAsync();

    private async Task LoadProjectsAsync()
    {
        _status.Text = "Loading projects…";
        _grid.Controls.Clear();
        try
        {
            var projects = await _api.ListProjectsAsync();
            _status.Text = $"{projects.Count} project(s)";
            foreach (var p in projects)
                _grid.Controls.Add(CreateCard(p));
        }
        catch (Exception ex)
        {
            _status.Text = "Could not reach API — ensure FastAPI is running on port 8000.";
            _grid.Controls.Add(new Label
            {
                Text = ex.Message,
                AutoSize = true,
                ForeColor = DesignTokens.TextMuted,
                MaximumSize = new Size(600, 0)
            });
        }
    }

    private Control CreateCard(ProjectDto project)
    {
        var card = new RoundedPanel
        {
            Size = new Size(260, 140),
            Margin = new Padding(8),
            Cursor = Cursors.Hand,
            Tag = project
        };
        card.Click += (_, _) => _navigation.Navigate(NavigationArgs.For(AppView.ProjectDetail, project.Id));

        var accent = ColorTranslator.FromHtml(project.Color);
        var stripe = new Panel
        {
            BackColor = accent,
            Dock = DockStyle.Top,
            Height = 4
        };
        var name = new Label
        {
            Text = $"{project.Icon}  {project.Name}",
            Font = new Font(DesignTokens.FontFamily, 12f, FontStyle.Bold),
            ForeColor = DesignTokens.Text,
            Location = new Point(16, 20),
            AutoSize = true
        };
        var meta = new Label
        {
            Text = $"{project.DashboardCount} dashboards · {project.DatasetCount} datasets",
            Font = new Font(DesignTokens.FontFamily, 8.5f),
            ForeColor = DesignTokens.TextMuted,
            Location = new Point(16, 52),
            AutoSize = true
        };
        var desc = new Label
        {
            Text = project.Description ?? "No description",
            Font = new Font(DesignTokens.FontFamily, 8.5f),
            ForeColor = DesignTokens.TextDim,
            Location = new Point(16, 76),
            Size = new Size(228, 48)
        };

        card.Controls.Add(desc);
        card.Controls.Add(meta);
        card.Controls.Add(name);
        card.Controls.Add(stripe);
        return card;
    }

    private async Task CreateProjectAsync()
    {
        var name = InputDialog.Prompt(FindForm(), "New Project", "Project name:", "My Analytics Project");
        if (string.IsNullOrWhiteSpace(name)) return;

        try
        {
            await _api.CreateProjectAsync(new ProjectCreateRequest { Name = name.Trim() });
            await LoadProjectsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Create Project", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
