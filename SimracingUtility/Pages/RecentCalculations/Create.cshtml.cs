using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using SimracingUtility.Models;
using SimracingUtility.Services;

namespace SimracingUtility.Pages.RecentCalculations
{
    public class CreateModel : PageModel
    {
        private readonly IRecentFuelCalcService _service;
        public CreateModel(IRecentFuelCalcService service) => _service = service;

        [BindProperty]
        public RecentFuelCalculation Calculation { get; set; } = new();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();
            await _service.CreateAsync(Calculation);
            return RedirectToPage("Index");
        }
    }
}