using System.Linq;
using LMU.Agent.Core.Models;
using LMU.Agent.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace LMU.Agent.Core.Services;

public class StatisticsParser : IStatisticsParser
{
    private readonly LMUAgentContext _context;

    public StatisticsParser(LMUAgentContext context)
    {
        _context = context;
    }

    public async Task<List<Statistics>> CalculateStatisticsAsync()
    {
        var driverNames = await _context.DriverProfiles
            .Select(d => d.Name)
            .Distinct()
            .ToListAsync();

        var statisticsList = new List<Statistics>();

        foreach (var driverName in driverNames)
        {
            var stats = await CalculateDriverStatisticsAsync(driverName);
            statisticsList.Add(stats);
        }

        return statisticsList;
    }

    private async Task<Statistics> CalculateDriverStatisticsAsync(string driverName)
    {
        var driverResults = await _context.RaceResults
            .Where(r => r.DriverName == driverName)
            .ToListAsync();

        if (!driverResults.Any())
        {
            return new Statistics
            {
                DriverName = driverName,
                AverageTime = 0,
                BestPosition = int.MaxValue,
                TotalRaces = 0,
                Podiums = 0,
                Wins = 0,
                FastestLapTime = double.MaxValue,
                LastRaceDate = DateTime.MinValue
            };
        }

        var totalTime = driverResults.Sum(r => r.Time);
        var bestPosition = driverResults.Min(r => r.Position);
        var podiums = driverResults.Count(r => r.Position <= 3);
        var wins = driverResults.Count(r => r.Position == 1);
        var allLapTimes = driverResults.SelectMany(r => r.LapTimes).Select(l => l.Time).ToList();
        var fastestLapTime = allLapTimes.Any() ? allLapTimes.Min() : double.MaxValue;
        var lastRaceDate = driverResults.Max(r => r.RaceDate);

        return new Statistics
        {
            DriverName = driverName,
            AverageTime = totalTime / driverResults.Count,
            BestPosition = bestPosition,
            TotalRaces = driverResults.Count,
            Podiums = podiums,
            Wins = wins,
            FastestLapTime = fastestLapTime,
            LastRaceDate = lastRaceDate
        };
    }

    public async Task<Statistics?> GetStatisticsByDriverNameAsync(string driverName)
    {
        var stats = await _context.Statistics
            .FirstOrDefaultAsync(s => s.DriverName == driverName);

        return stats;
    }

    public async Task<Statistics?> CalculateAndStoreStatisticsAsync()
    {
        var statisticsList = await CalculateStatisticsAsync();

        foreach (var stats in statisticsList)
        {
            _context.Statistics.Add(stats);
        }

        await _context.SaveChangesAsync();

        return statisticsList.FirstOrDefault();
    }
}