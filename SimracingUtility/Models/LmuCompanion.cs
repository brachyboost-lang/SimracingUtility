using System.ComponentModel.DataAnnotations;

namespace SimracingUtility.Models
{
    public enum CompanionKind
    {
        Teammate,
        Opponent
    }

    /// <summary>Ein Mitfahrer (Teamkollege) oder Gegner eines Fahrers mit Anzahl
    /// gemeinsamer Rennen.</summary>
    public class LmuCompanion
    {
        public int Id { get; set; }

        public int LmuDriverStatsId { get; set; }
        public LmuDriverStats? DriverStats { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        public CompanionKind Kind { get; set; }

        public int RacesShared { get; set; }
    }
}
