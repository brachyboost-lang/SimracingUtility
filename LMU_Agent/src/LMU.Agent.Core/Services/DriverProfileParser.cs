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

            // Speichere die Profile in der Datenbank
            foreach (var profile in profiles)
            {
                _context.DriverProfiles.Add(profile);
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
