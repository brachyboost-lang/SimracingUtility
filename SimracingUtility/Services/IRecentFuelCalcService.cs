using System.Collections.Generic;
using SimracingUtility.Models;
using System.Threading.Tasks;

namespace SimracingUtility.Services
{
    public interface IRecentFuelCalcService
    {
        Task<RecentFuelCalculation?> GetByIdAsync(int id);
        Task<List<RecentFuelCalculation>> GetRecentAsync(int max = 20);
        Task<RecentFuelCalculation> CreateAsync(RecentFuelCalculation entity);
        Task UpdateAsync(RecentFuelCalculation entity);
        Task DeleteAsync(int id);
    }
}