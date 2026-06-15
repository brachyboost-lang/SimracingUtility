using LMU.Agent.Core.Services;

namespace LMU.Agent.Tests;

public class TelemetryLocatorTests
{
    [Fact]
    public void GameLogFolderFromResults_GoesUpToGameRoot()
    {
        var results = @"G:\Steam\steamapps\common\Le Mans Ultimate\UserData\Log\Results";
        var log = TelemetryLocator.GameLogFolderFromResults(results);
        Assert.Equal(@"G:\Steam\steamapps\common\Le Mans Ultimate\LOG", log);
    }

    [Fact]
    public void Normalize_ReducesToAsciiSkeleton()
    {
        // Nicht-ASCII (ó, é) werden verworfen -> robust gegen Mojibake in Dateinamen.
        Assert.Equal("autdromojoscarlospace", TelemetryLocator.Normalize("Autódromo José Carlos Pace"));
        Assert.Equal("autdromojoscarlospace", TelemetryLocator.Normalize("AutÃ³dromo JosÃ© Carlos Pace"));
        Assert.Equal("circuitdelasarthe", TelemetryLocator.Normalize("Circuit de la Sarthe"));
    }

    [Fact]
    public void FindTelemetryFiles_MatchesByTrackIgnoringAccents()
    {
        var dir = Path.Combine(Path.GetTempPath(), "lmutel_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "2026-01-29 - 18-17-53 - Autódromo José Carlos Pace - P1.ld"), "x");
            File.WriteAllText(Path.Combine(dir, "2026-01-29 - 18-17-53 - Autódromo José Carlos Pace - P1.ldx"), "x");
            File.WriteAllText(Path.Combine(dir, "2026-02-01 - 10-00-00 - Autodromo Nazionale Monza - R1.ld"), "x");
            File.WriteAllText(Path.Combine(dir, "readme.txt"), "x");

            var hits = TelemetryLocator.FindTelemetryFiles(dir, "Autódromo José Carlos Pace");

            Assert.Equal(2, hits.Count); // nur das .ld/.ldx-Paar, nicht Monza, nicht txt
            Assert.All(hits, h => Assert.Contains("Carlos Pace", h));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void FindTelemetryFiles_MissingFolder_ReturnsEmpty()
    {
        var hits = TelemetryLocator.FindTelemetryFiles(@"X:\does\not\exist", "Monza");
        Assert.Empty(hits);
    }

    [Fact]
    public void LatestSessionFiles_KeepsOnlyNewestSessionPair()
    {
        var files = new[]
        {
            @"C:\LOG\2026-02-01 - 10-00-00 - Monza - R1.ld",
            @"C:\LOG\2026-02-01 - 10-00-00 - Monza - R1.ldx",
            @"C:\LOG\2026-05-16 - 00-31-12 - Monza - R1.ld",   // jüngste Session
            @"C:\LOG\2026-05-16 - 00-31-12 - Monza - R1.ldx",
        };

        var latest = TelemetryLocator.LatestSessionFiles(files);

        Assert.Equal(2, latest.Count);
        Assert.All(latest, f => Assert.Contains("2026-05-16", f));
    }
}
