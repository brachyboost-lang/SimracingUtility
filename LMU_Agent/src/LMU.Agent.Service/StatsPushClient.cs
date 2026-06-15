using System.Net.Http.Json;
using LMU.Agent.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LMU.Agent.Service;

/// <summary>
/// Sendet das berechnete Nutzer-Dashboard an die SimracingUtility-Website
/// (REST-Push). Ziel-URL und API-Schlüssel kommen aus der Konfiguration
/// (Abschnitt "Website"). Ist keine BaseUrl gesetzt, wird der Push übersprungen.
/// </summary>
public class StatsPushClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<StatsPushClient> _logger;

    public StatsPushClient(HttpClient http, IConfiguration config, ILogger<StatsPushClient> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task PushAsync(UserDashboard dashboard, CancellationToken ct)
    {
        var baseUrl = _config["Website:BaseUrl"];
        var apiKey = _config["Website:ApiKey"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogInformation(
                "Kein Website:BaseUrl konfiguriert – Push übersprungen.");
            return;
        }

        var url = $"{baseUrl.TrimEnd('/')}/api/lmu/stats";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(dashboard)
            };
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                request.Headers.Add("X-Api-Key", apiKey);
            }

            using var response = await _http.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Stats an {Url} gepusht (Fahrer: {Driver}).",
                    url, dashboard.DriverName);
            }
            else
            {
                _logger.LogWarning("Push an {Url} fehlgeschlagen: {Status}",
                    url, (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Push an {Url}", url);
        }
    }
}
