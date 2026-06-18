using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SimracingUtility.Data;
using SimracingUtility.Models;

namespace SimracingUtility.Controllers
{
    public class SetupController : Controller
    {
        private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB – erlaubt auch ZIPs mit Telemetrie (.ld/.ldx)
        private const int MaxOverviewItems = 200;

        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public SetupController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ---------------------------------------------------------------------
        // Übersicht / Dashboard (öffentlich, mit Filterung Sim -> Auto -> Strecke)
        // ---------------------------------------------------------------------
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Index(SimGame? sim, int? carId, int? trackId)
        {
            var query = _db.Setups
                .AsNoTracking()
                .AsQueryable();

            if (sim.HasValue) query = query.Where(s => s.Sim == sim.Value);
            if (carId.HasValue) query = query.Where(s => s.CarId == carId.Value);
            if (trackId.HasValue) query = query.Where(s => s.TrackId == trackId.Value);

            // Übersicht zeigt nur Metadaten – die (potenziell große) Setup-Datei
            // FileData wird hier bewusst NICHT mitgeladen (erst beim Download).
            var setups = await query
                .OrderByDescending(s => s.CreatedAt)
                .Take(MaxOverviewItems)
                .Select(s => new Setup
                {
                    Id = s.Id,
                    OwnerId = s.OwnerId,
                    Sim = s.Sim,
                    Name = s.Name,
                    Description = s.Description,
                    LapTime = s.LapTime,
                    TrackTempCelsius = s.TrackTempCelsius,
                    CreatorName = s.CreatorName,
                    FileName = s.FileName,
                    FileSize = s.FileSize,
                    CreatedAt = s.CreatedAt,
                    Car = s.Car == null ? null : new SimCar { Name = s.Car.Name },
                    Track = s.Track == null ? null : new SimTrack { Name = s.Track.Name },
                })
                .ToListAsync();

            // Hinweis, falls die Liste durch das Limit abgeschnitten wurde (#7).
            ViewBag.OverviewCapped = setups.Count == MaxOverviewItems;
            ViewBag.OverviewLimit = MaxOverviewItems;

            ViewBag.Sim = sim;
            ViewBag.CarId = carId;
            ViewBag.TrackId = trackId;
            ViewBag.SimList = SimSelectList(sim);
            ViewBag.CarList = await CarSelectListAsync(sim, carId);
            ViewBag.TrackList = await TrackSelectListAsync(sim, trackId);
            ViewBag.CurrentUserId = _userManager.GetUserId(User);

            return View(setups);
        }

        // ---------------------------------------------------------------------
        // Upload-Formular (nur angemeldete Nutzer)
        // ---------------------------------------------------------------------
        [Authorize]
        [HttpGet]
        public IActionResult Upload()
        {
            ViewBag.SimList = SimSelectList(null);
            return View(new SetupUploadViewModel());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MaxFileSizeBytes + 256 * 1024)] // etwas Puffer für Formularfelder
        public async Task<IActionResult> Upload(SetupUploadViewModel model)
        {
            await ValidateUploadAsync(model);

            if (!ModelState.IsValid)
            {
                ViewBag.SimList = SimSelectList(model.Sim);
                return View(model);
            }

            var file = model.File!;
            byte[] data;
            await using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                data = ms.ToArray();
            }

            var entity = new Setup
            {
                OwnerId = _userManager.GetUserId(User)!,
                Sim = model.Sim!.Value,
                CarId = model.CarId!.Value,
                TrackId = model.TrackId!.Value,
                Name = model.Name?.Trim(),
                Description = model.Description?.Trim(),
                LapTime = model.LapTime?.Trim(),
                TrackTempCelsius = model.TrackTempCelsius,
                CreatorName = model.CreatorName?.Trim(),
                FileName = Path.GetFileName(file.FileName), // Pfadanteile entfernen
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                FileSize = data.LongLength,
                FileData = data,
                CreatedAt = DateTime.UtcNow
            };

            _db.Setups.Add(entity);
            await _db.SaveChangesAsync();

