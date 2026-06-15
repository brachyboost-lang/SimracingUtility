using LMU.Agent.Core.Models;
using LMU.Agent.Core.Services;

namespace LMU.Agent.Tests;

public class DashboardBuilderTests
{
    private static RaceResult R(string driver, string session, int pos = 1, int field = 10,
        bool human = true, int minutes = 20, string track = "Monza", double lap = 0, string team = "")
        => new()
        {
            DriverName = driver,
            SessionId = session,
            Position = pos,
            FieldSize = field,
            IsPlayer = human,
            RaceMinutes = minutes,
            TrackName = track,
            BestLapTime = lap,
            TeamName = team,
            FinishStatus = "None",
            RaceDate = new DateTime(2026, 1, 1),
        };

    [Fact]
    public void Build_IgnoresAiSessionsEntirely()
    {
        var rows = new List<RaceResult>
        {
            R("Me", "s1"), R("R1", "s1"), R("R2", "s1"),
            R("Me", "s2"), R("R1", "s2"),
            R("Me", "s4"), R("R2", "s4"),
            // s3 ist ein KI-Rennen (enthält einen Bot) -> komplett ignorieren
            R("Me", "s3"), R("R3", "s3"), R("Bot", "s3", human: false),
        };

        var d = DashboardBuilder.Build(rows);

        Assert.Equal("Me", d.DriverName);
        Assert.Equal(3, d.Sprint.TotalRaces);                         // s1,s2,s4 (nicht s3)
        Assert.DoesNotContain(d.MostRacedWith, c => c.Name == "R3");  // nur im KI-Rennen
        Assert.DoesNotContain(d.MostRacedWith, c => c.Name == "Bot");
    }

    [Fact]
    public void Build_MostRacedWith_CountsSharedSessions()
    {
        var rows = new List<RaceResult>
        {
            R("Me", "s1"), R("R1", "s1"), R("R2", "s1"),
            R("Me", "s2"), R("R1", "s2"),
            R("Me", "s4"), R("R2", "s4"),
        };

        var d = DashboardBuilder.Build(rows);

        Assert.Equal(2, d.MostRacedWith.Single(c => c.Name == "R1").RacesShared); // s1,s2
        Assert.Equal(2, d.MostRacedWith.Single(c => c.Name == "R2").RacesShared); // s1,s4
    }

    [Fact]
    public void Build_SplitsSprintAndEndurance()
    {
        var rows = new List<RaceResult>
        {
            R("Me", "s1", pos: 1, minutes: 20),    // Sprint, Sieg
            R("R1", "s1", pos: 2, minutes: 20),
            R("Me", "s2", pos: 2, minutes: 240),   // Endurance, Podium
            R("R1", "s2", pos: 1, minutes: 240),
        };

        var d = DashboardBuilder.Build(rows);

        Assert.Equal(1, d.Sprint.TotalRaces);
        Assert.Equal(1, d.Sprint.Wins);
        Assert.Equal(1, d.Endurance.TotalRaces);
        Assert.Equal(0, d.Endurance.Wins);
        Assert.Equal(1, d.Endurance.Podiums);
    }

    [Fact]
    public void Build_BestLapsByTrack_PicksMinPerTrack()
    {
        var rows = new List<RaceResult>
        {
            R("Me", "s1", track: "Monza", lap: 100.0),
            R("R1", "s1", track: "Monza", lap: 95.0),   // anderer Fahrer zählt nicht
            R("Me", "s2", track: "Monza", lap: 98.0),
            R("Me", "s3", track: "Spa",   lap: 120.0),
        };

        var d = DashboardBuilder.Build(rows);

        Assert.Equal(98.0, d.BestLapsByTrack.Single(t => t.Track == "Monza").BestLapTime, 3);
        Assert.Equal(120.0, d.BestLapsByTrack.Single(t => t.Track == "Spa").BestLapTime, 3);
    }

    [Fact]
    public void Build_MostRacedAgainstTeams_ExcludesStandardLiveriesAndOwnTeam()
    {
        var rows = new List<RaceResult>
        {
            // s1: Standard-Livery "Default GT3" von zwei Fahrern -> wird gefiltert
            R("Me", "s1", team: "MyTeam"),
            R("R1", "s1", team: "Custom A"),
            R("R2", "s1", team: "Default GT3"),
            R("R3", "s1", team: "Default GT3"),
            // s2: Custom A erneut -> 2 gemeinsame Sessions
            R("Me", "s2", team: "MyTeam"),
            R("R1", "s2", team: "Custom A"),
            // s4 nur damit "Me" die meisten Ergebnisse hat
            R("Me", "s4", team: "MyTeam"),
        };

        var d = DashboardBuilder.Build(rows);

        Assert.Equal("Me", d.DriverName);
        Assert.Equal(2, d.MostRacedAgainstTeams.Single(t => t.Name == "Custom A").RacesShared);
        Assert.DoesNotContain(d.MostRacedAgainstTeams, t => t.Name == "Default GT3"); // Standard
        Assert.DoesNotContain(d.MostRacedAgainstTeams, t => t.Name == "MyTeam");      // eigenes Team
    }

    [Fact]
    public void Build_OnlyAiSessions_ReturnsEmpty()
    {
        var rows = new List<RaceResult>
        {
            R("Me", "s1"), R("Bot", "s1", human: false),
        };
        var d = DashboardBuilder.Build(rows);
        Assert.Equal(string.Empty, d.DriverName);
    }
}
