namespace LMU.Agent.Core.Services;

/// <summary>
/// Ermittelt den Ordner mit den Le-Mans-Ultimate-Ergebnisdateien. Da Steam je
/// nach Installation auf einem beliebigen Laufwerk/Library-Pfad liegen kann, ist
/// der Pfad konfigurierbar; der Default deckt nur die Standard-Steam-Installation
/// ab und muss sonst vom Nutzer gesetzt werden.
/// </summary>
public static class LmuPathResolver
{
    public const string DefaultResultsPath =
        @"C:\Program Files (x86)\Steam\steamapps\common\Le Mans Ultimate\UserData\Log\Results";

    public const string EnvVariable = "LMU_RESULTS_PATH";

    /// <summary>
    /// Reihenfolge: explizit konfigurierter Wert → Umgebungsvariable
    /// <c>LMU_RESULTS_PATH</c> → Standard-Steam-Pfad.
    /// </summary>
    public static string ResolveResultsPath(string? configured, string? envValue = null)
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

        return DefaultResultsPath;
    }
}
