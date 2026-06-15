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
        // Fahrer werden aus den tatsächlichen Rennergebnissen abgeleitet – so
        // funktioniert die Statistik unabhängig von einer Profil-Datei.
        var allResults = await _context.RaceResults.ToListAsync();

        return allResults
            .GroupBy(r => r.DriverName)
            .Select(g => ComputeStatistics(g.Key, g.ToList()))
            .OrderByDescending(s => s.Wins)
            .ThenByDescending(s => s.Podiums)
            .ToList();
    }

    /// <summary>
    /// Berechnet die Kennzahlen eines Fahrers aus seinen Rennergebnissen.
    /// Positionsbasierte Zähler (Sieg/Podium/Top-N) zählen nur regulär beendete
    /// Rennen; DNFs werden separat gezählt.
    /// </summary>
    public static Statistics ComputeStatistics(string driverName, List<RaceResult> results)
    {
        var finished = results.Where(r => !r.IsDnf && r.Position > 0).ToList();
        var validLaps = results.Where(r => r.BestLapTime > 0).Select(r => r.BestLapTime).ToList();

        return new Statistics
        {
            DriverName = driverName,
            TotalRaces = results.Count,
            Wins = finished.Count(r => r.Position == 1),
            Podiums = finished.Count(r => r.Position <= 3),
            Top5 = finished.Count(r => r.Position <= 5),
            Top10 = finished.Count(r => r.Position <= 10),
            TopHalf = finished.Count(IsInTopHalf),
            Dnf = results.Count(r => r.IsDnf),
            BestPosition = finished.Count > 0 ? finished.Min(r => r.Position) : 0,
            FastestLapTime = validLaps.Count > 0 ? validLaps.Min() : 0d,
            LastRaceDate = results.Count > 0 ? results.Max(r => r.RaceDate) : DateTime.MinValue,
        };
    }

    private static bool IsInTopHalf(RaceResult r)
    {
        if (r.FieldSize <= 0) return false;
        var half = (int)Math.Ceiling(r.FieldSize / 2.0);
        return r.Position <= half;
    }

    public async Task<Statistics?> GetStatisticsByDriverNameAsync(string driverName)
    {
        return await _context.Statistics
            .FirstOrDefaultAsync(s => s.DriverName == driverName);
    }

    public async Task<Statistics?> CalculateAndStoreStatisticsAsync()
    {
        var statisticsList = await CalculateStatisticsAsync();

        // Idempotentes Schreiben: ein Statistik-Satz pro Fahrer wird aktualisiert
        // statt dupliziert (Upsert anhand des Fahrernamens).
        foreach (var stats in statisticsList)
        {
            var existing = await _context.Statistics
                .FirstOrDefaultAsync(s => s.DriverName == stats.DriverName);

            if (existing == null)
            {
                _context.Statistics.Add(stats);
            }
            else
            {
                existing.TotalRaces = stats.TotalRaces;
                existing.Wins = stats.Wins;
                existing.Podiums = stats.Podiums;
                existing.Top5 = stats.Top5;
                existing.Top10 = stats.Top10;
                existing.TopHalf = stats.TopHalf;
                existing.Dnf = stats.Dnf;
                existing.BestPosition = stats.BestPosition;
                existing.FastestLapTime = stats.FastestLapTime;
                existing.LastRaceDate = stats.LastRaceDate;
            }
        }

        await _context.SaveChangesAsync();

        return statisticsList.FirstOrDefault();
    }
}
