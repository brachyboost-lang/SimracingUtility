using System.IO;
using System.Text.Json;
using LMU.Agent.Core.Models;
using LMU.Agent.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace LMU.Agent.Core.Services;

public class DriverProfileParser : IDriverProfileParser
{
    private readonly LMUAgentContext _context;

    public DriverProfileParser(LMUAgentContext context)
    {
        _context = context;
    }

    public async Task<List<DriverProfile>> ParseProfilesAsync(string profilePath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(profilePath);
            var profiles = JsonSerializer.Deserialize<List<DriverProfile>>(json);
            
            if (profiles == null || !profiles.Any())
            {
                return new List<DriverProfile>();
            }

            // Idempotentes Schreiben: Upsert anhand des Fahrernamens (natuerlicher
            // Schluessel). Wiederholtes Parsen aktualisiert das Profil statt es
            // doppelt anzulegen.
            foreach (var profile in profiles)
            {
                var existing = await _context.DriverProfiles
                    .FirstOrDefaultAsync(p => p.Name == profile.Name);

                if (existing == null)
                {
                    _context.DriverProfiles.Add(profile);
                }
                else
                {
                    existing.Team = profile.Team;
                    existing.CarModel = profile.CarModel;
                    existing.Class = profile.Class;
                    existing.TotalRaces = profile.TotalRaces;
                    existing.Podiums = profile.Podiums;
                    existing.Wins = profile.Wins;
                    existing.AveragePosition = profile.AveragePosition;
                }
            }
            await _context.SaveChangesAsync();

            return profiles;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Parsen von Driver Profiles: {ex.Message}");
            return new List<DriverProfile>();
        }
    }

    public async Task<List<DriverProfile>> GetDriverProfilesAsync()
    {
        var profiles = await _context.DriverProfiles.ToListAsync();
        return profiles;
    }

    public async Task<DriverProfile?> GetDriverProfileByIdAsync(int id)
    {
        return await _context.DriverProfiles.FindAsync(id);
    }
}
