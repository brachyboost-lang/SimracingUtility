using LMU.Agent.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace LMU.Agent.UI.Controllers;

// Liefert eine HTML-Übersicht der berechneten Fahrer-Statistiken. Dient zugleich
// als Sichtprüfung, ob der Agent die LMU-Ergebnisse korrekt erfasst hat.
public class StatsController : Controller
{
    private readonly IStatisticsParser _statisticsParser;

    public StatsController(IStatisticsParser statisticsParser)
    {
        _statisticsParser = statisticsParser;
    }

    [HttpGet("/")]
    [HttpGet("/stats")]
    public async Task<IActionResult> Index()
    {
        var stats = await _statisticsParser.CalculateStatisticsAsync();
        return View(stats);
    }
}
