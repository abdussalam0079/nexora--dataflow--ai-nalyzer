using DataFlow.Core.Interfaces;
using DataFlow.Infrastructure.Api;
using Microsoft.Extensions.DependencyInjection;

namespace DataFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, Action<DataFlowApiOptions>? configure = null)
    {
        var options = new DataFlowApiOptions();
        configure?.Invoke(options);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));

        services.AddHttpClient<IDataFlowApiClient, DataFlowApiClient>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        return services;
    }
}
