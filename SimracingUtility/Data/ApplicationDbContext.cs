using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SimracingUtility.Models;

namespace SimracingUtility.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<FuelCalcViewModel> FuelCalc { get; set; } 
    }
}
