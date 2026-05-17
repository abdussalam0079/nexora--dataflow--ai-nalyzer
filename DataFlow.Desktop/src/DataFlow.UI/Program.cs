using DataFlow.Analytics;
using DataFlow.Application;
using DataFlow.Core.Interfaces;
using DataFlow.Infrastructure;
using DataFlow.Infrastructure.Api;
using DataFlow.UI.Forms;
using DataFlow.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DataFlow.UI;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        System.Windows.Forms.Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        var contentRoot = AppContext.BaseDirectory;

        var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.SetBasePath(contentRoot);
                cfg.AddJsonFile(Path.Combine(contentRoot, "appsettings.json"), optional: true, reloadOnChange: true);
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddApplication();
                services.AddAnalytics();
                services.AddSingleton<AppShellService>();
                services.AddSingleton<IAppShellService>(sp => sp.GetRequiredService<AppShellService>());
                services.AddInfrastructure(o =>
                {
                    ctx.Configuration.GetSection(DataFlowApiOptions.SectionName).Bind(o);
                });
                services.AddSingleton<MainForm>();
            })
            .Build();

        var main = host.Services.GetRequiredService<MainForm>();
        System.Windows.Forms.Application.Run(main);
    }
}
