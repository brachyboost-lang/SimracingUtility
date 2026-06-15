using Microsoft.AspNetCore.Mvc;
using LMU.Agent.Core.Services;
using LMU.Agent.Core.Models;

namespace LMU.Agent.UI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResultsController : ControllerBase
{
    private readonly IRaceResultParser _raceResultParser;

    public ResultsController(IRaceResultParser raceResultParser)
    {
        _raceResultParser = raceResultParser;
    }

    [HttpGet]
    public async Task<ActionResult<List<RaceResult>>> GetLastRaceResults()
    {
        var results = await _raceResultParser.GetLastRaceResultsAsync();
        return Ok(results);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RaceResult>> GetRaceResult(int id)
    {
        var result = await _raceResultParser.GetRaceResultByIdAsync(id);
        if (result == null)
        {
            return NotFound();
        }
        return Ok(result);
    }
}