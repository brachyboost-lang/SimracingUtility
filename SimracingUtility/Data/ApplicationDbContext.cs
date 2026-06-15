using Microsoft.AspNetCore.Identity;
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

        public DbSet<RecentFuelCalculation> RecentFuelCalculations { get; set; }
        public DbSet<Setup> Setups { get; set; }
        public DbSet<SimCar> SimCars { get; set; }
        public DbSet<SimTrack> SimTracks { get; set; }
        public DbSet<LmuDriverStats> LmuDriverStats { get; set; }
        public DbSet<LmuCompanion> LmuCompanions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Bestehende Fuel-Calc-Tabelle.
            modelBuilder.Entity<RecentFuelCalculation>(b =>
            {
                b.ToTable("FuelCalc");
                b.HasKey(x => x.Id);
                b.Property(x => x.TrackName).IsRequired();
                b.Property(x => x.CarName).IsRequired();
                b.Property(x => x.CarClass).IsRequired();
            });

            modelBuilder.Entity<SimCar>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Slug).IsRequired().HasMaxLength(80);
                b.Property(x => x.Name).IsRequired().HasMaxLength(150);
                b.HasIndex(x => new { x.Sim, x.Slug }).IsUnique();
            });

            modelBuilder.Entity<SimTrack>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Slug).IsRequired().HasMaxLength(80);
                b.Property(x => x.Name).IsRequired().HasMaxLength(150);
                b.HasIndex(x => new { x.Sim, x.Slug }).IsUnique();
            });

            modelBuilder.Entity<Setup>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.FileName).IsRequired().HasMaxLength(260);
                b.Property(x => x.ContentType).HasMaxLength(150);

                b.HasOne(x => x.Owner)
                    .WithMany()
                    .HasForeignKey(x => x.OwnerId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.Car)
                    .WithMany(c => c.Setups)
                    .HasForeignKey(x => x.CarId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.Track)
                    .WithMany(t => t.Setups)
                    .HasForeignKey(x => x.TrackId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => new { x.Sim, x.CarId, x.TrackId });
                b.HasIndex(x => x.OwnerId);
            });

            // Vom LMU-Agent gepushte Statistiken.
            modelBuilder.Entity<LmuDriverStats>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.DriverName).IsRequired().HasMaxLength(150);
                b.HasIndex(x => x.DriverName).IsUnique();

                b.HasMany(x => x.Companions)
                    .WithOne(c => c.DriverStats)
                    .HasForeignKey(c => c.LmuDriverStatsId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<LmuCompanion>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired().HasMaxLength(150);
                b.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20);
            });
        }
    }
}
