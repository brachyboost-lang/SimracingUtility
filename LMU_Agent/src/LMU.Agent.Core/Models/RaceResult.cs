namespace LMU.Agent.Core.Models;

public class RaceResult
{
    public int Id { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public double Time { get; set; }
    public DateTime RaceDate { get; set; }
    public int Position { get; set; }
    public int TotalLaps { get; set; }
    public List<LapTime> LapTimes { get; set; } = new();
    public string CarNumber { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
}

public class LapTime
{
    public int LapNumber { get; set; }
    public double Time { get; set; }
    public bool IsFastest { get; set; }
}