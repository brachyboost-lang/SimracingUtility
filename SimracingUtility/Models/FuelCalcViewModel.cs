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
        public double TotalFuelNeeded { get; set; }
        public int NumberOfPitStops { get; set; }
        public double DriveThroughTime { get; set; }
        public double TotalTimeLost { get; set; }
        public string CarName { get; set; } = string.Empty;
        public string CarClass { get; set; } = string.Empty;
        public double FuelTankCapacity { get; set; }
        public double TimePerLap { get; set; }
        public double Laps { get; set; }

        public void CalculateFuel()
        {
            if (TimePerLap <= 0 || FuelTankCapacity <= 0)
            {
                NumberOfPitStops = 0;
                TotalFuelNeeded = 0;
                Laps = 0;
                TotalTimeLost = 0;
                return;
            }

            double totalFuel = 0;
            int pitStops = 0;
            double prevFuel;
            int maxIter = 40;
            int iter = 0;

            double eventSeconds = EventDurationMinutes * 60.0;

            do
            {
                prevFuel = totalFuel;
                double availableSeconds = Math.Max(0, eventSeconds - pitStops * (PitBoxTime + DriveThroughTime));
                double laps = availableSeconds / TimePerLap;
                totalFuel = Math.Max(0, laps * FuelPerLap);
                pitStops = Math.Max(0, (int)Math.Ceiling(totalFuel / FuelTankCapacity) - 1);
                iter++;
            } while (iter < maxIter && Math.Abs(totalFuel - prevFuel) > 0.01);

            NumberOfPitStops = pitStops;
            TotalFuelNeeded = totalFuel;
            TotalTimeLost = NumberOfPitStops * (PitBoxTime + DriveThroughTime);
            Laps = totalFuel > 0 && FuelPerLap > 0 ? totalFuel / FuelPerLap : 0;
        }

    }
}
