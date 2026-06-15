namespace LMU.Agent.Core.Models;

public class Statistics
{
    public int Id { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public double AverageTime { get; set; }
    public int BestPosition { get; set; }
    public int TotalRaces { get; set; }
    public int Podiums { get; set; }
    public int Wins { get; set; }
    public double FastestLapTime { get; set; }
    public DateTime LastRaceDate { get; set; }
}