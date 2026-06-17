using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using SimracingUtility.Models;

namespace SimracingUtility.Services
{
    /// <summary>
    /// Holt das öffentliche SimGrid-Profil (mit Browser-Kennung, da SimGrid einfache
    /// Bots per 403 blockt) und liest daraus per <see cref="SimGridStatsParser"/> die
    /// Renn-Kennzahlen. Aggressives Caching, damit nicht bei jedem Seitenaufruf
    /// gescraped wird; Fehler werden geschluckt (best effort – kein Stat-Block statt
    /// Fehlerseite). Bewusst fragil/ToS-grau: nur das selbst angegebene eigene Profil.
    /// </summary>
    public class SimGridClient
    {
        private readonly HttpClient _http;
        private readonly IMemoryCache _cache;

        public SimGridClient(HttpClient http, IMemoryCache cache)
        {
            _http = http;
            _cache = cache;
        }

        public async Task<SimGridStats?> GetStatsAsync(string? profileUrl, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(profileUrl)) return null;

            var key = "simgrid-stats:" + profileUrl;
            if (_cache.TryGetValue(key, out SimGridStats? cached)) return cached;

            SimGridStats? result = null;
            try
            {
                using var resp = await _http.GetAsync(profileUrl, ct);
                if (resp.IsSuccessStatusCode)
                {
                    var html = await resp.Content.ReadAsStringAsync(ct);
                    var stats = SimGridStatsParser.Parse(html);
                    result = stats.HasAny ? stats : null;
                }
            }
            catch
            {
                // Netzwerk-/Timeout-/Parserfehler: bewusst schlucken.
            }

            // Erfolg deutlich länger cachen als einen Fehlschlag.
            _cache.Set(key, result, result != null ? TimeSpan.FromHours(6) : TimeSpan.FromMinutes(30));
            return result;
        }
    }
}
