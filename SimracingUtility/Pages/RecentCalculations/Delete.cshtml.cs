using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using SimracingUtility.Services;

namespace SimracingUtility.Pages.RecentCalculations
{
    public class DeleteModel : PageModel
    {
        private readonly IRecentFuelCalcService _service;
        public DeleteModel(IRecentFuelCalcService service) => _service = service;

        public Models.RecentFuelCalculation? Calculation { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Calculation = await _service.GetByIdAsync(id);
            if (Calculation == null) return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            await _service.DeleteAsync(id);
            return RedirectToPage("Index");
        }
    }
}