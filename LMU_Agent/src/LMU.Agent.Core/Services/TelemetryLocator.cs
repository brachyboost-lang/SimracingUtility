using System.Globalization;
using System.Text;

namespace LMU.Agent.Core.Services;

/// <summary>
/// Findet die MoTeC-Telemetriedateien (.ld/.ldx) von Le Mans Ultimate. Diese
/// liegen im Ordner <c>&lt;Spiel&gt;\LOG</c>; die Dateinamen enthalten Datum, Zeit
/// und Streckennamen, z. B. "2026-01-29 - 18-17-53 - Autódromo José Carlos Pace - P1.ld".
/// </summary>
public static class TelemetryLocator
{
    /// <summary>
    /// Leitet den Spiel-LOG-Ordner aus dem Ergebnis-Ordner ab:
    /// <c>…\UserData\Log\Results</c> → <c>…\LOG</c>.
    /// </summary>
    public static string? GameLogFolderFromResults(string resultsPath)
    {
        // Results -> Log -> UserData -> <Spielwurzel>
        var gameRoot = new DirectoryInfo(resultsPath).Parent?.Parent?.Parent;
        return gameRoot == null ? null : Path.Combine(gameRoot.FullName, "LOG");
    }

    /// <summary>Alle .ld/.ldx-Dateien, deren Name den Streckennamen enthält.</summary>
    public static List<string> FindTelemetryFiles(string logFolder, string track)
    {
        if (string.IsNullOrWhiteSpace(track) || !Directory.Exists(logFolder))
        {
            return new List<string>();
        }

        var needle = Normalize(track);
        if (needle.Length == 0) return new List<string>();

        return Directory.EnumerateFiles(logFolder)
            .Where(f =>
            {
                var ext = Path.GetExtension(f).ToLowerInvariant();
                return ext == ".ld" || ext == ".ldx";
            })
            .Where(f => Normalize(Path.GetFileNameWithoutExtension(f)).Contains(needle))
            .OrderBy(f => f)
            .ToList();
    }

    /// <summary>
    /// Reduziert auf ein reines ASCII-Skelett (nur a-z0-9, klein). Bewusst werden
    /// ALLE Nicht-ASCII-Zeichen verworfen – die LMU-Telemetrie-Dateinamen enthalten
    /// teils Mojibake ("AutÃ³dromo" statt "Autódromo"), während der Streckenname aus
    /// den Ergebnissen sauber ist. So reduzieren sich beide Seiten gleich
    /// ("autdromojoscarlospace") und matchen trotzdem.
    /// </summary>
    public static string Normalize(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            if (ch is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
                sb.Append(ch);
            else if (ch is >= 'A' and <= 'Z')
                sb.Append((char)(ch + 32)); // lower
        }
        return sb.ToString();
    }
}
