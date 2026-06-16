using SimracingUtility;
using SimracingUtility.Models;
namespace SimracingUtility.Tests
{
    public class FuelCalcViewModelTests
    {
        [Fact]
        public void CalculateFuel_ZeroTimeOrCapacity_ResultsZero()
        {
            var vm = new FuelCalcViewModel
            {
                EventDurationMinutes = 60,
                TimePerLap = 0,
                FuelTankCapacity = 0,
                FuelPerLap = 2,
                PitBoxTime = 20,
                DriveThroughTime = 5
            };

            vm.CalculateFuel();

            Assert.Equal(0, vm.NumberOfPitStops);
            Assert.Equal(0, vm.TotalFuelNeeded);
            Assert.Equal(0, vm.Laps);
            Assert.Equal(0, vm.TotalTimeLost);
        }

        [Fact]
        public void CalculateFuel_Simple_NoPitStops()
        {
            var vm = new FuelCalcViewModel
            {
                EventDurationMinutes = 10,
                TimePerLap = 60,
                FuelPerLap = 2,
                FuelTankCapacity = 100,
                PitBoxTime = 20,
                DriveThroughTime = 5
            };

            vm.CalculateFuel();

            double expectedLaps = 600.0 / 60.0;
            double expectedFuel = expectedLaps * 2.0;

            Assert.Equal(0, vm.NumberOfPitStops);
            Assert.True(Math.Abs(vm.Laps - expectedLaps) < 0.01);
            Assert.True(Math.Abs(vm.TotalFuelNeeded - expectedFuel) < 0.01);
            Assert.Equal(0, vm.TotalTimeLost);
        }

        [Fact]
        public void CalculateFuel_PitStops_MaintainsInvariants()
        {
            var vm = new FuelCalcViewModel
            {
                EventDurationMinutes = 120,
                TimePerLap = 60,
                FuelPerLap = 5,
                FuelTankCapacity = 100,
                PitBoxTime = 20,
                DriveThroughTime = 5
            };

            vm.CalculateFuel();

            Assert.True(vm.TotalFuelNeeded > 0);
            Assert.True(vm.Laps > 0);
            Assert.True(vm.NumberOfPitStops >= 0);
            Assert.Equal(vm.NumberOfPitStops * (vm.PitBoxTime + vm.DriveThroughTime), vm.TotalTimeLost, 5);
            Assert.True(Math.Abs(vm.Laps * vm.FuelPerLap - vm.TotalFuelNeeded) < 0.5);
        }

        [Fact]
        public void CalculateFuel_Idempotent_OnRepeatedCalls()
        {
            var vm = new FuelCalcViewModel
            {
                EventDurationMinutes = 45,
                TimePerLap = 90,
                FuelPerLap = 3,
                FuelTankCapacity = 50,
                PitBoxTime = 15,
                DriveThroughTime = 5
            };

            vm.CalculateFuel();

            var firstStops = vm.NumberOfPitStops;
            var firstFuel = vm.TotalFuelNeeded;
            var firstLaps = vm.Laps;

            vm.CalculateFuel();

            Assert.Equal(firstStops, vm.NumberOfPitStops);
            Assert.Equal(firstFuel, vm.TotalFuelNeeded, 3);
            Assert.Equal(firstLaps, vm.Laps, 3);
        }

        [Fact]
        public void CalculateFuel_Laps_AreWholeNumbers()
        {
            // #4: ganze Planungsrunde statt fraktionaler Runden (z. B. 117,92).
            var vm = new FuelCalcViewModel { EventDurationMinutes = 120, TimePerLap = 60,
                FuelPerLap = 5, FuelTankCapacity = 100, PitBoxTime = 20, DriveThroughTime = 5 };
            vm.CalculateFuel();
            Assert.True(vm.Laps == Math.Floor(vm.Laps)); // keine Nachkommastellen
        }

        [Fact]
        public void CalculateFuel_FuelMatchesLaps_NoOffByOne()
        {
            // #3: Sprit und Runden/Stopps stammen aus derselben Rechnung
            // (vorher 1-Schritt-Versatz zwischen altem und neuem Stopp-Wert).
            var vm = new FuelCalcViewModel { EventDurationMinutes = 120, TimePerLap = 60,
                FuelPerLap = 5, FuelTankCapacity = 100, PitBoxTime = 20, DriveThroughTime = 5 };
            vm.CalculateFuel();
            Assert.Equal(vm.Laps * vm.FuelPerLap, vm.TotalFuelNeeded, 6);
        }

