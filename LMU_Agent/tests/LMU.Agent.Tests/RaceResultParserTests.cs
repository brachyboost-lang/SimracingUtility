using System.Xml.Linq;
using LMU.Agent.Core.Services;

namespace LMU.Agent.Tests;

public class RaceResultParserTests
{
    // Beispiel im rFactor-2-/LMU-Ergebnisformat mit drei Fahrern,
    // davon einer mit DNF.
    private const string SampleXml = """
        <rFactorXML>
          <RaceResults>
            <DateTime>2026-06-14 21:30:00</DateTime>
            <TrackEvent>
              <TrackName>Circuit de la Sarthe</TrackName>
            </TrackEvent>
            <Race>
              <Driver>
                <Name>Alice</Name>
                <Position>1</Position>
                <CarNumber>7</CarNumber>
                <CarClass>Hypercar</CarClass>
                <Laps>24</Laps>
                <BestLapTime>210.123</BestLapTime>
                <FinishStatus>Finished Normally</FinishStatus>
              </Driver>
              <Driver>
                <Name>Bob</Name>
                <Position>2</Position>
                <CarNumber>8</CarNumber>
                <CarClass>Hypercar</CarClass>
                <Laps>24</Laps>
                <BestLapTime>211.500</BestLapTime>
                <FinishStatus>Finished Normally</FinishStatus>
              </Driver>
              <Driver>
                <Name>Charlie</Name>
                <Position>3</Position>
                <CarNumber>9</CarNumber>
                <CarClass>LMP2</CarClass>
                <Laps>12</Laps>
                <BestLapTime>0</BestLapTime>
                <FinishStatus>DNF</FinishStatus>
              </Driver>
            </Race>
          </RaceResults>
        </rFactorXML>
        """;

    [Fact]
    public void ParseResults_ReadsAllDriversWithFields()
    {
        var results = RaceResultParser.ParseResults(XDocument.Parse(SampleXml));

        Assert.Equal(3, results.Count);

        var alice = results.Single(r => r.DriverName == "Alice");
        Assert.Equal(1, alice.Position);
        Assert.Equal("Hypercar", alice.CarClass);
        Assert.Equal("7", alice.CarNumber);
        Assert.Equal(24, alice.Laps);
        Assert.Equal(210.123, alice.BestLapTime, 3);
        Assert.Equal("Circuit de la Sarthe", alice.TrackName);
        Assert.Equal(new DateTime(2026, 6, 14, 21, 30, 0), alice.RaceDate);
    }

    [Fact]
    public void ParseResults_SetsFieldSizeToDriverCount()
    {
        var results = RaceResultParser.ParseResults(XDocument.Parse(SampleXml));
        Assert.All(results, r => Assert.Equal(3, r.FieldSize));
    }

    [Fact]
    public void ParseResults_DetectsDnfFromFinishStatus()
    {
        var results = RaceResultParser.ParseResults(XDocument.Parse(SampleXml));

        Assert.False(results.Single(r => r.DriverName == "Alice").IsDnf);
        Assert.True(results.Single(r => r.DriverName == "Charlie").IsDnf);
    }

    [Fact]
    public void ParseResults_NoRaceSession_ReturnsEmpty()
    {
        var xml = "<rFactorXML><RaceResults><Qualify /></RaceResults></rFactorXML>";
        var results = RaceResultParser.ParseResults(XDocument.Parse(xml));
        Assert.Empty(results);
    }

    [Fact]
    public void ParseResults_MissingNumbers_FallBackToZero()
    {
        var xml = """
            <rFactorXML><RaceResults><Race>
              <Driver><Name>NoData</Name></Driver>
            </Race></RaceResults></rFactorXML>
            """;
        var result = RaceResultParser.ParseResults(XDocument.Parse(xml)).Single();

        Assert.Equal(0, result.Position);
        Assert.Equal(0, result.Laps);
        Assert.Equal(0d, result.BestLapTime);
    }
}
