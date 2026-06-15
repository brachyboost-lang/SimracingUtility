namespace SimracingUtility.Models
{
    // Payload, das der LMU-Agent an /api/lmu/stats sendet. Eigene DTOs, damit die
    // Website nicht vom Agent-Projekt abhängt (Feldnamen-Abgleich ist case-insensitiv).

    public class LmuStatsPushDto
    {
        public string DriverName { get; set; } = string.Empty;
        public LmuStatsDto Stats { get; set; } = new();
        public List<LmuCompanionDto> Teammates { get; set; } = new();
        public List<LmuCompanionDto> Opponents { get; set; } = new();
    }

    public class LmuStatsDto
    {
        public int TotalRaces { get; set; }
        public int Wins { get; set; }
        public int Podiums { get; set; }
        public int Top5 { get; set; }
        public int Top10 { get; set; }
        public int TopHalf { get; set; }
        public int Dnf { get; set; }
        public int BestPosition { get; set; }
        public double FastestLapTime { get; set; }
        public DateTime LastRaceDate { get; set; }
    }

    public class LmuCompanionDto
    {
        public string Name { get; set; } = string.Empty;
        public int RacesShared { get; set; }
    }
}
