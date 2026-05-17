using DataFlow.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DataFlow.Analytics;

public static class DependencyInjection
{
    public static IServiceCollection AddAnalytics(this IServiceCollection services)
    {
        services.AddSingleton<ILocalAnalyticsService, LocalAnalyticsService>();
        return services;
    }
}
