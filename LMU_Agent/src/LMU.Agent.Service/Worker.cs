using LMU.Agent.Core.Data;
using LMU.Agent.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LMU.Agent.Service;

/// <summary>
/// Hintergrunddienst, der periodisch die LMU-Ergebnisdateien einliest, in die
/// SQLite-Datenbank schreibt und die Fahrer-Statistiken aktualisiert.
/// </summary>
public class Worker : BackgroundService
{
    // Da die Parser idempotent schreiben (Upsert), ist wiederholtes Einlesen
    // gefahrlos möglich.
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);

    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private readonly ILogger<Worker> _logger;

    public Worker(IServiceProvider services, IConfiguration config, ILogger<Worker> logger)
    {
        _services = services;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var scope = _services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<LMUAgentContext>();
            await context.Database.EnsureCreatedAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCaptureAsync();

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break; // normaler Shutdown
            }
        }
    }

    private async Task RunCaptureAsync()
    {
        var resultsPath = LmuPathResolver.ResolveResultsPath(_config["Lmu:ResultsPath"]);

        if (!Directory.Exists(resultsPath))
        {
            _logger.LogWarning(
                "LMU-Ergebnisordner nicht gefunden: {Path}. Bitte den Pfad in " +
                "appsettings.json (Lmu:ResultsPath) oder über die Umgebungsvariable " +
                "{EnvVar} setzen – er zeigt typischerweise auf " +
                "<Steam>\\steamapps\\common\\Le Mans Ultimate\\UserData\\Log\\Results.",
                resultsPath, LmuPathResolver.EnvVariable);
            return;
        }

        try
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LMUAgentContext>();
            var resultParser = scope.ServiceProvider.GetRequiredService<IRaceResultParser>();
            var statisticsParser = scope.ServiceProvider.GetRequiredService<IStatisticsParser>();
            var pushClient = scope.ServiceProvider.GetRequiredService<StatsPushClient>();

            _logger.LogInformation("Lese Rennergebnisse aus {Path}", resultsPath);
            var results = await resultParser.ParseRaceResultsAsync(resultsPath);
            _logger.LogInformation("Geparste Ergebnis-Datensätze: {Count}", results.Count);

            _logger.LogInformation("Berechne Statistiken...");
            await statisticsParser.CalculateAndStoreStatisticsAsync();

            // Nutzer-Dashboard bauen und an die Website pushen.
            var all = await context.RaceResults.ToListAsync();
            var dashboard = DashboardBuilder.Build(all);
            await pushClient.PushAsync(dashboard, CancellationToken.None);

            _logger.LogInformation("LMU Agent: Datenerfassung abgeschlossen.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler bei der Datenerfassung des LMU Agent.");
        }
    }
}
