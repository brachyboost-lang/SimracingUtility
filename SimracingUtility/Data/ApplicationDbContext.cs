using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SimracingUtility.Models;

namespace SimracingUtility.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<FuelCalcViewModel> FuelCalc { get; set; }
        public DbSet<Models.RecentFuelCalculation> RecentFuelCalculations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Map the RecentFuelCalculation entity to the existing "FuelCalc" table so existing data remains accessible.
            modelBuilder.Entity<Models.RecentFuelCalculation>(b =>
            {
                b.ToTable("FuelCalc");
                b.HasKey(x => x.Id);
                b.Property(x => x.TrackName).IsRequired();
                b.Property(x => x.CarName).IsRequired();
                b.Property(x => x.CarClass).IsRequired();
                b.Property(x => x.RowVersion).IsRowVersion();
            });
        }
    }
}
