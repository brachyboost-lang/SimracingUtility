namespace LMU.Agent.Core.Models;

public class Statistics
{
    public int Id { get; set; }
    public string DriverName { get; set; } = string.Empty;

    public int TotalRaces { get; set; }
    public int Wins { get; set; }          // P1
    public int Podiums { get; set; }       // Top 3
    public int Top5 { get; set; }
    public int Top10 { get; set; }
    public int TopHalf { get; set; }       // Top 50 % des Feldes
    public int Dnf { get; set; }           // nicht regulär beendet

    public int BestPosition { get; set; }
    public double FastestLapTime { get; set; }
    public DateTime LastRaceDate { get; set; }
}
