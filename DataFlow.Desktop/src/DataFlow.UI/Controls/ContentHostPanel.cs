using DataFlow.Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using DataFlow.Core.Interfaces;
using DataFlow.Core.Navigation;
using DataFlow.UI.Views;

namespace DataFlow.UI.Controls;

public sealed class ContentHostPanel : Panel
{
    private readonly INavigationService _navigation;
    private readonly Dictionary<AppView, Control> _views = new();
    private Control? _current;

    public ContentHostPanel(INavigationService navigation, IServiceProvider services)
    {
        _navigation = navigation;
        Dock = DockStyle.Fill;
        BackColor = Color.White;

        Register(AppView.Chat, new ChatWorkspaceView(services));
        Register(AppView.ProjectsHome, new ProjectsHomeView(services));
        Register(AppView.ProjectDetail, new ProjectDetailView(services));
        Register(AppView.ProjectChat, new ProjectChatView(services));
        Register(AppView.DashboardBuilder, new DashboardBuilderView(services));
        Register(AppView.Insights, new InsightsView(services));
        Register(AppView.Realtime, new RealtimeView(services));
        Register(AppView.EnterpriseAudit, new EnterpriseAuditView(services));

        _navigation.NavigationChanged += OnNavigationChanged;
    }

    private void Register(AppView view, Control control)
    {
        control.Dock = DockStyle.Fill;
        control.Visible = false;
        _views[view] = control;
        Controls.Add(control);
    }

    public void Initialize() => ShowView(_navigation.CurrentArgs ?? NavigationArgs.For(AppView.Chat));

    private void OnNavigationChanged(object? sender, NavigationChangedEventArgs e) => ShowView(e.Args);

    private void ShowView(NavigationArgs args)
    {
        if (!_views.TryGetValue(args.View, out var next))
            return;

        if (_current == next)
        {
            if (next is INavigationAware aware)
                aware.OnNavigatedTo(args);
            return;
        }

        _current?.Hide();
        _current = next;
        next.Show();
        next.BringToFront();

        if (next is INavigationAware aware2)
            aware2.OnNavigatedTo(args);
    }
}

public interface INavigationAware
{
    void OnNavigatedTo(NavigationArgs args);
}
