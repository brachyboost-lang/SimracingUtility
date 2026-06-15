namespace SimracingUtility.Models
{
    // Payload, das der LMU-Agent an /api/lmu/stats sendet. Eigene DTOs, damit die
    // Website nicht vom Agent-Projekt abhängt (Feldnamen-Abgleich ist case-insensitiv).

    public class LmuStatsPushDto
    {
        public string DriverName { get; set; } = string.Empty;
        public CategoryStatsDto Sprint { get; set; } = new();
        public CategoryStatsDto Endurance { get; set; } = new();
        public List<TrackBestDto> BestLapsByTrack { get; set; } = new();
        public List<RacedWithDto> MostRacedWith { get; set; } = new();
        public List<RacedWithDto> MostRacedAgainstTeams { get; set; } = new();
    }

    public class CategoryStatsDto
    {
        public int TotalRaces { get; set; }
        public int Wins { get; set; }
        public int Podiums { get; set; }
        public int Top5 { get; set; }
        public int Top10 { get; set; }
        public int TopHalf { get; set; }
        public int Dnf { get; set; }
        public int BestPosition { get; set; }
        public DateTime LastRaceDate { get; set; }
    }

    public class TrackBestDto
    {
        public string Track { get; set; } = string.Empty;
        public double BestLapTime { get; set; }
    }

    public class RacedWithDto
    {
        public string Name { get; set; } = string.Empty;
        public int RacesShared { get; set; }
    }
}
