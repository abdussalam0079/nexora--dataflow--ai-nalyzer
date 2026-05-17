using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DataFlow.Application;
using DataFlow.Infrastructure;
using DataFlow.Infrastructure.Api;

namespace DataFlow.UI.Wpf;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var contentRoot = AppContext.BaseDirectory;

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.SetBasePath(contentRoot);
                cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddApplication();
                services.AddSingleton<MainWindow>();
                services.AddInfrastructure(o =>
                {
                    ctx.Configuration.GetSection(DataFlowApiOptions.SectionName).Bind(o);
                });
            })
            .Build();

        await _host.StartAsync();

        var main = _host.Services.GetRequiredService<MainWindow>();
        main.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}

