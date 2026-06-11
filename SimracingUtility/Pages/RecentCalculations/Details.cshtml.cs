using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using SimracingUtility.Services;
using SimracingUtility.Models;

namespace SimracingUtility.Pages.RecentCalculations
{
    public class DetailsModel : PageModel
    {
        private readonly IRecentFuelCalcService _service;
        public DetailsModel(IRecentFuelCalcService service) => _service = service;

        public RecentFuelCalculation? Calculation { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Calculation = await _service.GetByIdAsync(id);
            if (Calculation == null) return NotFound();
            return Page();
        }
    }
}