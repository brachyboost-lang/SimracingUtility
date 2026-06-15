using Microsoft.AspNetCore.Mvc;
using LMU.Agent.Core.Services;
using LMU.Agent.Core.Models;

namespace LMU.Agent.UI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventParser _eventParser;

    public EventsController(IEventParser eventParser)
    {
        _eventParser = eventParser;
    }

    [HttpGet]
    public async Task<ActionResult<List<Event>>> GetUpcomingEvents()
    {
        var events = await _eventParser.GetUpcomingEventsAsync();
        return Ok(events);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Event>> GetEvent(int id)
    {
        var eventItem = await _eventParser.GetEventByIdAsync(id);
        if (eventItem == null)
        {
            return NotFound();
        }
        return Ok(eventItem);
    }
}