using LMU.Agent.Core.Data;
using LMU.Agent.Core.Services;
using LMU.Agent.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LMU.Agent.Service;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // Hintergrund-Host: Erfassung + Push (Worker).
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddScoped<LMUAgentContext>();
        builder.Services.AddScoped<IEventParser, EventParser>();
        builder.Services.AddScoped<IRaceResultParser, RaceResultParser>();
        builder.Services.AddScoped<IDriverProfileParser, DriverProfileParser>();
        builder.Services.AddScoped<IStatisticsParser, StatisticsParser>();
        builder.Services.AddHttpClient<StatsPushClient>();
        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();
        host.Start();

        // Lokaler Telemetrie-Download-Server.
        var config = host.Services.GetRequiredService<IConfiguration>();
        var resultsPath = LmuPathResolver.ResolveResultsPath(config["Lmu:ResultsPath"]);
        var port = int.TryParse(config["Telemetry:Port"], out var p) ? p : 5601;

        var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
        var telemetry = new TelemetryServer(port, resultsPath, loggerFactory.CreateLogger("Telemetry"));
        telemetry.Start();

        // Tray-Oberfläche (blockiert bis "Beenden").
        Application.Run(new TrayAppContext(host, telemetry, resultsPath));
    }
}
