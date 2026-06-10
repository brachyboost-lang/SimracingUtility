using Microsoft.AspNetCore.Mvc;
using SimracingUtility.Data;

namespace SimracingUtility.Controllers
{
    public class FuelCalcController : Controller
    {
        private readonly ApplicationDbContext _context;
        public FuelCalcController(ApplicationDbContext context) 
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var fuelCalcs = _context.FuelCalc.ToList();
            ViewBag.RecentFuelCalcs = fuelCalcs;
            return View();
        }
    }
}
