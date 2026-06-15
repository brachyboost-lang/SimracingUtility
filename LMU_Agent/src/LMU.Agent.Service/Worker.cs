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
    private readonly IServiceProvider _services;
    private readonly ILogger<Worker> _logger;

    public Worker(IServiceProvider services, ILogger<Worker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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
            var context = scope.ServiceProvider.GetRequiredService<LMUAgentContext>();
            var eventParser = scope.ServiceProvider.GetRequiredService<IEventParser>();
            var resultParser = scope.ServiceProvider.GetRequiredService<IRaceResultParser>();
            var profileParser = scope.ServiceProvider.GetRequiredService<IDriverProfileParser>();
            var statisticsParser = scope.ServiceProvider.GetRequiredService<IStatisticsParser>();

            // Datenbank sicherstellen
            await context.Database.EnsureCreatedAsync(stoppingToken);

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

            _logger.LogInformation("LMU Agent: Initiale Datenerfassung abgeschlossen.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler bei der Datenerfassung des LMU Agent.");
        }

        // Dienst am Leben halten, bis er gestoppt wird.
        // TODO: Periodisches Neu-Einlesen mit idempotentem Upsert (aktuell würden
        //       die Parser bei jedem Lauf Duplikate anlegen).
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // normaler Shutdown
        }
    }
}
