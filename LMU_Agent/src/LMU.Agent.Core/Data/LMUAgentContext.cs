using Microsoft.EntityFrameworkCore;
using LMU.Agent.Core.Models;

namespace LMU.Agent.Core.Data;

public class LMUAgentContext : DbContext
{
    // Bei jeder schemaverändernden Modelländerung erhöhen. Die SQLite-DB ist nur
    // ein aus den XML-Dateien reproduzierbarer Cache – bei abweichender Version
    // wird sie verworfen und neu aufgebaut (Ersatz für Migrationen).
    public const int SchemaVersion = 1;

    public DbSet<Event> Events { get; set; }
    public DbSet<RaceResult> RaceResults { get; set; }
    public DbSet<DriverProfile> DriverProfiles { get; set; }
    public DbSet<Statistics> Statistics { get; set; }

    /// <summary>Fester, gemeinsamer Speicherort der SQLite-Datei.</summary>
    public static string DbDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LMUAgent");

    public static string DbPath => Path.Combine(DbDirectory, "lmu_agent.db");

    /// <summary>
    /// Verwirft die DB, wenn die gespeicherte Schema-Version abweicht (z. B. nach
    /// einem Agent-Update mit neuen Spalten), und schreibt die aktuelle Version.
    /// Muss vor <c>EnsureCreated</c> aufgerufen werden.
    /// </summary>
    public static void PrepareDatabaseFile()
    {
        Directory.CreateDirectory(DbDirectory);
        var versionFile = DbPath + ".schema";
        var stored = File.Exists(versionFile) ? File.ReadAllText(versionFile).Trim() : null;

        if (File.Exists(DbPath) && stored != SchemaVersion.ToString())
        {
            foreach (var f in new[] { DbPath, DbPath + "-wal", DbPath + "-shm" })
                if (File.Exists(f)) File.Delete(f);
        }
        File.WriteAllText(versionFile, SchemaVersion.ToString());
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            Directory.CreateDirectory(DbDirectory);
            optionsBuilder.UseSqlite($"Data Source={DbPath}");
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
            entity.Ignore(e => e.IsDnf);        // berechnete Eigenschaft, keine Spalte
            entity.Ignore(e => e.CarKey);       // berechnete Eigenschaft, keine Spalte
            entity.Ignore(e => e.IsEndurance);  // berechnete Eigenschaft, keine Spalte
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