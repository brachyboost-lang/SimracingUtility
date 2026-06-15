using System.Text.RegularExpressions;

namespace LMU.Agent.Core.Services;

/// <summary>
/// Ermittelt den Ordner mit den Le-Mans-Ultimate-Ergebnisdateien. Da Steam je
/// nach Installation auf einem beliebigen Laufwerk/Library-Pfad liegen kann, wird
/// der Pfad konfigurierbar gehalten und – falls nicht gesetzt – automatisch über
/// die Steam-Bibliotheken erkannt.
/// </summary>
public static class LmuPathResolver
{
    public const string DefaultResultsPath =
        @"C:\Program Files (x86)\Steam\steamapps\common\Le Mans Ultimate\UserData\Log\Results";

    public const string EnvVariable = "LMU_RESULTS_PATH";

    // Relativer Pfad innerhalb einer Steam-Library bis zu den Ergebnissen.
    private const string ResultsRelative =
        @"steamapps\common\Le Mans Ultimate\UserData\Log\Results";

    /// <summary>
    /// Reihenfolge: explizit konfiguriert → Umgebungsvariable <c>LMU_RESULTS_PATH</c>
    /// → Auto-Erkennung über Steam-Bibliotheken → Standard-Steam-Pfad.
    /// </summary>
    public static string ResolveResultsPath(
        string? configured, string? envValue = null, Func<string?>? autoDetect = null)
    {
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.Trim();
        }

        envValue ??= Environment.GetEnvironmentVariable(EnvVariable);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return envValue.Trim();
        }

        var detected = (autoDetect ?? AutoDetectResultsPath)();
        if (!string.IsNullOrWhiteSpace(detected))
        {
            return detected!;
        }

        return DefaultResultsPath;
    }

    /// <summary>Sucht den Ergebnis-Ordner in allen gefundenen Steam-Bibliotheken.</summary>
    public static string? AutoDetectResultsPath()
    {
        foreach (var lib in SteamLibraryFolders())
        {
            var path = Path.Combine(lib, ResultsRelative);
            if (Directory.Exists(path))
            {
                return path;
            }
        }
        return null;
    }

    private static IEnumerable<string> SteamLibraryFolders()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in SteamRootCandidates())
        {
            if (!Directory.Exists(root) || !seen.Add(root)) continue;
            yield return root;

            // Weitere Bibliotheken aus libraryfolders.vdf.
            foreach (var lib in ParseLibraryFolders(root))
            {
                if (seen.Add(lib)) yield return lib;
            }
        }
    }

    private static IEnumerable<string> SteamRootCandidates()
    {
        var pf86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
        if (!string.IsNullOrEmpty(pf86)) yield return Path.Combine(pf86, "Steam");

        var pf = Environment.GetEnvironmentVariable("ProgramFiles");
        if (!string.IsNullOrEmpty(pf)) yield return Path.Combine(pf, "Steam");

        // Übliche Library-Orte auf allen Laufwerken (z. B. G:\Steam, D:\SteamLibrary).
        foreach (var drive in DriveInfo.GetDrives())
        {
            var r = drive.RootDirectory.FullName;
            yield return Path.Combine(r, "Steam");
            yield return Path.Combine(r, "SteamLibrary");
        }
    }

    private static IEnumerable<string> ParseLibraryFolders(string steamRoot)
    {
        foreach (var vdf in new[]
                 {
                     Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf"),
                     Path.Combine(steamRoot, "config", "libraryfolders.vdf"),
                 })
        {
            if (!File.Exists(vdf)) continue;

            string text;
            try { text = File.ReadAllText(vdf); }
            catch { continue; }

            // Zeilen wie:  "path"   "D:\\SteamLibrary"
            foreach (Match m in Regex.Matches(text, "\"path\"\\s+\"([^\"]+)\""))
            {
                yield return m.Groups[1].Value.Replace(@"\\", @"\");
            }
        }
    }
}
