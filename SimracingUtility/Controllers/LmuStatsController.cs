using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimracingUtility.Data;
using SimracingUtility.Services;

namespace SimracingUtility.Controllers
{
    // Zeigt die vom LMU-Agent gepushten Statistiken des Nutzers an.
    public class LmuStatsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly SimGridClient _simGrid;

        public LmuStatsController(ApplicationDbContext db, IConfiguration config, SimGridClient simGrid)
        {
            _db = db;
            _config = config;
            _simGrid = simGrid;
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

            // Optional: öffentliche SimGrid-Stats des selbst gepflegten Profils
            // (best effort, gecached). Schlägt es fehl, bleibt es einfach leer.
            if (!string.IsNullOrEmpty(driver?.SimGridProfileUrl))
            {
                ViewBag.SimGridStats = await _simGrid.GetStatsAsync(driver.SimGridProfileUrl);
            }

            return View(driver);
        }

        // Speichert die vom Nutzer selbst angegebene SimGrid-Profil-URL am Fahrer.
        // Nur thesimgrid.com wird akzeptiert (siehe SimGridProfile); leere Eingabe
        // entfernt den Link wieder.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetSimGrid(int id, string? simGridUrl)
        {
            var driver = await _db.LmuDrivers.FirstOrDefaultAsync(d => d.Id == id);
            if (driver == null) return NotFound();

            if (string.IsNullOrWhiteSpace(simGridUrl))
            {
                driver.SimGridProfileUrl = null;
            }
            else if (SimGridProfile.TryParse(simGridUrl, out _, out _, out var canonical))
            {
                driver.SimGridProfileUrl = canonical;
            }
            else
            {
                TempData["SimGridError"] =
                    "Bitte eine gültige SimGrid-Profil-URL angeben " +
                    "(z. B. https://www.thesimgrid.com/drivers/8444-ranokar).";
                return RedirectToAction(nameof(Index));
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
