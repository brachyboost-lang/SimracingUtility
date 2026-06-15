using System.ComponentModel.DataAnnotations;

namespace SimracingUtility.Models
{
    /// <summary>
    /// Vom LMU-Agent gepushte Auswertung eines Fahrers (des Agent-Besitzers).
    /// Pro Fahrername genau ein Datensatz (Upsert beim Push).
    /// </summary>
    public class LmuDriver
    {
        public int Id { get; set; }

        /// <summary>
        /// Vom Host-/Login-System vergebener Nutzer-Identifier. Über diesen ordnet
        /// das einbindende System die Stats seinen Accounts zu. Leer = Einzelnutzer-
        /// /Entwicklungsmodus (Zuordnung dann über <see cref="DriverName"/>).
        /// </summary>
        [StringLength(200)]
        public string OwnerKey { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string DriverName { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; }

        public List<LmuCategoryStat> Categories { get; set; } = new();
        public List<LmuTrackBest> TrackBests { get; set; } = new();
        public List<LmuRacedWith> RacedWith { get; set; } = new();
    }

    /// <summary>Kennzahlen je Renn-Kategorie ("Sprint" oder "Endurance").</summary>
    public class LmuCategoryStat
    {
        public int Id { get; set; }
        public int LmuDriverId { get; set; }
        public LmuDriver? Driver { get; set; }

        [Required]
        [StringLength(20)]
        public string Category { get; set; } = string.Empty;

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

    /// <summary>Beste Rundenzeit des Fahrers je Strecke.</summary>
    public class LmuTrackBest
    {
        public int Id { get; set; }
        public int LmuDriverId { get; set; }
        public LmuDriver? Driver { get; set; }

        [Required]
        [StringLength(150)]
        public string Track { get; set; } = string.Empty;

        public double BestLapTime { get; set; }
    }

    /// <summary>Menschlicher Mitstreiter ("Driver") oder gegnerisches custom Team
    /// ("Team") und Anzahl gemeinsamer Rennen.</summary>
    public class LmuRacedWith
    {
        public int Id { get; set; }
        public int LmuDriverId { get; set; }
        public LmuDriver? Driver { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        /// <summary>"Driver" (Mitstreiter) oder "Team" (gegnerisches custom Team).</summary>
        [Required]
        [StringLength(20)]
        public string Kind { get; set; } = "Driver";

        public int RacesShared { get; set; }
    }
}
