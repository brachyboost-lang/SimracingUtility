using System.Globalization;
using System.Xml.Linq;
using LMU.Agent.Core.Models;
using LMU.Agent.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace LMU.Agent.Core.Services;

/// <summary>
/// Liest die Rennergebnisse von Le Mans Ultimate. Diese liegen als einzelne
/// XML-Dateien im Ordner <c>UserData\Log\Results</c> der Spielinstallation
/// (rFactor-2-Engine-Format). Das angenommene Schema:
/// <code>
/// &lt;rFactorXML&gt;
///   &lt;RaceResults&gt;
///     &lt;DateTime&gt;2025-06-01 22:55:03&lt;/DateTime&gt;
///     &lt;TrackEvent&gt;&lt;TrackName&gt;Le Mans&lt;/TrackName&gt;&lt;/TrackEvent&gt;
///     &lt;Race&gt;
///       &lt;Driver&gt;
///         &lt;Name&gt;...&lt;/Name&gt;
///         &lt;Position&gt;1&lt;/Position&gt;
///         &lt;CarNumber&gt;7&lt;/CarNumber&gt;
///         &lt;CarClass&gt;Hypercar&lt;/CarClass&gt;
///         &lt;Laps&gt;24&lt;/Laps&gt;
///         &lt;BestLapTime&gt;210.123&lt;/BestLapTime&gt;
///         &lt;FinishStatus&gt;Finished Normally&lt;/FinishStatus&gt;
///       &lt;/Driver&gt;
///     &lt;/Race&gt;
///   &lt;/RaceResults&gt;
/// &lt;/rFactorXML&gt;
/// </code>
/// </summary>
public class RaceResultParser : IRaceResultParser
{
    private readonly LMUAgentContext _context;

    public RaceResultParser(LMUAgentContext context)
    {
        _context = context;
    }

    public async Task<List<RaceResult>> ParseRaceResultsAsync(string resultsFolder)
    {
        var parsed = new List<RaceResult>();

        if (!Directory.Exists(resultsFolder))
        {
            Console.WriteLine($"Ergebnis-Ordner nicht gefunden: {resultsFolder}");
            return parsed;
        }

        foreach (var file in Directory.EnumerateFiles(resultsFolder, "*.xml"))
        {
            try
            {
                parsed.AddRange(ParseResultsFile(file));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Parsen von {file}: {ex.Message}");
            }
        }

        if (parsed.Count == 0)
        {
            return parsed;
        }

        // Idempotentes Schreiben: ein historisches Ergebnis wird nur eingefügt,
        // wenn es noch nicht existiert (Fahrer + Renndatum + Position).
        foreach (var result in parsed)
        {
            var exists = await _context.RaceResults.AnyAsync(r =>
                r.DriverName == result.DriverName &&
                r.RaceDate == result.RaceDate &&
                r.Position == result.Position);

            if (!exists)
            {
                _context.RaceResults.Add(result);
            }
        }
        await _context.SaveChangesAsync();

        return parsed;
    }

    public async Task<List<RaceResult>> GetLastRaceResultsAsync()
    {
        return await _context.RaceResults
            .OrderByDescending(r => r.RaceDate)
            .Take(10)
            .ToListAsync();
    }

    public async Task<RaceResult?> GetRaceResultByIdAsync(int id)
    {
        return await _context.RaceResults.FindAsync(id);
    }

    // --- XML-Verarbeitung ------------------------------------------------

    public static IEnumerable<RaceResult> ParseResultsFile(string path)
    {
        var doc = XDocument.Load(path);
        return ParseResults(doc);
    }

    /// <summary>Parst ein bereits geladenes Ergebnis-XML (auch für Tests nutzbar).</summary>
    public static List<RaceResult> ParseResults(XDocument doc)
    {
        var results = new List<RaceResult>();

        var raceResults = doc.Descendants("RaceResults").FirstOrDefault();
        if (raceResults == null)
        {
            return results;
        }

        var raceDate = ParseDate(raceResults.Element("DateTime")?.Value);
        var trackName = raceResults.Descendants("TrackName").FirstOrDefault()?.Value?.Trim()
                        ?? string.Empty;

        // Nur die Renn-Session auswerten (nicht Practice/Qualify).
        var race = raceResults.Element("Race");
        if (race == null)
        {
            return results;
        }

        var drivers = race.Elements("Driver").ToList();
        var fieldSize = drivers.Count;

        foreach (var driver in drivers)
        {
            results.Add(new RaceResult
            {
                DriverName = driver.Element("Name")?.Value?.Trim() ?? string.Empty,
                RaceDate = raceDate,
                TrackName = trackName,
                Position = ParseInt(driver.Element("Position")?.Value),
                FieldSize = fieldSize,
                Laps = ParseLaps(driver),
                BestLapTime = ParseDouble(driver.Element("BestLapTime")?.Value),
                FinishStatus = driver.Element("FinishStatus")?.Value?.Trim() ?? string.Empty,
                CarNumber = driver.Element("CarNumber")?.Value?.Trim() ?? string.Empty,
                CarClass = driver.Element("CarClass")?.Value?.Trim() ?? string.Empty,
            });
        }

        return results;
    }

    private static int ParseLaps(XElement driver)
    {
        var lapsValue = driver.Element("Laps")?.Value;
        if (int.TryParse(lapsValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var laps))
        {
            return laps;
        }
        // Fallback: Anzahl der einzelnen <Lap>-Elemente.
        return driver.Elements("Lap").Count();
    }

    private static DateTime ParseDate(string? value)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal, out var date))
        {
            return date;
        }
        return DateTime.MinValue;
    }

    private static int ParseInt(string? value)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;

    private static double ParseDouble(string? value)
        => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0d;
}