        [Fact]
        public void CalculateFuel_Deterministic_AtTankBoundary()
        {
            // #3: Im Tank-Grenzfall darf das Ergebnis nicht pendeln.
            var vm = new FuelCalcViewModel { EventDurationMinutes = 60, TimePerLap = 60,
                FuelPerLap = 10, FuelTankCapacity = 100, PitBoxTime = 120, DriveThroughTime = 0 };
            vm.CalculateFuel();
            var stops = vm.NumberOfPitStops; var fuel = vm.TotalFuelNeeded; var laps = vm.Laps;
            vm.CalculateFuel();
            Assert.Equal(stops, vm.NumberOfPitStops);
            Assert.Equal(fuel, vm.TotalFuelNeeded, 6);
            Assert.Equal(laps, vm.Laps, 6);
        }

        [Fact]
        public void CalculateFuel_PitTime_ReducesLaps()
        {
            // Kernpunkt: Boxenstopps kosten Streckenzeit und verringern die Runden.
            // Gleiche Eingaben, nur die Boxenzeit unterscheidet sich.
            var ohnePit = new FuelCalcViewModel { EventDurationMinutes = 120, TimePerLap = 60,
                FuelPerLap = 5, FuelTankCapacity = 100, PitBoxTime = 0, DriveThroughTime = 0 };
            var mitPit = new FuelCalcViewModel { EventDurationMinutes = 120, TimePerLap = 60,
                FuelPerLap = 5, FuelTankCapacity = 100, PitBoxTime = 20, DriveThroughTime = 5 };
            ohnePit.CalculateFuel();
            mitPit.CalculateFuel();
            Assert.True(mitPit.NumberOfPitStops > 0);   // Stopps wegen Sprit noetig
            Assert.True(mitPit.Laps < ohnePit.Laps);    // Boxenzeit kostet Runden
        }

        [Fact]
        public void CalculateFuel_PitStops_AreSufficientForFuel()
        {
            // Sicherheit: Start-Tank + Nachtankungen muessen den Gesamtsprit decken.
            var vm = new FuelCalcViewModel { EventDurationMinutes = 240, TimePerLap = 95,
                FuelPerLap = 3.4, FuelTankCapacity = 80, PitBoxTime = 25, DriveThroughTime = 8 };
            vm.CalculateFuel();
            double capacity = (vm.NumberOfPitStops + 1) * vm.FuelTankCapacity;
            Assert.True(capacity + 1e-6 >= vm.TotalFuelNeeded); // nie zu knapp getankt
        }

        [Fact]
        public void CalculateFuel_NoPhantomPitStopAtRaceEnd()
        {
            // Grenzfall: Restzeit reicht fuer einen Stopp, aber nicht mehr fuer eine
            // weitere Runde -> kein zusaetzlicher Phantom-Stopp. Erwartet: 50/4.
            var vm = new FuelCalcViewModel { EventDurationMinutes = 60, TimePerLap = 60,
                FuelPerLap = 10, FuelTankCapacity = 100, PitBoxTime = 120, DriveThroughTime = 0 };
            vm.CalculateFuel();
            Assert.Equal(50, vm.Laps, 6);
            Assert.Equal(4, vm.NumberOfPitStops);
            Assert.Equal(500, vm.TotalFuelNeeded, 6);
            Assert.Equal(480, vm.TotalTimeLost, 6);
        }

        [Fact]
        public void CalculateFuel_KnownScenario_ExactValues()
        {
            // Von Hand simuliert: 2h, 60s/Runde, 5L/Runde, 100L, 25s Boxenverlust
            // -> 118 Runden, 5 Stopps, 590L, 125s Zeitverlust.
            var vm = new FuelCalcViewModel { EventDurationMinutes = 120, TimePerLap = 60,
                FuelPerLap = 5, FuelTankCapacity = 100, PitBoxTime = 20, DriveThroughTime = 5 };
            vm.CalculateFuel();
            Assert.Equal(118, vm.Laps, 6);
            Assert.Equal(5, vm.NumberOfPitStops);
            Assert.Equal(590, vm.TotalFuelNeeded, 6);
            Assert.Equal(125, vm.TotalTimeLost, 6);
        }

        // ----- Sicherheitsreserve ("mehr laden, gleiche Strategie") -----

