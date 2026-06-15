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

            // Speichere die Events in der Datenbank
            foreach (var eventItem in events)
            {
                _context.Events.Add(eventItem);
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
