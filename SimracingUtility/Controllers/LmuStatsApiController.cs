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

            // Upsert pro Fahrername; alle Kind-Datensätze werden ersetzt.
            var driver = await _db.LmuDrivers
                .Include(d => d.Categories)
                .Include(d => d.TrackBests)
                .Include(d => d.RacedWith)
                .FirstOrDefaultAsync(d => d.DriverName == dto.DriverName);

            if (driver == null)
            {
                driver = new LmuDriver { DriverName = dto.DriverName };
                _db.LmuDrivers.Add(driver);
            }
            else
            {
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
