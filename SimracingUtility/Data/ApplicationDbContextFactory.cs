using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SimracingUtility.Data
{
    /// <summary>
    /// Design-Time-Factory für die EF-Core-Tools (z. B. "dotnet ef migrations add").
    /// Verhindert, dass die Tools die App-Pipeline (inkl. Database.Migrate beim
    /// Start) ausführen und dafür eine erreichbare Datenbank bräuchten.
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var connectionString = config.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Port=5432;Database=SimracingUtility;Username=postgres;Password=postgres";

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            return new ApplicationDbContext(options);
        }
    }
}
