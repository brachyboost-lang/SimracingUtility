using Microsoft.AspNetCore.Mvc;
using LMU.Agent.Core.Services;
using LMU.Agent.Core.Models;

namespace LMU.Agent.UI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsParser _statisticsParser;

    public StatisticsController(IStatisticsParser statisticsParser)
    {
        _statisticsParser = statisticsParser;
    }

    [HttpGet]
    public async Task<ActionResult<List<Statistics>>> GetStatistics()
    {
        var statistics = await _statisticsParser.CalculateStatisticsAsync();
        return Ok(statistics);
    }

    [HttpGet("driver/{name}")]
    public async Task<ActionResult<Statistics>> GetDriverStatistics(string name)
    {
        var stats = await _statisticsParser.GetStatisticsByDriverNameAsync(name);
        if (stats == null)
        {
            return NotFound();
        }
        return Ok(stats);
    }

    [HttpPost("recalculate")]
    public async Task<ActionResult<Statistics>> RecalculateStatistics()
    {
        var stats = await _statisticsParser.CalculateAndStoreStatisticsAsync();
        return Ok(stats);
    }
}