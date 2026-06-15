namespace LMU.Agent.Core.Models;

/// <summary>Aggregierte Kennzahlen für eine Renn-Kategorie (Sprint oder Endurance).</summary>
public class CategoryStats
{
    public int TotalRaces { get; set; }
    public int Wins { get; set; }          // P1 (in der Klasse)
    public int Podiums { get; set; }       // Top 3
    public int Top5 { get; set; }
    public int Top10 { get; set; }
    public int TopHalf { get; set; }       // Top 50 % der Klasse
    public int Dnf { get; set; }
    public int BestPosition { get; set; }
    public DateTime LastRaceDate { get; set; }
}

/// <summary>Beste Rundenzeit des Fahrers auf einer Strecke.</summary>
public class TrackBestLap
{
    public string Track { get; set; } = string.Empty;
    public double BestLapTime { get; set; }
}

/// <summary>Ein Fahrer und wie oft man mit ihm im selben Rennen war.</summary>
public class CompanionCount
{
    public string Name { get; set; } = string.Empty;
    public int RacesShared { get; set; }
}

/// <summary>
/// Auswertung für den Agent-Besitzer (Fahrer mit den meisten Ergebnissen unter
/// den menschlichen Fahrern). KI-Trainingsrennen sind ausgeschlossen. Wird an die
/// Website gepusht.
/// </summary>
public class UserDashboard
{
    public string DriverName { get; set; } = string.Empty;
    public CategoryStats Sprint { get; set; } = new();
    public CategoryStats Endurance { get; set; } = new();
    public List<TrackBestLap> BestLapsByTrack { get; set; } = new();
    public List<CompanionCount> MostRacedWith { get; set; } = new();

    /// <summary>Häufigste gegnerische <b>custom</b> Teams (Standard-Liverys herausgefiltert).</summary>
    public List<CompanionCount> MostRacedAgainstTeams { get; set; } = new();
}
