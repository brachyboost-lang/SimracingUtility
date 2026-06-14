using System.Text.Json;
using SimracingUtility.Models;

namespace SimracingUtility.Data
{
    /// <summary>
    /// Liest wwwroot/data/sim_data.json und legt fehlende Autos/Strecken an.
    /// Idempotent: vorhandene Einträge (anhand Sim + Slug) werden nicht dupliziert.
    /// </summary>
    public static class SimDataSeeder
    {
        private static readonly Dictionary<string, SimGame> SimMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["iracing"] = SimGame.iRacing,
            ["lmu"] = SimGame.LMU,
            ["acc"] = SimGame.ACC
        };

        public static void Seed(ApplicationDbContext db, IWebHostEnvironment env, ILogger? logger = null)
        {
            var path = Path.Combine(env.WebRootPath ?? string.Empty, "data", "sim_data.json");
            if (!File.Exists(path))
            {
                logger?.LogWarning("sim_data.json nicht gefunden unter {Path} – Seeding übersprungen.", path);
                return;
            }

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("simulations", out var sims) || sims.ValueKind != JsonValueKind.Array)
                return;

            var existingCars = db.SimCars
                .Select(c => new { c.Sim, c.Slug })
                .ToHashSet();
            var existingTracks = db.SimTracks
                .Select(t => new { t.Sim, t.Slug })
                .ToHashSet();

            int addedCars = 0, addedTracks = 0;

            foreach (var simEl in sims.EnumerateArray())
            {
                if (!simEl.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.String)
                    continue;
                if (!SimMap.TryGetValue(idEl.GetString() ?? string.Empty, out var sim))
                    continue;

                if (simEl.TryGetProperty("cars", out var cars) && cars.ValueKind == JsonValueKind.Array)
                {
                    foreach (var (slug, name) in EnumerateNamed(cars))
                    {
                        if (existingCars.Contains(new { Sim = sim, Slug = slug })) continue;
                        db.SimCars.Add(new SimCar { Sim = sim, Slug = slug, Name = name });
                        existingCars.Add(new { Sim = sim, Slug = slug });
                        addedCars++;
                    }
                }

                if (simEl.TryGetProperty("tracks", out var tracks) && tracks.ValueKind == JsonValueKind.Array)
                {
                    foreach (var (slug, name) in EnumerateNamed(tracks))
                    {
                        if (existingTracks.Contains(new { Sim = sim, Slug = slug })) continue;
                        db.SimTracks.Add(new SimTrack { Sim = sim, Slug = slug, Name = name });
                        existingTracks.Add(new { Sim = sim, Slug = slug });
                        addedTracks++;
                    }
                }
            }

            if (addedCars > 0 || addedTracks > 0)
            {
                db.SaveChanges();
                logger?.LogInformation("Seeding: {Cars} Autos, {Tracks} Strecken hinzugefügt.", addedCars, addedTracks);
            }
        }

        private static IEnumerable<(string slug, string name)> EnumerateNamed(JsonElement array)
        {
            foreach (var item in array.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;
                var slug = item.TryGetProperty("id", out var i) && i.ValueKind == JsonValueKind.String ? i.GetString() : null;
                var name = item.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() : null;
                if (!string.IsNullOrWhiteSpace(slug) && !string.IsNullOrWhiteSpace(name))
                    yield return (slug!, name!);
            }
        }
    }
}
