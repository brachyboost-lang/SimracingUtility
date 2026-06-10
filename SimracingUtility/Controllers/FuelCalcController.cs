using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using SimracingUtility.Data;
using SimracingUtility.Models;
using System.Text.Json;
using System.IO;
using System.Linq;

namespace SimracingUtility.Controllers
{
    public class FuelCalcController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public FuelCalcController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public IActionResult Index()
        {
            var fuelCalcs = _context.FuelCalc.ToList();
            ViewBag.RecentFuelCalcs = fuelCalcs;

            // Load cars and classes from JSON (wwwroot/data/cars.json)
            try
            {
                var jsonPath = Path.Combine(_env.WebRootPath ?? string.Empty, "data", "cars.json");
                if (System.IO.File.Exists(jsonPath))
                {
                    var json = System.IO.File.ReadAllText(jsonPath);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    // Read classes
                    List<string> classes = new();
                    if (root.TryGetProperty("carClasses", out var classesElement) && classesElement.ValueKind == JsonValueKind.Array)
                    {
                        classes = classesElement.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString() ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();
                    }

                    // Read cars
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

                    ViewBag.CarClasses = new SelectList(classes);
                    ViewBag.CarNames = new SelectList(cars);
                }
                else
                {
                    ViewBag.CarClasses = new SelectList(Enumerable.Empty<string>());
                    ViewBag.CarNames = new SelectList(Enumerable.Empty<string>());
                }
            }
            catch
            {
                ViewBag.CarClasses = new SelectList(Enumerable.Empty<string>());
                ViewBag.CarNames = new SelectList(Enumerable.Empty<string>());
            }

            var model = new FuelCalcViewModel();
            return View(model);
        }
    }
}
