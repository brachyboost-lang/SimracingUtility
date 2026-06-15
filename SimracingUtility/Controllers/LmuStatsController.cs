using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimracingUtility.Data;

namespace SimracingUtility.Controllers
{
    // Zeigt die vom LMU-Agent gepushten Statistiken des Nutzers an.
    public class LmuStatsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;

        public LmuStatsController(ApplicationDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task<IActionResult> Index()
        {
            // Basis-URL des lokalen Agent-Telemetrie-Servers (läuft auf dem Rechner
            // des Nutzers); die Strecken-Tabelle verlinkt dort die ZIP-Downloads.
            ViewBag.TelemetryBase = _config["Lmu:AgentTelemetryUrl"] ?? "http://localhost:5601";

            // "Nur die Statistiken des Users" – zuletzt aktualisierter Fahrer.
            var driver = await _db.LmuDrivers
                .Include(d => d.Categories)
                .Include(d => d.TrackBests)
                .Include(d => d.RacedWith)
                .OrderByDescending(d => d.UpdatedAt)
                .FirstOrDefaultAsync();

            return View(driver);
        }
    }
}
