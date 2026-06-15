using LMU.Agent.Core.Services;

namespace LMU.Agent.Tests;

public class StandardTeamsTests
{
    [Theory]
    [InlineData("United Autosports #22")]
    [InlineData("United Autosports 2025 #59")]
    [InlineData("Akkodis ASP Team")]
    [InlineData("Toyota Gazoo Racing")]
    [InlineData("Vista AF Corse 2025 #21")]
    [InlineData("Spirit of Race")]
    public void IsOfficial_TrueForKnownTeams(string name)
        => Assert.True(StandardTeams.IsOfficial(name));

    [Theory]
    [InlineData("SDL eSports N°27")]
    [InlineData("Team JB17 MOTORSPORT")]
    [InlineData("404 Grip Not Found")]
    [InlineData("")]
    public void IsOfficial_FalseForCustomTeams(string name)
        => Assert.False(StandardTeams.IsOfficial(name));
}
