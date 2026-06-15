using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimracingUtility.Data;
using SimracingUtility.Models;

namespace SimracingUtility.Controllers
{
    // Nimmt die vom LMU-Agent gepushten Statistiken entgegen (REST → PostgreSQL).
    [ApiController]
    [Route("api/lmu")]
    public class LmuStatsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;

        public LmuStatsApiController(ApplicationDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpPost("stats")]
        public async Task<IActionResult> Push([FromBody] LmuStatsPushDto? dto)
        {
            var expectedKey = _config["Lmu:IngestApiKey"];
            if (!string.IsNullOrEmpty(expectedKey))
            {
                if (!Request.Headers.TryGetValue("X-Api-Key", out var provided)
                    || provided != expectedKey)
                {
                    return Unauthorized();
                }
            }

            if (dto == null || string.IsNullOrWhiteSpace(dto.DriverName))
            {
                return BadRequest("DriverName fehlt.");
            }

            // Optionaler Host-Nutzer-Identifier (für die Zuordnung im einbindenden
            // System). Fehlt er, wird – wie im Einzelnutzer-/Dev-Modus – über den
            // Fahrernamen zugeordnet.
            var ownerKey = Request.Headers.TryGetValue("X-User-Key", out var ok)
                ? ok.ToString().Trim()
                : string.Empty;

            // Upsert: per OwnerKey, falls vorhanden, sonst per Fahrername.
            var driver = await (string.IsNullOrEmpty(ownerKey)
                ? _db.LmuDrivers.Include(d => d.Categories).Include(d => d.TrackBests).Include(d => d.RacedWith)
                    .FirstOrDefaultAsync(d => d.OwnerKey == "" && d.DriverName == dto.DriverName)
                : _db.LmuDrivers.Include(d => d.Categories).Include(d => d.TrackBests).Include(d => d.RacedWith)
                    .FirstOrDefaultAsync(d => d.OwnerKey == ownerKey));

            if (driver == null)
            {
                driver = new LmuDriver { DriverName = dto.DriverName, OwnerKey = ownerKey };
                _db.LmuDrivers.Add(driver);
            }
            else
            {
                driver.DriverName = dto.DriverName;
                _db.LmuCategoryStats.RemoveRange(driver.Categories);
                _db.LmuTrackBests.RemoveRange(driver.TrackBests);
                _db.LmuRacedWith.RemoveRange(driver.RacedWith);
                driver.Categories.Clear();
                driver.TrackBests.Clear();
                driver.RacedWith.Clear();
            }

            driver.UpdatedAt = DateTime.UtcNow;
            driver.Categories.Add(ToCategory("Sprint", dto.Sprint));
            driver.Categories.Add(ToCategory("Endurance", dto.Endurance));

            foreach (var t in dto.BestLapsByTrack)
            {
                driver.TrackBests.Add(new LmuTrackBest { Track = t.Track, BestLapTime = t.BestLapTime });
            }
            foreach (var r in dto.MostRacedWith)
            {
                driver.RacedWith.Add(new LmuRacedWith { Name = r.Name, RacesShared = r.RacesShared, Kind = "Driver" });
            }
            foreach (var t in dto.MostRacedAgainstTeams)
            {
                driver.RacedWith.Add(new LmuRacedWith { Name = t.Name, RacesShared = t.RacesShared, Kind = "Team" });
            }

            await _db.SaveChangesAsync();
            return Ok(new
            {
                driver = driver.DriverName,
                tracks = driver.TrackBests.Count,
                racedWith = driver.RacedWith.Count
            });
        }

        /// <summary>
        /// Liest die Statistik als JSON. Auswahl per <c>?owner=</c> (Host-Nutzer)
        /// oder <c>?driver=</c>; ohne Parameter der zuletzt aktualisierte Fahrer.
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> Get([FromQuery] string? owner, [FromQuery] string? driver)
        {
            var q = _db.LmuDrivers
                .Include(d => d.Categories)
                .Include(d => d.TrackBests)
                .Include(d => d.RacedWith)
                .AsQueryable();

            LmuDriver? d2;
            if (!string.IsNullOrWhiteSpace(owner))
                d2 = await q.FirstOrDefaultAsync(x => x.OwnerKey == owner);
            else if (!string.IsNullOrWhiteSpace(driver))
                d2 = await q.FirstOrDefaultAsync(x => x.DriverName == driver);
            else
                d2 = await q.OrderByDescending(x => x.UpdatedAt).FirstOrDefaultAsync();

            return d2 == null ? NotFound() : Ok(ToResponse(d2));
        }

        private static LmuStatsResponseDto ToResponse(LmuDriver d)
        {
            CategoryStatsDto Cat(string name)
            {
                var c = d.Categories.FirstOrDefault(x => x.Category == name);
                return c == null ? new CategoryStatsDto() : new CategoryStatsDto
                {
                    TotalRaces = c.TotalRaces, Wins = c.Wins, Podiums = c.Podiums,
                    Top5 = c.Top5, Top10 = c.Top10, TopHalf = c.TopHalf, Dnf = c.Dnf,
                    BestPosition = c.BestPosition, LastRaceDate = c.LastRaceDate,
                };
            }

            List<RacedWithDto> Companions(string kind) => d.RacedWith
                .Where(r => r.Kind == kind)
                .OrderByDescending(r => r.RacesShared)
                .Select(r => new RacedWithDto { Name = r.Name, RacesShared = r.RacesShared })
                .ToList();

            return new LmuStatsResponseDto
            {
                OwnerKey = d.OwnerKey,
                DriverName = d.DriverName,
                UpdatedAt = d.UpdatedAt,
                Sprint = Cat("Sprint"),
                Endurance = Cat("Endurance"),
                BestLapsByTrack = d.TrackBests
                    .OrderBy(t => t.Track)
                    .Select(t => new TrackBestDto { Track = t.Track, BestLapTime = t.BestLapTime })
                    .ToList(),
                MostRacedWith = Companions("Driver"),
                MostRacedAgainstTeams = Companions("Team"),
            };
        }

        private static LmuCategoryStat ToCategory(string category, CategoryStatsDto s)
            => new()
            {
                Category = category,
                TotalRaces = s.TotalRaces,
                Wins = s.Wins,
                Podiums = s.Podiums,
                Top5 = s.Top5,
                Top10 = s.Top10,
                TopHalf = s.TopHalf,
                Dnf = s.Dnf,
                BestPosition = s.BestPosition,
                // Npgsql speichert timestamptz als UTC – eingehenden Wert als UTC behandeln.
                LastRaceDate = DateTime.SpecifyKind(s.LastRaceDate, DateTimeKind.Utc),
            };
    }
}
