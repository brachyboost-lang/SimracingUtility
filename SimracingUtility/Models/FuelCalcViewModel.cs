using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimracingUtility.Models
{
    [NotMapped]
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

            double eventSeconds = EventDurationMinutes * 60.0;
            double pitCost = PitBoxTime + DriveThroughTime;
            const double eps = 1e-9; // absorbiert Gleitkomma-Rauschen an Grenzwerten

            // Direkte Rennsimulation: Runden und Boxenstopps sind gegenseitig
            // abhaengig – ein Stopp kostet Streckenzeit → weniger Runden → weniger
            // Sprit → evtl. weniger Stopps. Statt diese Rueckkopplung iterativ zu
            // schaetzen (was im Tank-Grenzfall pendelte und ein nicht-deterministi-
            // sches Ergebnis lieferte), wird das Rennen Runde fuer Runde nachgefahren.
            // Das koppelt Runden und Stopps exakt und ist deterministisch.
            int laps = 0;
            int pitStops = 0;
            double clockRemaining = eventSeconds; // verbleibende Rennzeit in Sekunden
            double fuelInTank = FuelTankCapacity;  // Start mit vollem Tank
            const int maxLaps = 1_000_000;         // Sicherheitsnetz gegen Extremeingaben

            while (clockRemaining > eps && laps < maxLaps)
            {
                // Reicht der Sprit nicht fuer die naechste Runde? Zuerst tanken.
                if (FuelPerLap > 0 && fuelInTank < FuelPerLap - eps)
                {
                    // Ein voller Tank muss ueberhaupt fuer eine Runde reichen.
                    if (FuelTankCapacity < FuelPerLap) break;
                    // Nur tanken, wenn danach noch Zeit fuer mindestens eine Runde
                    // bleibt – sonst waere es ein Phantom-Stopp ohne weitere Runde.
                    if (clockRemaining - pitCost <= eps) break;
                    clockRemaining -= pitCost;
                    pitStops++;
                    fuelInTank = FuelTankCapacity;
                }

                // Eine im Rennen begonnene Runde wird komplett gewertet (die Zeit
                // darf dabei ablaufen) → die angefangene Runde zaehlt voll.
                laps++;
                clockRemaining -= TimePerLap;
                fuelInTank -= FuelPerLap;
            }

            NumberOfPitStops = pitStops;
            TotalFuelNeeded = Math.Max(0, laps * FuelPerLap);
            TotalTimeLost = pitStops * pitCost;
            Laps = laps;
        }

    }
}
