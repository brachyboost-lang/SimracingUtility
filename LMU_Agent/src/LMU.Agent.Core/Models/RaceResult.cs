namespace LMU.Agent.Core.Models;

public class RaceResult
{
    public int Id { get; set; }
    public string DriverName { get; set; } = string.Empty;

    /// <summary>Zeitstempel der Session (aus dem Ergebnis-XML).</summary>
    public DateTime RaceDate { get; set; }

    public string TrackName { get; set; } = string.Empty;

    /// <summary>Endposition des Fahrers im Rennen.</summary>
    public int Position { get; set; }

    /// <summary>Anzahl der Teilnehmer in diesem Rennen (für Top-50%-Auswertung).</summary>
    public int FieldSize { get; set; }

    public int Laps { get; set; }

    /// <summary>Beste Rundenzeit in Sekunden (0, falls keine gültige Runde).</summary>
    public double BestLapTime { get; set; }

    /// <summary>Roh-Status aus dem XML, z. B. "Finished Normally", "DNF", "DQ".</summary>
    public string FinishStatus { get; set; } = string.Empty;

    public string CarNumber { get; set; } = string.Empty;
    public string CarClass { get; set; } = string.Empty;

    /// <summary>True, wenn der Fahrer das Rennen nicht regulär beendet hat.</summary>
    public bool IsDnf =>
        !FinishStatus.Contains("Finished", StringComparison.OrdinalIgnoreCase);
}
