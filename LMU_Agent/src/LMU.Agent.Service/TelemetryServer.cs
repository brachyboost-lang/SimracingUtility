using System.IO.Compression;
using System.Net;
using System.Text;
using LMU.Agent.Core.Services;
using Microsoft.Extensions.Logging;

namespace LMU.Agent.Service;

/// <summary>
/// Kleiner lokaler HTTP-Server, der die Telemetrie einer Strecke als ZIP
/// (.ld + .ldx zusammen) ausliefert. Wird von der Website-Statistikseite per
/// Download-Link angesprochen (läuft auf dem Rechner des Nutzers).
/// Endpunkt: <c>GET /telemetry?track=&lt;Strecke&gt;</c>.
/// </summary>
public sealed class TelemetryServer : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly string _resultsPath;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _cts = new();

    public TelemetryServer(int port, string resultsPath, ILogger logger)
    {
        _resultsPath = resultsPath;
        _logger = logger;
        _listener.Prefixes.Add($"http://localhost:{port}/");
    }

    public void Start()
    {
        try
        {
            _listener.Start();
            _ = Task.Run(LoopAsync);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telemetrie-Server konnte nicht starten.");
        }
    }

    private async Task LoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try { ctx = await _listener.GetContextAsync(); }
            catch { break; } // Listener gestoppt

            try { Handle(ctx); }
            catch (Exception ex) { _logger.LogError(ex, "Telemetrie-Anfrage fehlgeschlagen."); }
        }
    }

    private void Handle(HttpListenerContext ctx)
    {
        var res = ctx.Response;
        // Direktaufruf per Link von der Website erlauben.
        res.AddHeader("Access-Control-Allow-Origin", "*");

        if (!string.Equals(ctx.Request.Url?.AbsolutePath, "/telemetry", StringComparison.OrdinalIgnoreCase))
        {
            res.StatusCode = 404;
            res.Close();
            return;
        }

        // Query selbst UTF-8-sicher parsen (HttpListener.QueryString decodiert
        // %C3%B3 & Co. nicht zuverlässig als UTF-8).
        var track = GetQueryValue(ctx.Request.Url?.Query, "track");
        var all = GetQueryValue(ctx.Request.Url?.Query, "all") is "1" or "true";
        var logFolder = TelemetryLocator.GameLogFolderFromResults(_resultsPath);
        var files = logFolder == null
            ? new List<string>()
            : TelemetryLocator.FindTelemetryFiles(logFolder, track);

        // Standardmäßig nur die jüngste Session (klein & relevant); ?all=1 = alles.
        if (!all)
        {
            files = TelemetryLocator.LatestSessionFiles(files);
        }

        if (files.Count == 0)
        {
            res.StatusCode = 404;
            var msg = Encoding.UTF8.GetBytes($"Keine Telemetrie für '{track}' gefunden.");
            res.OutputStream.Write(msg);
            res.Close();
            return;
        }

        res.ContentType = "application/zip";
        var fileName = $"telemetry-{TelemetryLocator.Normalize(track)}.zip";
        res.AddHeader("Content-Disposition", $"attachment; filename=\"{fileName}\"");

        using (var zip = new ZipArchive(res.OutputStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in files)
            {
                var entry = zip.CreateEntry(Path.GetFileName(file), CompressionLevel.Fastest);
                using var entryStream = entry.Open();
                using var fs = File.OpenRead(file);
                fs.CopyTo(entryStream);
            }
        }
        res.Close();
        _logger.LogInformation("Telemetrie für '{Track}' ausgeliefert ({Count} Dateien).", track, files.Count);
    }

    private static string GetQueryValue(string? query, string key)
    {
        var q = (query ?? string.Empty).TrimStart('?');
        foreach (var pair in q.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            var k = eq < 0 ? pair : pair[..eq];
            if (string.Equals(Uri.UnescapeDataString(k), key, StringComparison.OrdinalIgnoreCase))
            {
                return eq < 0 ? string.Empty : Uri.UnescapeDataString(pair[(eq + 1)..].Replace('+', ' '));
            }
        }
        return string.Empty;
    }

    public void Dispose()
    {
        _cts.Cancel();
        if (_listener.IsListening) _listener.Stop();
        _listener.Close();
    }
}
