using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SimracingUtility.Data;
using System.IO;

namespace SimracingUtility
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString)
                    // Laufzeit-False-Positive von EF 10 unterdrücken: das Tooling
                    // (dotnet ef) bestätigt, dass keine Modelländerungen offen sind.
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
                .AddEntityFrameworkStores<ApplicationDbContext>();
            builder.Services.AddScoped<Services.IRecentFuelCalcService, Services.RecentFuelCalcService>();

            // SimGrid-Profil-Stats (best-effort-Scraping): gecachter HTTP-Client mit
            // Browser-Kennung und kurzem Timeout, damit ein langsames/fehlendes
            // SimGrid die Stats-Seite nicht blockiert.
            builder.Services.AddMemoryCache();
            builder.Services.AddHttpClient<Services.SimGridClient>(c =>
            {
                c.Timeout = TimeSpan.FromSeconds(8);
                c.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
            });

            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Datenbank migrieren und Stammdaten (Autos/Strecken) seeden.
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var db = services.GetRequiredService<ApplicationDbContext>();
                db.Database.Migrate();
                SimDataSeeder.Seed(db, app.Environment, services.GetService<ILoggerFactory>()?.CreateLogger("SimDataSeeder"));
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();



            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            app.Run();
        }
    }
}
