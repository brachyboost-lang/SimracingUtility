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
        // Konfiguration für SQLite (lokale Datei-Datenbank)
        var connectionString = "Data Source=lmu_agent.db";
        
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite(connectionString);
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
            entity.Property(e => e.Time).IsRequired();
            entity.Property(e => e.Position).IsRequired();
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