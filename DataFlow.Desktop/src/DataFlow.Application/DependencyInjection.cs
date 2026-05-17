using DataFlow.Application.Services;
using DataFlow.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DataFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IAppStateService, AppStateService>();
        return services;
    }
}
