using System.ComponentModel.DataAnnotations;

namespace SimracingUtility.Models
{
    /// <summary>
    /// Vom LMU-Agent gepushte Statistik eines Fahrers (des Agent-Besitzers).
    /// Pro Fahrername existiert genau ein Datensatz (Upsert beim Push).
    /// </summary>
    public class LmuDriverStats
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string DriverName { get; set; } = string.Empty;

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

        /// <summary>Zeitpunkt des letzten Pushs.</summary>
        public DateTime UpdatedAt { get; set; }

        public List<LmuCompanion> Companions { get; set; } = new();
    }
}
