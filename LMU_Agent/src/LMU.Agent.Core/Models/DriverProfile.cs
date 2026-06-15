namespace LMU.Agent.Core.Models;

public class DriverProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public string CarModel { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public int TotalRaces { get; set; }
    public int Podiums { get; set; }
    public int Wins { get; set; }
    public double AveragePosition { get; set; }
    public DateTime CreatedDate { get; set; }
}