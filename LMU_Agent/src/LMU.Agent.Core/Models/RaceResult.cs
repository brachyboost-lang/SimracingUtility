namespace LMU.Agent.Core.Models;

public class RaceResult
{
    public int Id { get; set; }
    public string DriverName { get; set; } = string.Empty;

    /// <summary>Eindeutige Kennung der Renn-Session (für die Gruppierung pro Rennen).</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>Zeitstempel der Session (aus dem Ergebnis-XML).</summary>
    public DateTime RaceDate { get; set; }

    public string TrackName { get; set; } = string.Empty;

    /// <summary>Team-/Einsatzname des Autos – Fahrer mit gleichem Wert im selben
    /// Rennen sind Teamkollegen (Fahrerwechsel in der Endurance).</summary>
    public string TeamName { get; set; } = string.Empty;

    /// <summary>Fahrzeugdatei (VEH) – Fallback-Kennung des Autos.</summary>
    public string CarEntry { get; set; } = string.Empty;

    /// <summary>Schlüssel zur Auto-/Team-Zuordnung: TeamName, sonst CarEntry.</summary>
    public string CarKey =>
        !string.IsNullOrWhiteSpace(TeamName) ? TeamName : CarEntry;

    /// <summary>Endposition in der eigenen Fahrzeugklasse (LMU ist multiclass).</summary>
    public int Position { get; set; }

    /// <summary>Gesamtposition über alle Klassen (informativ).</summary>
    public int OverallPosition { get; set; }

    /// <summary>Anzahl der Teilnehmer in derselben Klasse (für Top-50%-Auswertung).</summary>
    public int FieldSize { get; set; }

    public int Laps { get; set; }

    /// <summary>Beste Rundenzeit in Sekunden (0, falls keine gültige Runde).</summary>
    public double BestLapTime { get; set; }

    /// <summary>Roh-Status aus dem XML. In LMU: "None" = beendet, "DNF"/"DQ"/"DNS" = Ausfall.</summary>
    public string FinishStatus { get; set; } = string.Empty;

    public string CarNumber { get; set; } = string.Empty;
    public string CarClass { get; set; } = string.Empty;

    /// <summary>True = menschlicher Fahrer, False = KI-Bot (aus &lt;isPlayer&gt;).</summary>
    public bool IsPlayer { get; set; }

    /// <summary>
    /// True, wenn der Fahrer das Rennen nicht regulär beendet hat. LMU markiert
    /// beendete Rennen mit "None"; alles andere außer "Finished*" gilt als Ausfall.
    /// </summary>
    public bool IsDnf
    {
        get
        {
            var s = FinishStatus?.Trim() ?? string.Empty;
            if (s.Length == 0) return false;
            if (s.Equals("None", StringComparison.OrdinalIgnoreCase)) return false;
            if (s.Contains("Finished", StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }
    }
}
