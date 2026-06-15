using LMU.Agent.Core.Models;
using LMU.Agent.Core.Services;

namespace LMU.Agent.Tests;

public class StatisticsParserTests
{
    private static RaceResult Result(int pos, int field, double bestLap, string status, DateTime date)
        => new()
        {
            DriverName = "Alice",
            Position = pos,
            FieldSize = field,
            BestLapTime = bestLap,
            FinishStatus = status,
            RaceDate = date,
        };

    [Fact]
    public void ComputeStatistics_CountsPositionsAndDnf()
    {
        var results = new List<RaceResult>
        {
            Result(1, 10, 100.0, "None", new DateTime(2026, 6, 1)),
            Result(3, 10, 99.0,  "None", new DateTime(2026, 6, 2)),
            Result(6, 10, 101.0, "None", new DateTime(2026, 6, 3)),
            Result(8, 20, 0.0,   "DNF",               new DateTime(2026, 6, 4)),
        };

        var s = StatisticsParser.ComputeStatistics("Alice", results);

        Assert.Equal(4, s.TotalRaces);
        Assert.Equal(1, s.Wins);        // P1 in Rennen 1
        Assert.Equal(2, s.Podiums);     // Pos 1 und 3
        Assert.Equal(2, s.Top5);        // Pos 1 und 3 (6 zählt nicht)
        Assert.Equal(3, s.Top10);       // alle drei beendeten
        Assert.Equal(2, s.TopHalf);     // Feld 10 -> Top 5: Pos 1 und 3
        Assert.Equal(1, s.Dnf);
        Assert.Equal(1, s.BestPosition);
        Assert.Equal(99.0, s.FastestLapTime, 3); // DNF-Eintrag (0) wird ignoriert
        Assert.Equal(new DateTime(2026, 6, 4), s.LastRaceDate);
    }

    [Fact]
    public void ComputeStatistics_DnfNotCountedAsFinishedPosition()
    {
        var results = new List<RaceResult>
        {
            Result(1, 10, 100.0, "DNF", new DateTime(2026, 6, 1)),
        };

        var s = StatisticsParser.ComputeStatistics("Alice", results);

        Assert.Equal(0, s.Wins);
        Assert.Equal(0, s.Podiums);
        Assert.Equal(1, s.Dnf);
        Assert.Equal(0, s.BestPosition); // keine beendeten Rennen
    }

    [Fact]
    public void ComputeStatistics_FieldSizeZero_NoTopHalf()
    {
        var results = new List<RaceResult>
        {
            Result(1, 0, 100.0, "None", new DateTime(2026, 6, 1)),
        };

        var s = StatisticsParser.ComputeStatistics("Alice", results);
        Assert.Equal(0, s.TopHalf);
    }

    [Fact]
    public void ComputeStatistics_OddFieldSize_RoundsUpHalf()
    {
        // Feld 7 -> Top 50 % = Top 4 (aufgerundet). Pos 4 zählt, Pos 5 nicht.
        var inHalf = StatisticsParser.ComputeStatistics("Alice", new List<RaceResult>
        {
            Result(4, 7, 0, "None", new DateTime(2026, 6, 1)),
        });
        var outOfHalf = StatisticsParser.ComputeStatistics("Alice", new List<RaceResult>
        {
            Result(5, 7, 0, "None", new DateTime(2026, 6, 1)),
        });

        Assert.Equal(1, inHalf.TopHalf);
        Assert.Equal(0, outOfHalf.TopHalf);
    }
}