            TempData["SetupMessage"] = "Setup erfolgreich hochgeladen.";
            return RedirectToAction(nameof(Index), new { sim = entity.Sim, carId = entity.CarId, trackId = entity.TrackId });
        }

        // ---------------------------------------------------------------------
        // Download (nur angemeldete Nutzer)
        // ---------------------------------------------------------------------
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var setup = await _db.Setups.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
            if (setup == null) return NotFound();

            // application/octet-stream erzwingt das Herunterladen statt Inline-Anzeige.
            return File(setup.FileData, "application/octet-stream", setup.FileName);
        }

        // ---------------------------------------------------------------------
        // Löschen (nur Eigentümer)
        // ---------------------------------------------------------------------
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var setup = await _db.Setups.FirstOrDefaultAsync(s => s.Id == id);
            if (setup == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (setup.OwnerId != userId) return Forbid();

            _db.Setups.Remove(setup);
            await _db.SaveChangesAsync();

            TempData["SetupMessage"] = "Setup gelöscht.";
            return RedirectToAction(nameof(Index));
        }

        // ---------------------------------------------------------------------
        // JSON-Endpunkte für die abhängigen Dropdowns (Auto/Strecke je Sim)
        // ---------------------------------------------------------------------
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Cars(SimGame sim)
        {
            var cars = await _db.SimCars
                .Where(c => c.Sim == sim)
                .OrderBy(c => c.Name)
                .Select(c => new { id = c.Id, name = c.Name })
                .ToListAsync();
            return Json(cars);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Tracks(SimGame sim)
        {
            var tracks = await _db.SimTracks
                .Where(t => t.Sim == sim)
                .OrderBy(t => t.Name)
                .Select(t => new { id = t.Id, name = t.Name })
                .ToListAsync();
            return Json(tracks);
        }

        // ---------------------------------------------------------------------
        // Hilfsmethoden
        // ---------------------------------------------------------------------
        private async Task ValidateUploadAsync(SetupUploadViewModel model)
        {
            if (model.Sim == null)
            {
                ModelState.AddModelError(nameof(model.Sim), "Bitte eine Simulation wählen.");
                return;
            }

            // Datei vorhanden / Größe
            if (model.File == null || model.File.Length == 0)
            {
                ModelState.AddModelError(nameof(model.File), "Bitte eine Setup-Datei wählen.");
            }
            else
            {
                if (model.File.Length > MaxFileSizeBytes)
                    ModelState.AddModelError(nameof(model.File), "Die Datei ist zu groß (max. 25 MB).");

                var ext = Path.GetExtension(model.File.FileName).ToLowerInvariant();
                var allowed = SimGameInfo.ExtensionsFor(model.Sim.Value);
                if (!allowed.Contains(ext))
                    ModelState.AddModelError(nameof(model.File),
                        $"Ungültiger Dateityp für {SimGameInfo.DisplayName(model.Sim.Value)}. Erlaubt: {string.Join(", ", allowed)}");
            }

            // Auto/Strecke müssen existieren UND zur gewählten Sim gehören
            if (model.CarId.HasValue)
            {
                var carOk = await _db.SimCars.AnyAsync(c => c.Id == model.CarId.Value && c.Sim == model.Sim.Value);
                if (!carOk) ModelState.AddModelError(nameof(model.CarId), "Ungültiges Auto für diese Simulation.");
            }
            if (model.TrackId.HasValue)
            {
                var trackOk = await _db.SimTracks.AnyAsync(t => t.Id == model.TrackId.Value && t.Sim == model.Sim.Value);
                if (!trackOk) ModelState.AddModelError(nameof(model.TrackId), "Ungültige Strecke für diese Simulation.");
            }
        }

        private static List<SelectListItem> SimSelectList(SimGame? selected) =>
            Enum.GetValues<SimGame>()
                .Select(s => new SelectListItem(SimGameInfo.DisplayName(s), ((int)s).ToString(), selected.HasValue && selected.Value == s))
                .ToList();

        private async Task<List<SelectListItem>> CarSelectListAsync(SimGame? sim, int? selected)
        {
            if (!sim.HasValue) return new List<SelectListItem>();
            return await _db.SimCars
                .Where(c => c.Sim == sim.Value)
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem(c.Name, c.Id.ToString(), selected.HasValue && selected.Value == c.Id))
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> TrackSelectListAsync(SimGame? sim, int? selected)
        {
            if (!sim.HasValue) return new List<SelectListItem>();
            return await _db.SimTracks
                .Where(t => t.Sim == sim.Value)
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem(t.Name, t.Id.ToString(), selected.HasValue && selected.Value == t.Id))
                .ToListAsync();
        }
    }
}
