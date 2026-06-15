using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimracingUtility.Data;

namespace SimracingUtility.Controllers
{
    // Zeigt die vom LMU-Agent gepushten Statistiken des Nutzers an.
    public class LmuStatsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public LmuStatsController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
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
