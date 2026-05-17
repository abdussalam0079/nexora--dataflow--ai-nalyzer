using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using DataFlow.Core.Enums;
using DataFlow.Core.Interfaces;
using DataFlow.Core.Navigation;
using DataFlow.UI.Wpf.Views;

namespace DataFlow.UI.Wpf;

public partial class MainWindow : Window
{
    private readonly INavigationService _navigation;
    private readonly IDataFlowApiClient _api;
    private readonly IAppStateService _state;
    private readonly Dictionary<AppView, UserControl> _staticViews;
    private bool _sidebarCollapsed;
    private AppView _currentView;

    private static readonly Brush ActiveBg = new SolidColorBrush(Color.FromRgb(30, 27, 75));
    private static readonly Brush ActiveFg = new SolidColorBrush(Color.FromRgb(129, 140, 248));
    private static readonly Brush DefaultFg = new SolidColorBrush(Color.FromRgb(148, 163, 184));

    public MainWindow(INavigationService navigation, IDataFlowApiClient api, IAppStateService state)
    {
        _navigation = navigation;
        _api = api;
        _state = state;
        InitializeComponent();

        DateTextBlock.Text = DateTime.Now.ToString("ddd, MMM d");

        // Static views (no per-project context needed)
        _staticViews = new Dictionary<AppView, UserControl>
        {
            [AppView.Chat]           = new ChatWorkspaceView(api, state),
            [AppView.ProjectsHome]   = new ProjectsHomeView(api, state, navigation),
            [AppView.DashboardBuilder] = new DashboardBuilderView(api, state),
            [AppView.Insights]       = new InsightsView(),
            [AppView.Realtime]       = new RealtimeView(),
            [AppView.EnterpriseAudit]= new EnterpriseAuditView()
        };

        _navigation.NavigationChanged += OnNavigationChanged;
        _ = BuildProjectListAsync();
        _navigation.Navigate(NavigationArgs.For(AppView.Chat));
    }

    private void OnNavigationChanged(object? sender, NavigationChangedEventArgs e)
        => Dispatcher.Invoke(() => ShowView(e.Args));

    private void ShowView(NavigationArgs args)
    {
        _currentView = args.View;
        UpdateActiveNav(args.View);
        UpdateConversationVisibility(args.View);

        switch (args.View)
        {
            case AppView.ProjectDetail when args.ProjectId.HasValue:
                var detailView = new ProjectDetailView(_api, _state, _navigation);
                ContentArea.Content = detailView;
                _state.SetProject(args.ProjectId.Value);
                _ = detailView.LoadAsync(args.ProjectId.Value);
                break;

            case AppView.ProjectChat when args.ProjectId.HasValue:
                var chatView = new ProjectChatView(_api, _state);
                ContentArea.Content = chatView;
                _state.SetProject(args.ProjectId.Value);
                _ = chatView.LoadAsync(args.ProjectId.Value, args.ChatSessionId);
                break;

            case AppView.DashboardBuilder when args.ProjectId.HasValue && args.DashboardId.HasValue:
                // Navigate to specific dashboard in builder
                var builderView = new DashboardBuilderView(_api, _state);
                ContentArea.Content = builderView;
                _state.ActiveProjectId = args.ProjectId.Value;
                _state.ActiveDashboardId = args.DashboardId.Value;
                _ = builderView.LoadDashboardAsync(args.ProjectId.Value, args.DashboardId.Value);
                break;

            default:
                if (_staticViews.TryGetValue(args.View, out var staticView))
                    ContentArea.Content = staticView;
                break;
        }
    }

    private void UpdateActiveNav(AppView view)
    {
        var navButtons = new[]
        {
            (WorkspaceButton, AppView.Chat),
            (ProjectsButton, AppView.ProjectsHome),
            (DashboardNavButton, AppView.DashboardBuilder),
            (InsightsNavButton, AppView.Insights),
            (RealtimeNavButton, AppView.Realtime),
            (AuditNavButton, AppView.EnterpriseAudit)
        };

        foreach (var (btn, btnView) in navButtons)
        {
            var isActive = btnView == view;
            btn.Background = isActive ? ActiveBg : Brushes.Transparent;
            btn.Foreground = isActive ? ActiveFg : DefaultFg;
        }
    }

