using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SimracingUtility.Data;
using SimracingUtility.Models;

namespace SimracingUtility.Services
{
    public class RecentFuelCalcService : IRecentFuelCalcService
    {
        private readonly ApplicationDbContext _db;

        public RecentFuelCalcService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<RecentFuelCalculation?> GetByIdAsync(int id)
        {
            return await _db.RecentFuelCalculations.FindAsync(id);
        }

        public async Task<List<RecentFuelCalculation>> GetRecentAsync(int max = 20)
        {
            return await _db.RecentFuelCalculations.OrderByDescending(x => x.CreatedAt).Take(max).ToListAsync();
        }

        public async Task<RecentFuelCalculation> CreateAsync(RecentFuelCalculation entity)
        {
            _db.RecentFuelCalculations.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(RecentFuelCalculation entity)
        {
            entity.UpdatedAt = System.DateTime.UtcNow;
            _db.RecentFuelCalculations.Update(entity);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var e = await _db.RecentFuelCalculations.FindAsync(id);
            if (e != null)
            {
                _db.RecentFuelCalculations.Remove(e);
                await _db.SaveChangesAsync();
            }
        }
    }
}