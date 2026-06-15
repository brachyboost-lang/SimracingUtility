using System.Globalization;
using System.Xml.Linq;
using LMU.Agent.Core.Models;
using LMU.Agent.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace LMU.Agent.Core.Services;

/// <summary>
/// Liest die Rennergebnisse von Le Mans Ultimate. Diese liegen als einzelne
/// XML-Dateien im Ordner <c>UserData\Log\Results</c> der Spielinstallation
/// (rFactor-2-Engine-Format). Schema gegen echte LMU-Dateien verifiziert:
/// <code>
/// &lt;rFactorXML&gt;
///   &lt;RaceResults&gt;
///     &lt;DateTime&gt;1761201666&lt;/DateTime&gt;           &lt;!-- Unix-Timestamp --&gt;
///     &lt;TimeString&gt;2025/10/23 08:41:06&lt;/TimeString&gt; &lt;!-- lesbares Datum --&gt;
///     &lt;TrackCourse&gt;Autodromo Nazionale Monza&lt;/TrackCourse&gt;
///     &lt;Race&gt;
///       &lt;Driver&gt;
///         &lt;Name&gt;...&lt;/Name&gt;
///         &lt;CarClass&gt;GT3&lt;/CarClass&gt;
///         &lt;Position&gt;3&lt;/Position&gt;             &lt;!-- Gesamtposition --&gt;
///         &lt;ClassPosition&gt;1&lt;/ClassPosition&gt;   &lt;!-- Position in der Klasse --&gt;
///         &lt;Laps&gt;24&lt;/Laps&gt;
///         &lt;BestLapTime&gt;210.123&lt;/BestLapTime&gt;
///         &lt;FinishStatus&gt;None&lt;/FinishStatus&gt;  &lt;!-- "None" = beendet, "DNF" = Ausfall --&gt;
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
        XDocument doc;
        try
        {
            doc = XDocument.Load(path);
        }
        catch (System.Xml.XmlException)
        {
            // Manche Ergebnisdateien deklarieren UTF-8, enthalten aber abweichend
            // kodierte Zeichen (z. B. in Fahrernamen). Tolerant als Latin-1 lesen
            // (bildet jedes Byte auf ein Zeichen ab und wirft daher nie) und parsen.
            var text = File.ReadAllText(path, System.Text.Encoding.Latin1);
            doc = XDocument.Parse(text);
        }
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

        // LMU: lesbares Datum in <TimeString> (yyyy/MM/dd HH:mm:ss); <DateTime>
        // ist ein Unix-Timestamp. Strecke in <TrackCourse>/<TrackVenue>.
        var unix = raceResults.Descendants("DateTime").FirstOrDefault()?.Value?.Trim();
        var timeString = raceResults.Descendants("TimeString").FirstOrDefault()?.Value;
        var raceDate = ParseDate(timeString, unix);
        var trackName = (raceResults.Descendants("TrackCourse").FirstOrDefault()
                         ?? raceResults.Descendants("TrackVenue").FirstOrDefault())
                        ?.Value?.Trim() ?? string.Empty;

        // Session-Kennung: Unix-Zeitstempel der Session (eindeutig je Rennen).
        var sessionId = !string.IsNullOrWhiteSpace(unix) ? unix : (timeString?.Trim() ?? string.Empty);

        // Nur die Renn-Session auswerten (nicht Practice/Qualify).
        var race = raceResults.Descendants("Race").FirstOrDefault();
        if (race == null)
        {
            return results;
        }

        var drivers = race.Elements("Driver").ToList();

        // LMU ist multiclass: Feldgröße je Fahrzeugklasse bestimmen.
        var classCounts = drivers
            .GroupBy(d => d.Element("CarClass")?.Value?.Trim() ?? string.Empty)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var driver in drivers)
        {
            var carClass = driver.Element("CarClass")?.Value?.Trim() ?? string.Empty;

            results.Add(new RaceResult
            {
                DriverName = driver.Element("Name")?.Value?.Trim() ?? string.Empty,
                SessionId = sessionId,
                RaceDate = raceDate,
                TrackName = trackName,
                TeamName = driver.Element("TeamName")?.Value?.Trim() ?? string.Empty,
                CarEntry = driver.Element("VehFile")?.Value?.Trim() ?? string.Empty,
                Position = ParseInt(driver.Element("ClassPosition")?.Value),
                OverallPosition = ParseInt(driver.Element("Position")?.Value),
                FieldSize = classCounts.TryGetValue(carClass, out var n) ? n : drivers.Count,
                Laps = ParseLaps(driver),
                BestLapTime = ParseDouble(driver.Element("BestLapTime")?.Value),
                FinishStatus = driver.Element("FinishStatus")?.Value?.Trim() ?? string.Empty,
                CarNumber = driver.Element("CarNumber")?.Value?.Trim() ?? string.Empty,
                CarClass = carClass,
                IsPlayer = (driver.Element("isPlayer")?.Value?.Trim() ?? "0") == "1",
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

    private static DateTime ParseDate(string? timeString, string? unixSeconds)
    {
        // Primär: lesbarer Zeitstring "yyyy/MM/dd HH:mm:ss".
        if (DateTime.TryParseExact(timeString?.Trim(), "yyyy/MM/dd HH:mm:ss",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date))
        {
            return date;
        }
        // Toleranter Versuch für abweichende Formate.
        if (DateTime.TryParse(timeString, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal, out date))
        {
            return date;
        }
        // Fallback: Unix-Timestamp aus <DateTime>.
        if (long.TryParse(unixSeconds, out var seconds))
        {
            return DateTimeOffset.FromUnixTimeSeconds(seconds).LocalDateTime;
        }
        return DateTime.MinValue;
    }

    private static int ParseInt(string? value)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;

    private static double ParseDouble(string? value)
        => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0d;
}
