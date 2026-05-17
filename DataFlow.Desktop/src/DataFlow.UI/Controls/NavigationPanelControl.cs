using DataFlow.Core.Enums;
using DataFlow.Core.Interfaces;
using DataFlow.Core.Models;
using DataFlow.Core.Navigation;
using DataFlow.Core.Themes;
using DataFlow.UI.Helpers;
using DataFlow.UI.Services;

namespace DataFlow.UI.Controls;

public sealed class NavigationPanelControl : Panel
{
    private readonly INavigationService _navigation;
    private readonly IDataFlowApiClient _api;
    private readonly AppShellService _shell;
    private readonly Button _btnWorkspace;
    private readonly Button _btnProjects;
    private readonly FlowLayoutPanel _projectList;
    private readonly Panel _projectsSection;
    private bool _collapsed;

    public bool Collapsed
    {
        get => _collapsed;
        set
        {
            _collapsed = value;
            Width = value ? 0 : DesignTokens.NavPanelWidth;
            Visible = !value;
        }
    }

    public NavigationPanelControl(INavigationService navigation, IDataFlowApiClient api, AppShellService shell)
    {
        _navigation = navigation;
        _api = api;
        _shell = shell;
        Width = DesignTokens.NavPanelWidth;
        BackColor = Color.White;
        Dock = DockStyle.Left;

        var header = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(14, 14, 14, 0) };
        header.Controls.Add(new Label
        {
            Text = "DataFlow AI",
            Location = new Point(0, 0),
            AutoSize = true,
            Font = new Font(DesignTokens.FontFamily, 11f, FontStyle.Bold),
            ForeColor = DesignTokens.Text
        });
        header.Controls.Add(new Label
        {
            Text = "ANALYTICS",
            Location = new Point(0, 20),
            AutoSize = true,
            Font = new Font(DesignTokens.FontFamily, 7.5f, FontStyle.Bold),
            ForeColor = DesignTokens.TextDim
        });

        var navBtns = new Panel { Dock = DockStyle.Top, Height = 88, Padding = new Padding(10, 8, 10, 0) };

        _btnWorkspace = CreateNavButton("AI Workspace", true);
        _btnWorkspace.Click += (_, _) =>
        {
            SetActiveSection(NavSection.AiWorkspace);
            _navigation.Navigate(NavigationArgs.For(AppView.Chat));
        };

        _btnProjects = CreateNavButton("Projects", false);
        _btnProjects.Click += (_, _) =>
        {
            SetActiveSection(NavSection.Projects);
            _navigation.Navigate(NavigationArgs.For(AppView.ProjectsHome));
        };

        navBtns.Controls.Add(_btnWorkspace);
        navBtns.Controls.Add(_btnProjects);

