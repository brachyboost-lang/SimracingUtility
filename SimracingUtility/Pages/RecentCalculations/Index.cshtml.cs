using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;
using SimracingUtility.Models;
using SimracingUtility.Services;

namespace SimracingUtility.Pages.RecentCalculations
{
    public class IndexModel : PageModel
    {
        private readonly IRecentFuelCalcService _service;
        public IndexModel(IRecentFuelCalcService service) => _service = service;

        public List<RecentFuelCalculation> Recent { get; set; } = new();

        public async Task OnGetAsync()
        {
            Recent = await _service.GetRecentAsync(50);
        }
    }
}