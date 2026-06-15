using LMU.Agent.Core.Models;
using LMU.Agent.Core.Services;

namespace LMU.Agent.Tests;

public class DashboardBuilderTests
{
    private static RaceResult R(string driver, string session, string team, bool isPlayer = true)
        => new()
        {
            DriverName = driver,
            SessionId = session,
            TeamName = team,        // wird zu CarKey
            Position = 1,
            FieldSize = 1,
            FinishStatus = "None",
            RaceDate = new DateTime(2026, 1, 1),
            IsPlayer = isPlayer,
        };

    // Alice ist in 3 Rennen (am meisten -> Besitzer). Bob ist mal Teamkollege,
    // mal Gegner; Dave Teamkollege; Carol Gegner.
    private static List<RaceResult> Sample() => new()
    {
        R("Alice", "s1", "TeamA"), R("Bob", "s1", "TeamA"), R("Carol", "s1", "TeamB"),
        R("Alice", "s2", "TeamA"), R("Bob", "s2", "TeamB"), R("Dave", "s2", "TeamA"),
        R("Alice", "s3", "TeamA"),
    };

    [Fact]
    public void Build_IdentifiesOwnerAsDriverWithMostResults()
    {
        var d = DashboardBuilder.Build(Sample());
        Assert.Equal("Alice", d.DriverName);
        Assert.Equal(3, d.Stats.TotalRaces);
    }

    [Fact]
    public void Build_CountsTeammatesBySharedCar()
    {
        var d = DashboardBuilder.Build(Sample());

        // Bob (s1) und Dave (s2) teilten Alices Auto.
        Assert.Equal(1, d.Teammates.Single(c => c.Name == "Bob").RacesShared);
        Assert.Equal(1, d.Teammates.Single(c => c.Name == "Dave").RacesShared);
        Assert.DoesNotContain(d.Teammates, c => c.Name == "Carol");
    }

    [Fact]
    public void Build_CountsOpponentsByDifferentCar()
    {
        var d = DashboardBuilder.Build(Sample());

        // Carol (s1) und Bob (s2) fuhren in anderen Autos.
        Assert.Equal(1, d.Opponents.Single(c => c.Name == "Carol").RacesShared);
        Assert.Equal(1, d.Opponents.Single(c => c.Name == "Bob").RacesShared);
    }

    [Fact]
    public void Build_ExcludesBotsFromCompanions()
    {
        var rows = new List<RaceResult>
        {
            R("Alice", "s1", "TeamA"),                       // ich (Mensch)
            R("HumanMate", "s1", "TeamA"),                   // menschlicher Teamkollege
            R("BotMate", "s1", "TeamA", isPlayer: false),    // KI im selben Auto
            R("HumanRival", "s1", "TeamB"),                  // menschlicher Gegner
            R("BotRival", "s1", "TeamC", isPlayer: false),   // KI-Gegner
            R("Alice", "s2", "TeamA"),                       // damit Alice die meisten hat
        };

        var d = DashboardBuilder.Build(rows);

        Assert.Equal("Alice", d.DriverName);
        Assert.Contains(d.Teammates, c => c.Name == "HumanMate");
        Assert.Contains(d.Opponents, c => c.Name == "HumanRival");
        Assert.DoesNotContain(d.Teammates, c => c.Name == "BotMate");
        Assert.DoesNotContain(d.Opponents, c => c.Name == "BotRival");
    }

    [Fact]
    public void Build_NoHumans_ReturnsEmptyDashboard()
    {
        var rows = new List<RaceResult>
        {
            R("Bot1", "s1", "T1", isPlayer: false),
            R("Bot2", "s1", "T2", isPlayer: false),
        };
        var d = DashboardBuilder.Build(rows);
        Assert.Equal(string.Empty, d.DriverName);
    }

    [Fact]
    public void Build_EmptyInput_ReturnsEmptyDashboard()
    {
        var d = DashboardBuilder.Build(new List<RaceResult>());
        Assert.Equal(string.Empty, d.DriverName);
        Assert.Empty(d.Teammates);
        Assert.Empty(d.Opponents);
    }

    [Fact]
    public void Build_RespectsTopNLimit()
    {
        var rows = new List<RaceResult> { R("Me", "s1", "T0") };
        // 5 Gegner in derselben Session, aber topN = 3.
        for (int i = 0; i < 5; i++)
            rows.Add(R($"Opp{i}", "s1", $"Other{i}"));
        // "Me" muss die meisten Ergebnisse haben:
        rows.Add(R("Me", "s2", "T0"));

        var d = DashboardBuilder.Build(rows, topN: 3);
        Assert.Equal("Me", d.DriverName);
        Assert.Equal(3, d.Opponents.Count);
    }
}
