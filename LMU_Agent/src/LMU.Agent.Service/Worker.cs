using LMU.Agent.Core.Data;
using LMU.Agent.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LMU.Agent.Service;

/// <summary>
/// Hintergrunddienst, der beim Start die LMU-Datendateien einliest, in die
/// SQLite-Datenbank schreibt und die Fahrer-Statistiken berechnet.
/// </summary>
public class Worker : BackgroundService
{
    // Intervall zwischen zwei Erfassungsläufen. Da die Parser idempotent
    // schreiben (Upsert), ist wiederholtes Einlesen gefahrlos möglich.
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);

    private readonly IServiceProvider _services;
    private readonly ILogger<Worker> _logger;

    public Worker(IServiceProvider services, ILogger<Worker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Datenbank einmalig sicherstellen
        using (var scope = _services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<LMUAgentContext>();
            await context.Database.EnsureCreatedAsync(stoppingToken);
        }

        // Periodisch neu einlesen, bis der Dienst gestoppt wird.
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCaptureAsync(stoppingToken);

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

    private async Task RunCaptureAsync(CancellationToken stoppingToken)
    {
        // Pfad zu den LMU-Ultimate-Datenordnern konfigurieren
        var lmDataPath = Environment.GetEnvironmentVariable("LMU_DATA_PATH") ??
                         Path.Combine(
                             Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                             "AppData", "LocalLow", "SlightlyMad", "LeMansUltimate");

        var eventsPath = Path.Combine(lmDataPath, "Events.json");
        var resultsPath = Path.Combine(lmDataPath, "RaceResults.json");
        var profilesPath = Path.Combine(lmDataPath, "DriverProfiles.json");

        try
        {
            using var scope = _services.CreateScope();
            var eventParser = scope.ServiceProvider.GetRequiredService<IEventParser>();
            var resultParser = scope.ServiceProvider.GetRequiredService<IRaceResultParser>();
            var profileParser = scope.ServiceProvider.GetRequiredService<IDriverProfileParser>();
            var statisticsParser = scope.ServiceProvider.GetRequiredService<IStatisticsParser>();

            _logger.LogInformation("Lese Events von {Path}", eventsPath);
            var events = await eventParser.ParseEventsAsync(eventsPath);
            _logger.LogInformation("Geparsete Events: {Count}", events.Count);

            _logger.LogInformation("Lese Race Results von {Path}", resultsPath);
            var results = await resultParser.ParseRaceResultsAsync(resultsPath);
            _logger.LogInformation("Geparsete Race Results: {Count}", results.Count);

            _logger.LogInformation("Lese Driver Profiles von {Path}", profilesPath);
            var profiles = await profileParser.ParseProfilesAsync(profilesPath);
            _logger.LogInformation("Geparsete Driver Profiles: {Count}", profiles.Count);

            _logger.LogInformation("Berechne Statistiken...");
            await statisticsParser.CalculateAndStoreStatisticsAsync();

            _logger.LogInformation("LMU Agent: Datenerfassung abgeschlossen.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler bei der Datenerfassung des LMU Agent.");
        }
    }
}
