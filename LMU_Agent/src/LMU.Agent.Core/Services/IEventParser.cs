using LMU.Agent.Core.Models;

namespace LMU.Agent.Core.Services;

public interface IEventParser
{
    Task<List<Event>> ParseEventsAsync(string eventsPath);
    Task<List<Event>> GetUpcomingEventsAsync();
    Task<Event?> GetEventByIdAsync(int id);
}
