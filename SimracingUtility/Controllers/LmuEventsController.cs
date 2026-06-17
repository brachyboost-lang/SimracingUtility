using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using SimracingUtility.Models;

namespace SimracingUtility.Controllers
{
    // Kuratierte Übersicht „Was steht an in Le Mans Ultimate" (Special Events +
    // Community-Meisterschaften). Bewusst getrennt vom LMU-Agent und ohne Live-Abruf:
    // die Daten kommen aus wwwroot/data/lmu-events.json.
    public class LmuEventsController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public LmuEventsController(IWebHostEnvironment env)
        {
            _env = env;
        }

        public IActionResult Index()
        {
            ViewBag.Today = DateOnly.FromDateTime(DateTime.Today);
            return View(LoadCatalog());
        }

        private LmuEventCatalog LoadCatalog()
        {
            try
            {
                var candidates = new[]
                {
                    Path.Combine(_env.WebRootPath ?? string.Empty, "data", "lmu-events.json"),
                    Path.Combine(_env.ContentRootPath ?? string.Empty, "wwwroot", "data", "lmu-events.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "lmu-events.json"),
                };

                var path = candidates.FirstOrDefault(p => !string.IsNullOrEmpty(p) && System.IO.File.Exists(p));
                if (path == null) return new LmuEventCatalog();

                var json = System.IO.File.ReadAllText(path);
                return JsonSerializer.Deserialize<LmuEventCatalog>(
                    json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new LmuEventCatalog();
            }
            catch
            {
                // Defekte/fehlende Datei soll die Seite nicht crashen lassen.
                return new LmuEventCatalog();
            }
        }
    }
}
