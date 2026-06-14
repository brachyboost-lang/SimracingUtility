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
    }
}
