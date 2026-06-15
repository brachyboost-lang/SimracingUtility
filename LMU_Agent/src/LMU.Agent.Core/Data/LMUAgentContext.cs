using Microsoft.EntityFrameworkCore;
using LMU.Agent.Core.Models;

namespace LMU.Agent.Core.Data;

public class LMUAgentContext : DbContext
{
    public DbSet<Event> Events { get; set; }
    public DbSet<RaceResult> RaceResults { get; set; }
    public DbSet<DriverProfile> DriverProfiles { get; set; }
    public DbSet<Statistics> Statistics { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Fester, gemeinsamer Speicherort, damit Dienst und Web-API unabhängig
            // vom Arbeitsverzeichnis dieselbe Datenbank verwenden.
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LMUAgent");
            Directory.CreateDirectory(dir);
            var dbPath = Path.Combine(dir, "lmu_agent.db");

            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Date).IsRequired();
        });

        modelBuilder.Entity<RaceResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DriverName).IsRequired();
            entity.Property(e => e.Position).IsRequired();
            entity.Ignore(e => e.IsDnf); // berechnete Eigenschaft, keine Spalte
        });

        modelBuilder.Entity<DriverProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<Statistics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DriverName).IsRequired();
        });
    }
}