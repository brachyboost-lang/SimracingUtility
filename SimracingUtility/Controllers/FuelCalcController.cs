using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using SimracingUtility.Data;
using SimracingUtility.Models;
using SimracingUtility.Services;
using System.Text.Json;
using System.IO;
using System.Linq;

namespace SimracingUtility.Controllers
{
    public class FuelCalcController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IRecentFuelCalcService _recentService;

        public FuelCalcController(ApplicationDbContext context, IWebHostEnvironment env, IRecentFuelCalcService recentService)
        {
            _context = context;
            _env = env;
            _recentService = recentService;
        }

        private void PopulateRecentCalcs()
        {
            ViewBag.RecentFuelCalcs = _recentService.GetRecentAsync(20).GetAwaiter().GetResult();
        }

        private void LoadCarsIntoViewBag()
        {
            try
            {
                // Try multiple locations to locate wwwroot/data/cars.json
                var candidates = new[] {
                    Path.Combine(_env.WebRootPath ?? string.Empty, "data", "cars.json"),
                    Path.Combine(_env.ContentRootPath ?? string.Empty, "wwwroot", "data", "cars.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "cars.json")
                };

                string? jsonPath = candidates.FirstOrDefault(p => !string.IsNullOrEmpty(p) && System.IO.File.Exists(p));
                if (!string.IsNullOrEmpty(jsonPath))
                {
                    var json = System.IO.File.ReadAllText(jsonPath);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    List<string> classes = new();
                    if (root.TryGetProperty("carClasses", out var classesElement) && classesElement.ValueKind == JsonValueKind.Array)
                    {
                        classes = classesElement.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString() ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();
                    }

                    List<string> cars = new();
                    if (root.TryGetProperty("cars", out var carsElement) && carsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in carsElement.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String)
                            {
                                var name = nameProp.GetString() ?? string.Empty;
                                if (!string.IsNullOrEmpty(name)) cars.Add(name);
                            }
                        }
                        cars = cars.Distinct().ToList();
                    }

                    ViewBag.CarClasses = classes.Select(c => new SelectListItem(c, c)).ToList();
                    ViewBag.CarNames = cars.Select(c => new SelectListItem(c, c)).ToList();

                    var carObjects = new List<object>();
                    if (root.TryGetProperty("cars", out var carsEl) && carsEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in carsEl.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.Object)
                            {
                                string name = item.TryGetProperty("name", out var np) && np.ValueKind == JsonValueKind.String ? np.GetString() ?? string.Empty : string.Empty;
                                string cls = item.TryGetProperty("class", out var cp) && cp.ValueKind == JsonValueKind.String ? cp.GetString() ?? string.Empty : string.Empty;
                                if (!string.IsNullOrEmpty(name)) carObjects.Add(new { name, @class = cls });
                            }
                        }
                    }
                    ViewBag.CarsJson = JsonSerializer.Serialize(carObjects);
                }
                else
                {
                    ViewBag.CarClasses = Enumerable.Empty<SelectListItem>();
                    ViewBag.CarNames = Enumerable.Empty<SelectListItem>();
                }
            }
            catch
            {
                ViewBag.CarClasses = Enumerable.Empty<SelectListItem>();
                ViewBag.CarNames = Enumerable.Empty<SelectListItem>();
            }
        }

        public IActionResult Index()
        {
            PopulateRecentCalcs();
            LoadCarsIntoViewBag();

            var model = new FuelCalcViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(FuelCalcViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Perform calculation server-side
                model.CalculateFuel();

                // Persist calculation (map view model -> entity)
                var entity = new RecentFuelCalculation
                {
                    EventDurationMinutes = model.EventDurationMinutes,
                    TrackName = model.TrackName,
                    PitBoxTime = model.PitBoxTime,
                    FuelPerLap = model.FuelPerLap,
                    DriveThroughTime = model.DriveThroughTime,
                    CarName = model.CarName,
                    CarClass = model.CarClass,
                    FuelTankCapacity = model.FuelTankCapacity,
                    TimePerLap = model.TimePerLap
                };
                model.CalculateFuel();
                entity.NumberOfPitStops = model.NumberOfPitStops;
                entity.TotalFuelNeeded = model.TotalFuelNeeded;
                entity.Laps = model.Laps;
                entity.TotalTimeLost = model.TotalTimeLost;

                _recentService.CreateAsync(entity).GetAwaiter().GetResult();
            }
            PopulateRecentCalcs();
            LoadCarsIntoViewBag();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Calculate([FromBody] FuelCalcViewModel model)
        {
            if (model == null)
                return BadRequest();

            // perform calculation
            model.CalculateFuel();

            // map to entity and persist
            var entity = new RecentFuelCalculation
            {
                EventDurationMinutes = model.EventDurationMinutes,
                TrackName = model.TrackName,
                PitBoxTime = model.PitBoxTime,
                FuelPerLap = model.FuelPerLap,
                DriveThroughTime = model.DriveThroughTime,
                CarName = model.CarName,
                CarClass = model.CarClass,
                FuelTankCapacity = model.FuelTankCapacity,
                TimePerLap = model.TimePerLap,
                NumberOfPitStops = model.NumberOfPitStops,
                TotalFuelNeeded = model.TotalFuelNeeded,
                Laps = model.Laps,
                TotalTimeLost = model.TotalTimeLost
            };

            _recentService.CreateAsync(entity).GetAwaiter().GetResult();

            // return minimal data for client update
            return Json(new
            {
                id = entity.Id,
                numberOfPitStops = entity.NumberOfPitStops,
                laps = entity.Laps,
                totalFuelNeeded = entity.TotalFuelNeeded,
                trackName = entity.TrackName,
                eventDurationMinutes = entity.EventDurationMinutes,
                carName = entity.CarName,
                carClass = entity.CarClass,
                fuelPerLap = entity.FuelPerLap
            });
        }


    }
}
