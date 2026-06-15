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
            // "Nur die Statistiken des Users" – wir zeigen den zuletzt
            // aktualisierten Fahrer (i. d. R. genau einen).
            var stats = await _db.LmuDriverStats
                .Include(s => s.Companions)
                .OrderByDescending(s => s.UpdatedAt)
                .FirstOrDefaultAsync();

            return View(stats);
        }
    }
}
