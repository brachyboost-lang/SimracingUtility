using LMU.Agent.Core.Services;

namespace LMU.Agent.Tests;

public class LmuPathResolverTests
{
    [Fact]
    public void ConfiguredValue_TakesPrecedence()
    {
        var path = LmuPathResolver.ResolveResultsPath(@"D:\Custom\Results", @"E:\Env\Results");
        Assert.Equal(@"D:\Custom\Results", path);
    }

    [Fact]
    public void EnvValue_UsedWhenNoConfig()
    {
        var path = LmuPathResolver.ResolveResultsPath(null, @"E:\Env\Results");
        Assert.Equal(@"E:\Env\Results", path);
    }

    [Fact]
    public void Default_UsedWhenNothingSet()
    {
        var path = LmuPathResolver.ResolveResultsPath(null, null);
        Assert.Equal(LmuPathResolver.DefaultResultsPath, path);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankConfig_IgnoredInFavorOfEnv(string blank)
    {
        var path = LmuPathResolver.ResolveResultsPath(blank, @"E:\Env\Results");
        Assert.Equal(@"E:\Env\Results", path);
    }
}
