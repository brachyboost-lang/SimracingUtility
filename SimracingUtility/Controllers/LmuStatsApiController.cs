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
            // API-Key-Prüfung (geteiltes Geheimnis aus der Konfiguration).
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

            // Upsert pro Fahrername; Companions werden komplett ersetzt.
            var entity = await _db.LmuDriverStats
                .Include(s => s.Companions)
                .FirstOrDefaultAsync(s => s.DriverName == dto.DriverName);

            if (entity == null)
            {
                entity = new LmuDriverStats { DriverName = dto.DriverName };
                _db.LmuDriverStats.Add(entity);
            }
            else
            {
                _db.LmuCompanions.RemoveRange(entity.Companions);
                entity.Companions.Clear();
            }

            entity.TotalRaces = dto.Stats.TotalRaces;
            entity.Wins = dto.Stats.Wins;
            entity.Podiums = dto.Stats.Podiums;
            entity.Top5 = dto.Stats.Top5;
            entity.Top10 = dto.Stats.Top10;
            entity.TopHalf = dto.Stats.TopHalf;
            entity.Dnf = dto.Stats.Dnf;
            entity.BestPosition = dto.Stats.BestPosition;
            entity.FastestLapTime = dto.Stats.FastestLapTime;
            // Npgsql speichert timestamptz als UTC – eingehenden Wert als UTC behandeln.
            entity.LastRaceDate = DateTime.SpecifyKind(dto.Stats.LastRaceDate, DateTimeKind.Utc);
            entity.UpdatedAt = DateTime.UtcNow;

            foreach (var t in dto.Teammates)
            {
                entity.Companions.Add(new LmuCompanion
                {
                    Name = t.Name,
                    RacesShared = t.RacesShared,
                    Kind = CompanionKind.Teammate
                });
            }
            foreach (var o in dto.Opponents)
            {
                entity.Companions.Add(new LmuCompanion
                {
                    Name = o.Name,
                    RacesShared = o.RacesShared,
                    Kind = CompanionKind.Opponent
                });
            }

            await _db.SaveChangesAsync();
            return Ok(new { driver = entity.DriverName, companions = entity.Companions.Count });
        }
    }
}
