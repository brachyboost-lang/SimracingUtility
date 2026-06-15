using System.IO;
using System.Text.Json;
using LMU.Agent.Core.Models;
using LMU.Agent.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace LMU.Agent.Core.Services;

public class RaceResultParser : IRaceResultParser
{
    private readonly LMUAgentContext _context;

    public RaceResultParser(LMUAgentContext context)
    {
        _context = context;
    }

    public async Task<List<RaceResult>> ParseRaceResultsAsync(string savePath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(savePath);
            var results = JsonSerializer.Deserialize<List<RaceResult>>(json);
            
            if (results == null || !results.Any())
            {
                return new List<RaceResult>();
            }

            // Speichere die Ergebnisse in der Datenbank
            foreach (var result in results)
            {
                _context.RaceResults.Add(result);
            }
            await _context.SaveChangesAsync();

            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Parsen von Race Results: {ex.Message}");
            return new List<RaceResult>();
        }
    }

    public async Task<List<RaceResult>> GetLastRaceResultsAsync()
    {
        var lastResults = await _context.RaceResults
            .OrderByDescending(r => r.Time)
            .Take(10)
            .ToListAsync();

        return lastResults;
    }

    public async Task<RaceResult?> GetRaceResultByIdAsync(int id)
    {
        return await _context.RaceResults.FindAsync(id);
    }
}