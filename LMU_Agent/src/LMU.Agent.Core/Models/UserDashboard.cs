namespace LMU.Agent.Core.Models;

/// <summary>Ein Mitfahrer/Gegner und wie oft man mit/gegen ihn gefahren ist.</summary>
public class CompanionCount
{
    public string Name { get; set; } = string.Empty;
    public int RacesShared { get; set; }
}

/// <summary>
/// Auswertung für genau einen Fahrer (den Besitzer des Agents = Fahrer mit den
/// meisten Ergebnissen): eigene Statistik plus die häufigsten Teamkollegen und
/// Gegner. Dieses Objekt wird an die Website gepusht.
/// </summary>
public class UserDashboard
{
    public string DriverName { get; set; } = string.Empty;
    public Statistics Stats { get; set; } = new();
    public List<CompanionCount> Teammates { get; set; } = new();
    public List<CompanionCount> Opponents { get; set; } = new();
}
