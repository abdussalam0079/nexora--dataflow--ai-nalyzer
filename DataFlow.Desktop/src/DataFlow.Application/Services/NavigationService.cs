using DataFlow.Core.Enums;
using DataFlow.Core.Interfaces;
using DataFlow.Core.Navigation;

namespace DataFlow.Application.Services;

public sealed class NavigationService : INavigationService
{
    public AppView CurrentView { get; private set; } = AppView.Chat;
    public NavigationArgs? CurrentArgs { get; private set; }

    public event EventHandler<NavigationChangedEventArgs>? NavigationChanged;

    public void Navigate(NavigationArgs args)
    {
        CurrentView = args.View;
        CurrentArgs = args;
        NavigationChanged?.Invoke(this, new NavigationChangedEventArgs { Args = args });
    }
}
