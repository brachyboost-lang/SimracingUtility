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

        // Sicherheitsreserve (Eingabe): zusaetzlicher Sprit, mit dem man ins Ziel
        // kommen will. Einheit "Laps" (Runden) oder "Percent" (% der Renndistanz).
        public double FuelReserve { get; set; }
        public string FuelReserveUnit { get; set; } = "Laps";

        // Ergebnis-Zusatzfelder
        public double FuelReserveLiters { get; set; } // berechnete Reserve in Litern
        public bool ReserveExceedsTank { get; set; }  // Reserve passt nicht in den letzten Tank
        public List<FuelStint> Stints { get; set; } = new();

        public void CalculateFuel()
        {
            if (TimePerLap <= 0 || FuelTankCapacity <= 0)
            {
                NumberOfPitStops = 0;
                TotalFuelNeeded = 0;
                Laps = 0;
                TotalTimeLost = 0;
                FuelReserveLiters = 0;
                ReserveExceedsTank = false;
                Stints = new List<FuelStint>();
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

            // Stint-Mitschnitt: ein Stint ist eine Runden-Serie auf einer Tankfuellung
            // (zwischen zwei Boxenstopps bzw. Start/Ziel).
            var stints = new List<FuelStint>();
            int stintStartLap = 1;
            int lapsThisStint = 0;

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

                    // Laufenden Stint vor dem Tanken abschliessen.
                    if (lapsThisStint > 0)
                    {
                        stints.Add(new FuelStint
                        {
                            StintNumber = stints.Count + 1,
                            FromLap = stintStartLap,
                            ToLap = stintStartLap + lapsThisStint - 1,
                            Laps = lapsThisStint,
                            Fuel = lapsThisStint * FuelPerLap
                        });
                        stintStartLap += lapsThisStint;
                        lapsThisStint = 0;
                    }

                    clockRemaining -= pitCost;
                    pitStops++;
                    fuelInTank = FuelTankCapacity;
                }

                // Eine im Rennen begonnene Runde wird komplett gewertet (die Zeit
                // darf dabei ablaufen) → die angefangene Runde zaehlt voll.
                laps++;
                lapsThisStint++;
                clockRemaining -= TimePerLap;
                fuelInTank -= FuelPerLap;
            }

            // Letzten (laufenden) Stint abschliessen.
            if (lapsThisStint > 0)
            {
                stints.Add(new FuelStint
                {
                    StintNumber = stints.Count + 1,
                    FromLap = stintStartLap,
                    ToLap = stintStartLap + lapsThisStint - 1,
                    Laps = lapsThisStint,
                    Fuel = lapsThisStint * FuelPerLap
                });
            }

            double consumedFuel = Math.Max(0, laps * FuelPerLap);

            // Sicherheitsreserve, Modell "mehr laden, gleiche Strategie": die Reserve
            // wird zusaetzlich geladen, die Stopp-Strategie bleibt gleich. Reicht der
            // letzte Tank nicht, um mit der Reserve ins Ziel zu kommen, wird das als
            // Hinweis markiert (ReserveExceedsTank) – ohne die Stoppzahl zu aendern.
            double reserveLaps = 0;
            if (FuelReserve > 0 && laps > 0 && FuelPerLap > 0)
            {
                reserveLaps = string.Equals(FuelReserveUnit, "Percent", StringComparison.OrdinalIgnoreCase)
                    ? laps * (FuelReserve / 100.0)
                    : FuelReserve;
            }
            FuelReserveLiters = Math.Max(0, reserveLaps * FuelPerLap);

            double endFuel = Math.Max(0, fuelInTank); // natuerlicher Rest im letzten Tank
            ReserveExceedsTank = FuelReserveLiters > endFuel + eps;

            NumberOfPitStops = pitStops;
            TotalFuelNeeded = consumedFuel + FuelReserveLiters;
            TotalTimeLost = pitStops * pitCost;
            Laps = laps;
            Stints = stints;
        }

    }

    // Ein Stint: zusammenhaengende Runden auf einer Tankfuellung (zwischen zwei
    // Boxenstopps bzw. Start/Ziel).
    public class FuelStint
    {
        public int StintNumber { get; set; }
        public int FromLap { get; set; }
        public int ToLap { get; set; }
        public int Laps { get; set; }
        public double Fuel { get; set; }
    }
}
