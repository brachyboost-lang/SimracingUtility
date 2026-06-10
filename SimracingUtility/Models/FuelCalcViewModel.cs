using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace SimracingUtility.Models
{
    public class FuelCalcViewModel
    {
        public int Id { get; set; }
        public int EventDurationMinutes { get; set; }
        public string TrackName { get; set; } = string.Empty;
        public double PitBoxTime { get; set; }
        public double FuelPerLap { get; set; }
        public double TotalFuelNeeded { get => FuelPerLap * (EventDurationMinutes - (NumberOfPitStops * (TotalTimeLost / 60) / (TimePerLap / 60))); }
        public int NumberOfPitStops { get => (int)Math.Ceiling(TotalFuelNeeded / FuelTankCapacity) - 1; }
        public double DriveThroughTime { get; set; }
        public double TotalTimeLost { get => (NumberOfPitStops * PitBoxTime) + (NumberOfPitStops * DriveThroughTime); }
        public string CarName { get; set; } = string.Empty;
        public string CarClass { get; set; } = string.Empty;
        public double FuelTankCapacity { get; set; }
        public double TimePerLap { get; set; }
        public double Laps { get => EventDurationMinutes / TimePerLap; }

    }
}
