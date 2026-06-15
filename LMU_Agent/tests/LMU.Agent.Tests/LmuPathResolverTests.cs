using LMU.Agent.Core.Services;

namespace LMU.Agent.Tests;

public class LmuPathResolverTests
{
    // Auto-Detect in den Tests deaktivieren (greift sonst aufs Dateisystem zu).
    private static readonly Func<string?> NoAuto = () => null;

    [Fact]
    public void ConfiguredValue_TakesPrecedence()
    {
        var path = LmuPathResolver.ResolveResultsPath(@"D:\Custom\Results", @"E:\Env\Results", NoAuto);
        Assert.Equal(@"D:\Custom\Results", path);
    }

    [Fact]
    public void EnvValue_UsedWhenNoConfig()
    {
        var path = LmuPathResolver.ResolveResultsPath(null, @"E:\Env\Results", NoAuto);
        Assert.Equal(@"E:\Env\Results", path);
    }

    [Fact]
    public void AutoDetect_UsedWhenNoConfigOrEnv()
    {
        var path = LmuPathResolver.ResolveResultsPath(null, null, () => @"G:\Steam\...\Results");
        Assert.Equal(@"G:\Steam\...\Results", path);
    }

    [Fact]
    public void Default_UsedWhenNothingFound()
    {
        var path = LmuPathResolver.ResolveResultsPath(null, null, NoAuto);
        Assert.Equal(LmuPathResolver.DefaultResultsPath, path);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankConfig_IgnoredInFavorOfEnv(string blank)
    {
        var path = LmuPathResolver.ResolveResultsPath(blank, @"E:\Env\Results", NoAuto);
        Assert.Equal(@"E:\Env\Results", path);
    }
}
