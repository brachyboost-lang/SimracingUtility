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
    public void Build_IdentifiesOwnerAsDriverWithMostResults()
    {
        var rows = new List<RaceResult>
        {
            R("Me", "s1"), R("R1", "s1"),
            R("Me", "s2"), R("R1", "s2"),
            R("Me", "s3"), R("R2", "s3"),
        };

        var d = DashboardBuilder.Build(rows);
        Assert.Equal("Me", d.DriverName);
        Assert.Equal(3, d.Sprint.TotalRaces);
    }

    [Fact]
    public void Build_ConfiguredDriver_OverridesMostResults()
    {
        var rows = new List<RaceResult>
        {
            R("Me", "s1"), R("Champ", "s1"),
            R("Champ", "s2"), R("X", "s2"),
            R("Champ", "s3"), R("Y", "s3"),
        };

        // Champ hat die meisten Ergebnisse, aber der konfigurierte Fahrer gewinnt.
        var d = DashboardBuilder.Build(rows, configuredDriver: "Me");
        Assert.Equal("Me", d.DriverName);
    }

    [Fact]
    public void Build_ExcludesSoloVsAi_KeepsBackfillRaces()
    {
        var rows = new List<RaceResult>
        {
            R("Me", "s1"), R("R1", "s1"), R("R2", "s1"),
            R("Me", "s2"), R("R1", "s2"),
            R("Me", "s4"), R("R2", "s4"), R("Bot", "s4", human: false),  // 2 Menschen + KI -> echtes Rennen
            R("Me", "s3"), R("Bot", "s3", human: false),                 // nur ich + KI -> Training
        };

        var d = DashboardBuilder.Build(rows);

        Assert.Equal(3, d.Sprint.TotalRaces);                          // s1,s2,s4 (nicht s3)
        Assert.Equal(2, d.MostRacedWith.Single(c => c.Name == "R1").RacesShared);
        Assert.Equal(2, d.MostRacedWith.Single(c => c.Name == "R2").RacesShared);
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
        Assert.Equal(2, d.MostRacedWith.Single(c => c.Name == "R1").RacesShared);
        Assert.Equal(2, d.MostRacedWith.Single(c => c.Name == "R2").RacesShared);
    }

    [Fact]
    public void Build_SplitsSprintAndEndurance()
    {
        var rows = new List<RaceResult>
        {
            R("Me", "s1", pos: 1, minutes: 20), R("A", "s1"),     // Sprint, Sieg
            R("Me", "s2", pos: 2, minutes: 240), R("B", "s2"),    // Endurance, Podium
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
            R("Me", "s1", track: "Monza", lap: 100.0), R("A", "s1", track: "Monza", lap: 95.0),
            R("Me", "s2", track: "Monza", lap: 98.0),  R("B", "s2", track: "Monza"),
            R("Me", "s3", track: "Spa",   lap: 120.0), R("C", "s3", track: "Spa"),
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
            R("Me", "s1", team: "MyTeam"), R("Helper", "s1", team: "MyTeam"),
            R("R2", "s1", team: "Default GT3"), R("R3", "s1", team: "Default GT3"),  // 2 Fahrer/Rennen -> Stock
            R("Me", "s2", team: "MyTeam"), R("R4", "s2", team: "Custom A"),
            R("Me", "s5", team: "MyTeam"), R("R1", "s5", team: "Custom A"),           // Custom A: 2 Fahrer
        };

        var d = DashboardBuilder.Build(rows);

        Assert.Equal("Me", d.DriverName);
        Assert.Equal(2, d.MostRacedAgainstTeams.Single(t => t.Name == "Custom A").RacesShared);
        Assert.DoesNotContain(d.MostRacedAgainstTeams, t => t.Name == "Default GT3");
        Assert.DoesNotContain(d.MostRacedAgainstTeams, t => t.Name == "MyTeam");
    }

    [Fact]
    public void Build_StockLivery_DetectedByYearAndNumberPattern()
    {
        var rows = new List<RaceResult>
        {
            R("Me", "s1"), R("R1", "s1", team: "Akkodis ASP Team 2025 #87"),  // Stock-Muster
            R("Me", "s2"), R("R2", "s2", team: "Cool Custom Crew"),
            R("Me", "s3"), R("R5", "s3", team: "Cool Custom Crew"),           // 2 Fahrer -> echtes Team
        };

        var d = DashboardBuilder.Build(rows);
        Assert.Contains(d.MostRacedAgainstTeams, t => t.Name == "Cool Custom Crew");
        Assert.DoesNotContain(d.MostRacedAgainstTeams, t => t.Name == "Akkodis ASP Team 2025 #87");
    }

    [Fact]
    public void Build_OfficialTeamWithoutYear_FilteredViaCuratedList()
    {
        var rows = new List<RaceResult>
        {
            R("Me", "s1"), R("R1", "s1", team: "United Autosports #22"),
            R("Me", "s2"), R("R6", "s2", team: "United Autosports #22"),  // 2 Fahrer, aber offiziell
            R("Me", "s3"), R("Z", "s3"),
        };

        var d = DashboardBuilder.Build(rows);
        Assert.DoesNotContain(d.MostRacedAgainstTeams, t => t.Name == "United Autosports #22");
    }

    [Fact]
    public void Build_TeamNameAlreadyAmongRacedWith_IsRemovedFromTeams()
    {
        var rows = new List<RaceResult>
        {
            R("Me", "s1"), R("Bob", "s1"), R("Carl", "s1", team: "Bob"),
            R("Me", "s2"), R("Bob", "s2"), R("Dave", "s2", team: "Bob"),   // Team "Bob": 2 Fahrer
            R("Me", "s3"), R("Z", "s3"),                                   // Me führt klar
        };

        var d = DashboardBuilder.Build(rows);
        Assert.Contains(d.MostRacedWith, c => c.Name == "Bob");
        Assert.DoesNotContain(d.MostRacedAgainstTeams, t => t.Name == "Bob");
    }

    [Fact]
    public void Build_SingleDriverTeam_NotCountedAsTeam()
    {
        var rows = new List<RaceResult>
        {
            R("Me", "s1"), R("Solo", "s1", team: "Solos Garage"),  // nur 1 Fahrer
            R("Me", "s2"), R("X", "s2"),
            R("Me", "s3"), R("Y", "s3"),
        };

        var d = DashboardBuilder.Build(rows);
        Assert.DoesNotContain(d.MostRacedAgainstTeams, t => t.Name == "Solos Garage");
    }

    [Fact]
    public void Build_OnlyAiSession_ReturnsEmpty()
    {
        var rows = new List<RaceResult>
        {
            R("Me", "s1"), R("Bot", "s1", human: false),  // nur 1 Mensch -> kein Wettkampf
        };

        var d = DashboardBuilder.Build(rows);
        Assert.Equal(string.Empty, d.DriverName);
    }

    [Fact]
    public void Build_EmptyInput_ReturnsEmptyDashboard()
    {
        var d = DashboardBuilder.Build(new List<RaceResult>());
        Assert.Equal(string.Empty, d.DriverName);
        Assert.Empty(d.MostRacedWith);
        Assert.Empty(d.MostRacedAgainstTeams);
    }
}
