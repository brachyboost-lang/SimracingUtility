using System.Xml.Linq;
using LMU.Agent.Core.Services;

namespace LMU.Agent.Tests;

public class RaceResultParserTests
{
    // Beispiel im echten LMU-/rFactor-2-Ergebnisformat: zwei Klassen (Hypercar,
    // GT3), Datum als TimeString, Strecke als TrackCourse, FinishStatus "None"
    // für beendet und "DNF" für Ausfall.
    private const string SampleXml = """
        <rFactorXML>
          <RaceResults>
            <DateTime>1781465400</DateTime>
            <TimeString>2026/06/14 21:30:00</TimeString>
            <TrackCourse>Autodromo Nazionale Monza</TrackCourse>
            <Race>
              <TimeString>2026/06/14 21:55:00</TimeString>
              <Driver>
                <Name>Charlie</Name>
                <CarClass>Hypercar</CarClass>
                <CarNumber>7</CarNumber>
                <Position>1</Position>
                <ClassPosition>1</ClassPosition>
                <Laps>24</Laps>
                <BestLapTime>210.123</BestLapTime>
                <FinishStatus>None</FinishStatus>
              </Driver>
              <Driver>
                <Name>Dave</Name>
                <CarClass>Hypercar</CarClass>
                <CarNumber>8</CarNumber>
                <Position>2</Position>
                <ClassPosition>2</ClassPosition>
                <Laps>12</Laps>
                <BestLapTime>0</BestLapTime>
                <FinishStatus>DNF</FinishStatus>
              </Driver>
              <Driver>
                <Name>Alice</Name>
                <CarClass>GT3</CarClass>
                <CarNumber>21</CarNumber>
                <Position>3</Position>
                <ClassPosition>1</ClassPosition>
                <Laps>23</Laps>
                <BestLapTime>225.500</BestLapTime>
                <FinishStatus>None</FinishStatus>
              </Driver>
              <Driver>
                <Name>Bob</Name>
                <CarClass>GT3</CarClass>
                <CarNumber>22</CarNumber>
                <Position>5</Position>
                <ClassPosition>2</ClassPosition>
                <Laps>23</Laps>
                <BestLapTime>226.000</BestLapTime>
                <FinishStatus>None</FinishStatus>
              </Driver>
            </Race>
          </RaceResults>
        </rFactorXML>
        """;

    [Fact]
    public void ParseResults_ReadsAllDriversWithFields()
    {
        var results = RaceResultParser.ParseResults(XDocument.Parse(SampleXml));

        Assert.Equal(4, results.Count);

        var alice = results.Single(r => r.DriverName == "Alice");
        Assert.Equal(1, alice.Position);          // Klassenposition
        Assert.Equal(3, alice.OverallPosition);   // Gesamtposition
        Assert.Equal("GT3", alice.CarClass);
        Assert.Equal(23, alice.Laps);
        Assert.Equal(225.5, alice.BestLapTime, 3);
        Assert.Equal("Autodromo Nazionale Monza", alice.TrackName);
        Assert.Equal(new DateTime(2026, 6, 14, 21, 30, 0), alice.RaceDate);
    }

    [Fact]
    public void ParseResults_FieldSizeIsPerClass()
    {
        var results = RaceResultParser.ParseResults(XDocument.Parse(SampleXml));
        // 2 Hypercar + 2 GT3 -> jede Klasse hat Feldgröße 2.
        Assert.All(results, r => Assert.Equal(2, r.FieldSize));
    }

    [Fact]
    public void ParseResults_DetectsDnf_NoneMeansFinished()
    {
        var results = RaceResultParser.ParseResults(XDocument.Parse(SampleXml));

        Assert.False(results.Single(r => r.DriverName == "Alice").IsDnf);   // None
        Assert.False(results.Single(r => r.DriverName == "Charlie").IsDnf); // None
        Assert.True(results.Single(r => r.DriverName == "Dave").IsDnf);     // DNF
    }

    [Fact]
    public void ParseResults_FallsBackToUnixTimestamp_WhenNoTimeString()
    {
        var xml = """
            <rFactorXML><RaceResults>
              <DateTime>1781465400</DateTime>
              <Race><Driver><Name>X</Name><ClassPosition>1</ClassPosition>
              <FinishStatus>None</FinishStatus></Driver></Race>
            </RaceResults></rFactorXML>
            """;
        var r = RaceResultParser.ParseResults(XDocument.Parse(xml)).Single();
        Assert.NotEqual(DateTime.MinValue, r.RaceDate);
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
        Assert.False(result.IsDnf); // leerer Status = nicht als DNF werten
    }
}
