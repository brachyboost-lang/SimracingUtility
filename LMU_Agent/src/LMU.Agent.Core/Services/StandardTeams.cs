namespace LMU.Agent.Core.Services;

/// <summary>
/// Kuratierte Liste der offiziellen WEC-/ELMS-Teamnamen, die Le Mans Ultimate als
/// Standard-Liverys mitliefert. Dient dazu, Stock-Liverys auch dann zu erkennen,
/// wenn der Name kein Jahr/keine Startnummer enthält (z. B. „United Autosports #22").
///
/// Hinweis zur Aktualität: Saison-Varianten mit Jahr/Startnummer
/// („… 2025 #87") werden bereits dynamisch über das Muster im
/// <see cref="DashboardBuilder"/> erkannt – diese Liste muss daher nur die
/// Basis-Teamnamen enthalten und selten gepflegt werden.
/// </summary>
public static class StandardTeams
{
    // Basis-Teamnamen (Variante/Jahr/Nummer egal). Bewusst möglichst eindeutig
    // gehalten, um custom Teams nicht versehentlich zu treffen.
    private static readonly string[] OfficialNames =
    {
        // Hypercar
        "Toyota Gazoo Racing", "Ferrari AF Corse", "Vista AF Corse", "AF Corse",
        "Richard Mille AF Corse", "Porsche Penske Motorsport", "Cadillac Racing",
        "Hertz Team JOTA", "BMW M Team WRT", "Peugeot TotalEnergies",
        "Alpine Endurance Team", "Lamborghini Iron Lynx", "Isotta Fraschini",
        "Proton Competition", "Aston Martin THOR Team", "BMW M Hybrid",
        // LMGT3 / GT3
        "Akkodis ASP Team", "Manthey PureRxcing", "Manthey EMA", "Iron Lynx",
        "Iron Dames", "United Autosports", "Inception Racing", "TF Sport",
        "Heart of Racing Team", "D'station Racing", "Racing Spirit of Léman",
        "JMW Motorsport", "Spirit of Race", "The Bend Team WRT", "Team WRT",
        "Vector Sport", "Inter Europol Competition", "Nielsen Racing",
        "Algarve Pro Racing", "IDEC Sport", "Panis Racing", "DKR Engineering",
        "COOL Racing", "Pure Rxcing",
    };

    private static readonly HashSet<string> NormalizedNames =
        OfficialNames.Select(Normalize).ToHashSet();

    /// <summary>
    /// True, wenn der Teamname die Wortfolge eines offiziellen Teamnamens enthält.
    /// Es wird auf Wortgrenzen abgeglichen (Tokens), damit z. B. „Team WRTesting"
    /// nicht fälschlich als „Team WRT" zählt.
    /// </summary>
    public static bool IsOfficial(string? teamName)
    {
        if (string.IsNullOrWhiteSpace(teamName)) return false;
        var norm = " " + Normalize(teamName) + " ";
        foreach (var official in NormalizedNames)
        {
            if (official.Length > 0 && norm.Contains(" " + official + " "))
                return true;
        }
        return false;
    }

    /// <summary>Klein, nur Buchstaben/Ziffern je Token, durch einzelne Leerzeichen getrennt.</summary>
    private static string Normalize(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        var lastWasSpace = true; // führende Leerzeichen vermeiden
        foreach (var ch in s)
        {
            if (ch is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
            { sb.Append(ch); lastWasSpace = false; }
            else if (ch is >= 'A' and <= 'Z')
            { sb.Append((char)(ch + 32)); lastWasSpace = false; }
            else if (!lastWasSpace)
            { sb.Append(' '); lastWasSpace = true; }
        }
        return sb.ToString().Trim();
    }
}