    private void UpdateConversationVisibility(AppView view)
    {
        var visible = view is AppView.Chat or AppView.ProjectChat;
        ConversationsColumn.Width = visible ? new GridLength(300) : new GridLength(0);
    }

    private async Task BuildProjectListAsync()
    {
        try
        {
            var projects = await _api.ListProjectsAsync();
            Dispatcher.Invoke(() =>
            {
                ProjectListPanel.Children.Clear();
                foreach (var p in projects)
                    ProjectListPanel.Children.Add(CreateProjectItem(p));
            });
        }
        catch { /* ignore — sidebar is non-critical */ }
    }

    private Button CreateProjectItem(DataFlow.Core.Models.ProjectDto p)
    {
        var dotColor = Color.FromRgb(0x63, 0x66, 0xF1);
        try { dotColor = (Color)ColorConverter.ConvertFromString(p.Color ?? "#6366f1"); } catch { }

        var btn = new Button
        {
            Height = 40, Margin = new Thickness(0, 1, 0, 1),
            Background = Brushes.Transparent, BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(8, 0, 8, 0),
        };

        var template = new ControlTemplate(typeof(Button));
        var bdFactory = new FrameworkElementFactory(typeof(Border));
        bdFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        bdFactory.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background")
            { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
        var cpFactory = new FrameworkElementFactory(typeof(ContentPresenter));
        cpFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        cpFactory.SetValue(ContentPresenter.MarginProperty, new Thickness(8, 0, 0, 0));
        bdFactory.AppendChild(cpFactory);
        template.VisualTree = bdFactory;
        btn.Template = template;

        btn.Content = new StackPanel
        {
            Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new Ellipse
                {
                    Width = 8, Height = 8, Fill = new SolidColorBrush(dotColor),
                    Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center
                },
                new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = p.Name, FontSize = 12, FontFamily = new FontFamily("Segoe UI"),
                            FontWeight = FontWeights.Medium,
                            Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184))
                        },
                        new TextBlock
                        {
                            Text = $"{p.DashboardCount} dashboards", FontSize = 10,
                            FontFamily = new FontFamily("Segoe UI"),
                            Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105))
                        }
                    }
                }
            }
        };

        btn.MouseEnter += (_, _) => btn.Background = new SolidColorBrush(Color.FromRgb(26, 32, 53));
        btn.MouseLeave += (_, _) => btn.Background = Brushes.Transparent;
        btn.Click += (_, _) => _navigation.Navigate(NavigationArgs.For(AppView.ProjectDetail, p.Id));
        return btn;
    }

    // ── Window chrome ──────────────────────────────────────────────
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) ToggleMaximize();
        else DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void MaximizeButton_Click(object sender, RoutedEventArgs e) => ToggleMaximize();
    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    private void ToggleMaximize() => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    // ── Sidebar toggle ─────────────────────────────────────────────
    private void ToggleSidebarButton_Click(object sender, RoutedEventArgs e)
    {
        _sidebarCollapsed = !_sidebarCollapsed;
        NavColumn.Width = _sidebarCollapsed ? new GridLength(0) : new GridLength(220);
    }

    // ── Navigation ─────────────────────────────────────────────────
    private void WorkspaceButton_Click(object sender, RoutedEventArgs e)
        => _navigation.Navigate(NavigationArgs.For(AppView.Chat));

    private void ProjectsButton_Click(object sender, RoutedEventArgs e)
        => _navigation.Navigate(NavigationArgs.For(AppView.ProjectsHome));

    private void DashboardButton_Click(object sender, RoutedEventArgs e)
        => _navigation.Navigate(NavigationArgs.For(AppView.DashboardBuilder));

    private void InsightsButton_Click(object sender, RoutedEventArgs e)
        => _navigation.Navigate(NavigationArgs.For(AppView.Insights));

    private void RealtimeButton_Click(object sender, RoutedEventArgs e)
        => _navigation.Navigate(NavigationArgs.For(AppView.Realtime));

    private void AuditButton_Click(object sender, RoutedEventArgs e)
        => _navigation.Navigate(NavigationArgs.For(AppView.EnterpriseAudit));
}
