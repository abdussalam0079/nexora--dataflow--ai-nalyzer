using DataFlow.Core.Enums;
using DataFlow.Core.Interfaces;
using DataFlow.Core.Navigation;
using DataFlow.Core.Themes;
using DataFlow.UI.Controls;
using DataFlow.UI.Services;

namespace DataFlow.UI.Forms;

public sealed class MainForm : Form
{
    private readonly INavigationService _navigation;
    private readonly IAppStateService _state;
    private readonly IDataFlowApiClient _api;
    private readonly AppShellService _shell;
    private readonly NavigationPanelControl _navPanel;
    private readonly ConversationsSidebarControl _conversations;
    private readonly ContentHostPanel _contentHost;
    private readonly TopBarControl _topBar;
    private readonly ToastNotificationControl _toast;

    public MainForm(
        INavigationService navigation,
        IAppStateService state,
        IDataFlowApiClient api,
        AppShellService shell,
        IServiceProvider services)
    {
        _navigation = navigation;
        _state = state;
        _api = api;
        _shell = shell;

        Text = "DataFlow AI Analytics";
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1280, 720);
        BackColor = DesignTokens.PageBg;
        Font = new Font(DesignTokens.FontFamily, 9f);

        _navPanel = new NavigationPanelControl(_navigation, _api, _shell);
        _conversations = new ConversationsSidebarControl(_shell);

        var right = new Panel { Dock = DockStyle.Fill, BackColor = DesignTokens.PageBg };
        _topBar = new TopBarControl();
        _contentHost = new ContentHostPanel(_navigation, services);
        _contentHost.Initialize();

        _toast = new ToastNotificationControl();
        right.Controls.Add(_contentHost);
        right.Controls.Add(_topBar);
        right.Controls.Add(_toast);
        right.Resize += (_, _) => _toast.Location = new Point(right.Width - _toast.Width - 24, 64);

        var collapseBtn = new Button
        {
            Text = "«",
            Size = new Size(22, 22),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = DesignTokens.TextMuted,
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        collapseBtn.FlatAppearance.BorderColor = DesignTokens.Border;
        collapseBtn.Click += (_, _) =>
        {
            _state.SidebarCollapsed = !_state.SidebarCollapsed;
            _navPanel.Collapsed = _state.SidebarCollapsed;
            collapseBtn.Text = _state.SidebarCollapsed ? ">>" : "«";
            UpdateCollapsePosition(collapseBtn, _state.SidebarCollapsed);
        };

        var layout = new Panel { Dock = DockStyle.Fill, BackColor = DesignTokens.PageBg };
        layout.Controls.Add(right);
        layout.Controls.Add(_conversations);
        layout.Controls.Add(collapseBtn);
        layout.Controls.Add(_navPanel);
        layout.Resize += (_, _) => UpdateCollapsePosition(collapseBtn, _state.SidebarCollapsed);

        Controls.Add(layout);

        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.K)
            {
                e.Handled = true;
                _topBar.FocusSearch();
            }
        };

        _navigation.NavigationChanged += OnNavigationChanged;
        _shell.ConversationsPanelChanged += (_, _) => UpdateConversationsVisibility();
        Shown += async (_, _) =>
        {
            UpdateConversationsVisibility();
            await CheckApiHealthAsync();
            _ = _navPanel.RefreshProjectsAsync();
        };
    }

    private static void UpdateCollapsePosition(Button btn, bool collapsed)
    {
        btn.Location = collapsed
            ? new Point(10, 10)
            : new Point(DesignTokens.NavPanelWidth - 10, 10);
    }

    private void UpdateConversationsVisibility()
    {
        _conversations.Visible = _shell.ShowConversationsPanel;
        _conversations.Width = _shell.ShowConversationsPanel ? DesignTokens.ConversationsWidth : 0;
    }

    private void OnNavigationChanged(object? sender, NavigationChangedEventArgs e)
    {
        var view = e.Args.View;
        _shell.ShowConversationsPanel = view is AppView.Chat or AppView.ProjectChat;
        UpdateConversationsVisibility();

        if (view == AppView.Chat)
            _navPanel.SetActiveSection(NavSection.AiWorkspace);
        else if (view is AppView.ProjectsHome or AppView.ProjectDetail or AppView.DashboardBuilder)
            _navPanel.SetActiveSection(NavSection.Projects);
    }

    private async Task CheckApiHealthAsync()
    {
        if (!await _api.HealthCheckAsync())
            _toast.Show("API offline — run scripts\\start-api.ps1");
    }
}
