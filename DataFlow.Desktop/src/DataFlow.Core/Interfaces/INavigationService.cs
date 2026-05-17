using DataFlow.Core.Enums;
using DataFlow.Core.Navigation;

namespace DataFlow.Core.Interfaces;

public interface INavigationService
{
    AppView CurrentView { get; }
    NavigationArgs? CurrentArgs { get; }
    event EventHandler<NavigationChangedEventArgs>? NavigationChanged;
    void Navigate(NavigationArgs args);
}
