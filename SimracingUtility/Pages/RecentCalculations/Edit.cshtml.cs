using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using SimracingUtility.Models;
using SimracingUtility.Services;

namespace SimracingUtility.Pages.RecentCalculations
{
    public class EditModel : PageModel
    {
        private readonly IRecentFuelCalcService _service;
        public EditModel(IRecentFuelCalcService service) => _service = service;

        [BindProperty]
        public RecentFuelCalculation Calculation { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var e = await _service.GetByIdAsync(id);
            if (e == null) return NotFound();
            Calculation = e;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            await _service.UpdateAsync(Calculation);
            return RedirectToPage("Index");
        }
    }
}