        _projectsSection = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0) };

        var projHdr = new Label
        {
            Text = "PROJECTS",
            Dock = DockStyle.Top,
            Height = 28,
            ForeColor = DesignTokens.TextDim,
            Font = new Font(DesignTokens.FontFamily, 8f, FontStyle.Bold),
            Padding = new Padding(14, 8, 0, 0)
        };

        _projectList = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(6, 0, 6, 4)
        };

        _projectsSection.Controls.Add(_projectList);
        _projectsSection.Controls.Add(projHdr);

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 96, Padding = new Padding(10, 0, 10, 10) };

        var newProject = new Button
        {
            Text = "+  New Project",
            Dock = DockStyle.Top,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            ForeColor = DesignTokens.Accent,
            BackColor = Color.White,
            Font = new Font(DesignTokens.FontFamily, 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        newProject.FlatAppearance.BorderColor = DesignTokens.AccentBorder;
        newProject.FlatAppearance.BorderSize = 1;
        newProject.Paint += (_, e) =>
        {
            if (newProject.ClientRectangle.Width <= 0) return;
            using var pen = new Pen(DesignTokens.AccentBorder, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            e.Graphics.DrawRectangle(pen, 0, 0, newProject.Width - 1, newProject.Height - 1);
        };
        newProject.Click += (_, _) => _navigation.Navigate(NavigationArgs.For(AppView.ProjectsHome));

        var settings = new Button
        {
            Text = "  ⚙  Settings",
            Dock = DockStyle.Bottom,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = DesignTokens.TextMuted,
            BackColor = Color.White,
            Font = new Font(DesignTokens.FontFamily, 9f),
            Cursor = Cursors.Hand
        };
        settings.FlatAppearance.BorderSize = 0;
        footer.Controls.Add(settings);
        footer.Controls.Add(newProject);

        Controls.Add(_projectsSection);
        Controls.Add(footer);
        Controls.Add(navBtns);
        Controls.Add(header);

        _shell.NavSectionChanged += (_, _) => ApplyNavSection(_shell.ActiveNavSection);
        HandleCreated += async (_, _) => await RefreshProjectsAsync();
    }

    private static Button CreateNavButton(string text, bool active)
    {
        var btn = new Button
        {
            Text = "  " + text,
            Width = DesignTokens.NavPanelWidth - 24,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(DesignTokens.FontFamily, 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 0, 6)
        };
        StyleNavButton(btn, active);
        return btn;
    }

    private static void StyleNavButton(Button btn, bool active)
    {
        btn.BackColor = active ? DesignTokens.Accent : Color.White;
        btn.ForeColor = active ? Color.White : DesignTokens.Text;
        btn.FlatAppearance.BorderSize = active ? 0 : 1;
        btn.FlatAppearance.BorderColor = DesignTokens.Border;
    }

    public void SetActiveSection(NavSection section)
    {
        _shell.ActiveNavSection = section;
        ApplyNavSection(section);
    }

    private void ApplyNavSection(NavSection section)
    {
        StyleNavButton(_btnWorkspace, section == NavSection.AiWorkspace);
        StyleNavButton(_btnProjects, section == NavSection.Projects);
    }

    public async Task RefreshProjectsAsync()
    {
        try
        {
            var projects = await _api.ListProjectsAsync();
            _projectList.Controls.Clear();

            foreach (var p in projects)
                _projectList.Controls.Add(new ProjectTreeRow(p, _navigation, _api, DesignTokens.NavPanelWidth - 20));
        }
        catch
        {
            _projectList.Controls.Clear();
            _projectList.Controls.Add(new Label
            {
                Text = "API offline",
                ForeColor = DesignTokens.TextMuted,
                Font = new Font(DesignTokens.FontFamily, 8.5f),
                Padding = new Padding(8),
                AutoSize = true
            });
        }
    }

    private sealed class ProjectTreeRow : Panel
    {
        private readonly ProjectDto _project;
        private readonly Panel _children;
        private bool _expanded;

        public ProjectTreeRow(ProjectDto project, INavigationService nav, IDataFlowApiClient api, int width)
        {
            _project = project;
            Width = width;
            AutoSize = true;
            Margin = new Padding(0, 0, 0, 2);

            var header = new Panel { Width = width, Height = 32, Cursor = Cursors.Hand };
            var dot = new Panel { Size = new Size(6, 6), Location = new Point(4, 13), BackColor = DesignTokens.ProjectDot };
            dot.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.FillEllipse(new SolidBrush(DesignTokens.ProjectDot), 0, 0, 5, 5);
            };
            var title = new Label
            {
                Text = project.Name,
                Location = new Point(16, 8),
                AutoSize = true,
                ForeColor = DesignTokens.Text,
                Font = new Font(DesignTokens.FontFamily, 9f)
            };
            header.Controls.Add(dot);
            header.Controls.Add(title);
            header.Click += async (_, _) => await ToggleAsync(nav, api);

            _children = new Panel { Width = width, Visible = false, AutoSize = true, Padding = new Padding(8, 0, 0, 4) };

            Controls.Add(_children);
            Controls.Add(header);
        }

        private async Task ToggleAsync(INavigationService nav, IDataFlowApiClient api)
        {
            _expanded = !_expanded;
            _children.Visible = _expanded;

            if (_expanded && _children.Controls.Count == 0)
            {
                try
                {
                    var summary = await api.GetProjectSummaryAsync(_project.Id);
                    AddSectionHeader("DASHBOARDS");
                    foreach (var d in summary.Dashboards.Take(5))
                    {
                        var dash = d;
                        AddLink($"📊 {d.Name}", () => nav.Navigate(NavigationArgs.For(AppView.DashboardBuilder, _project.Id, dash.Id)));
                    }
                    AddSectionHeader("CHATS");
                    foreach (var c in summary.Chats.Take(5))
                    {
                        var chat = c;
                        AddLink($"💬 {c.Title ?? "Chat"}", () => nav.Navigate(NavigationArgs.For(AppView.ProjectChat, _project.Id, chatSessionId: chat.Id)));
                    }
                }
                catch
                {
                    AddLink("Open project", () => nav.Navigate(NavigationArgs.For(AppView.ProjectDetail, _project.Id)));
                }
            }

            if (!_expanded)
                nav.Navigate(NavigationArgs.For(AppView.ProjectDetail, _project.Id));
        }

        private void AddSectionHeader(string text)
        {
            _children.Controls.Add(new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = DesignTokens.TextDim,
                Font = new Font(DesignTokens.FontFamily, 7.5f, FontStyle.Bold),
                Padding = new Padding(8, 6, 0, 2)
            });
        }

        private void AddLink(string text, Action navigate)
        {
            var btn = new Button
            {
                Text = "  " + text,
                Width = Width - 8,
                Height = 26,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = DesignTokens.TextMuted,
                BackColor = Color.Transparent,
                Font = new Font(DesignTokens.FontFamily, 8.5f),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (_, _) => navigate();
            _children.Controls.Add(btn);
        }
    }
}
