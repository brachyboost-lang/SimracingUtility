using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimracingUtility.Models
{
    public class RecentFuelCalculation
    {
        public int Id { get; set; }

        [Required]
        public int EventDurationMinutes { get; set; }

        [Required]
        [StringLength(200)]
        public string TrackName { get; set; } = string.Empty;

        public double PitBoxTime { get; set; }
        public double FuelPerLap { get; set; }

        public double TotalFuelNeeded { get; set; }
        public int NumberOfPitStops { get; set; }
        public double DriveThroughTime { get; set; }
        public double TotalTimeLost { get; set; }

        [Required]
        [StringLength(200)]
        public string CarName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CarClass { get; set; } = string.Empty;

        public double FuelTankCapacity { get; set; }
        public double TimePerLap { get; set; }
        public double Laps { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}