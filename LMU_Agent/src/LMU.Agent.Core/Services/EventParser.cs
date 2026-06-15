using System.IO;
using System.Text.Json;
using LMU.Agent.Core.Models;
using LMU.Agent.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace LMU.Agent.Core.Services;

public class EventParser : IEventParser
{
    private readonly LMUAgentContext _context;

    public EventParser(LMUAgentContext context)
    {
        _context = context;
    }

    public async Task<List<Event>> ParseEventsAsync(string eventsPath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(eventsPath);
            var events = JsonSerializer.Deserialize<List<Event>>(json);
            
            if (events == null || !events.Any())
            {
                return new List<Event>();
            }

            // Idempotentes Schreiben: Upsert anhand des natuerlichen Schluessels
            // (Name + Date). So fuehrt wiederholtes Parsen derselben Datei nicht
            // zu Duplikaten, sondern aktualisiert bestehende Eintraege.
            foreach (var eventItem in events)
            {
                var existing = await _context.Events
                    .FirstOrDefaultAsync(e => e.Name == eventItem.Name && e.Date == eventItem.Date);

                if (existing == null)
                {
                    _context.Events.Add(eventItem);
                }
                else
                {
                    existing.Location = eventItem.Location;
                    existing.Description = eventItem.Description;
                    existing.IsActive = eventItem.IsActive;
                }
            }
            await _context.SaveChangesAsync();

            return events;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Parsen von Events: {ex.Message}");
            return new List<Event>();
        }
    }

    public async Task<List<Event>> GetUpcomingEventsAsync()
    {
        var today = DateTime.Today;
        var upcomingEvents = await _context.Events
            .Where(e => e.Date > today && e.IsActive)
            .ToListAsync();

        return upcomingEvents;
    }

    public async Task<Event?> GetEventByIdAsync(int id)
    {
        return await _context.Events.FindAsync(id);
    }
}