        [Fact]
        public void CalculateFuel_Reserve_Laps_AddsFuel_KeepsStops()
        {
            var vm = new FuelCalcViewModel { EventDurationMinutes = 10, TimePerLap = 60,
                FuelPerLap = 2, FuelTankCapacity = 100, PitBoxTime = 20, DriveThroughTime = 5,
                FuelReserve = 2, FuelReserveUnit = "Laps" };
            vm.CalculateFuel();
            Assert.Equal(10, vm.Laps, 6);
            Assert.Equal(0, vm.NumberOfPitStops);
            Assert.Equal(4, vm.FuelReserveLiters, 6);   // 2 Runden * 2 L
            Assert.Equal(24, vm.TotalFuelNeeded, 6);    // 20 Verbrauch + 4 Reserve
            Assert.False(vm.ReserveExceedsTank);
        }

        [Fact]
        public void CalculateFuel_Reserve_Percent_AddsFuel()
        {
            var vm = new FuelCalcViewModel { EventDurationMinutes = 10, TimePerLap = 60,
                FuelPerLap = 2, FuelTankCapacity = 100, PitBoxTime = 20, DriveThroughTime = 5,
                FuelReserve = 10, FuelReserveUnit = "Percent" };
            vm.CalculateFuel();
            // 10 % von 10 Runden = 1 Runde = 2 L Reserve
            Assert.Equal(2, vm.FuelReserveLiters, 6);
            Assert.Equal(22, vm.TotalFuelNeeded, 6);
        }

        [Fact]
        public void CalculateFuel_Reserve_Zero_NoChange()
        {
            var vm = new FuelCalcViewModel { EventDurationMinutes = 120, TimePerLap = 60,
                FuelPerLap = 5, FuelTankCapacity = 100, PitBoxTime = 20, DriveThroughTime = 5,
                FuelReserve = 0 };
            vm.CalculateFuel();
            Assert.Equal(590, vm.TotalFuelNeeded, 6);
            Assert.Equal(0, vm.FuelReserveLiters, 6);
            Assert.False(vm.ReserveExceedsTank);
        }

        [Fact]
        public void CalculateFuel_Reserve_ExceedsLastTank_SetsFlag()
        {
            // Referenzszenario endet mit 10 L Rest im letzten Tank; 3 Runden Reserve
            // (15 L) passen nicht mehr -> Hinweis, Stoppzahl bleibt aber gleich.
            var vm = new FuelCalcViewModel { EventDurationMinutes = 120, TimePerLap = 60,
                FuelPerLap = 5, FuelTankCapacity = 100, PitBoxTime = 20, DriveThroughTime = 5,
                FuelReserve = 3, FuelReserveUnit = "Laps" };
            vm.CalculateFuel();
            Assert.True(vm.ReserveExceedsTank);
            Assert.Equal(5, vm.NumberOfPitStops);       // Strategie unveraendert
            Assert.Equal(605, vm.TotalFuelNeeded, 6);   // 590 + 15
        }

        // ----- Stint-Plan -----

        [Fact]
        public void CalculateFuel_Stints_CoverAllLapsAndFuel()
        {
            var vm = new FuelCalcViewModel { EventDurationMinutes = 120, TimePerLap = 60,
                FuelPerLap = 5, FuelTankCapacity = 100, PitBoxTime = 20, DriveThroughTime = 5 };
            vm.CalculateFuel();
            Assert.Equal(vm.NumberOfPitStops + 1, vm.Stints.Count); // 1 Stint mehr als Stopps
            Assert.Equal((int)vm.Laps, vm.Stints.Sum(s => s.Laps));
            Assert.Equal(vm.Laps * vm.FuelPerLap, vm.Stints.Sum(s => s.Fuel), 6);
        }

        [Fact]
        public void CalculateFuel_Stints_AreContiguousFromLapOne()
        {
            var vm = new FuelCalcViewModel { EventDurationMinutes = 120, TimePerLap = 60,
                FuelPerLap = 5, FuelTankCapacity = 100, PitBoxTime = 20, DriveThroughTime = 5 };
            vm.CalculateFuel();
            Assert.Equal(1, vm.Stints.First().FromLap);
            for (int i = 1; i < vm.Stints.Count; i++)
                Assert.Equal(vm.Stints[i - 1].ToLap + 1, vm.Stints[i].FromLap);
            Assert.Equal((int)vm.Laps, vm.Stints.Last().ToLap);
        }

        [Fact]
        public void CalculateFuel_Stints_EmptyWhenInputInvalid()
        {
            var vm = new FuelCalcViewModel { EventDurationMinutes = 60, TimePerLap = 0,
                FuelPerLap = 2, FuelTankCapacity = 0 };
            vm.CalculateFuel();
            Assert.Empty(vm.Stints);
        }
    }
}
