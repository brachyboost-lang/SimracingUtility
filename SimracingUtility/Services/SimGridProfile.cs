using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace SimracingUtility.Services
{
    /// <summary>
    /// Validiert eine vom Nutzer angegebene SimGrid-Fahrerprofil-URL und extrahiert
    /// Slug ("8444-ranokar") und Fahrer-Id. Akzeptiert wird ausschliesslich
    /// thesimgrid.com – so landet keine beliebige Fremd-URL als angezeigter Link in
    /// der Stats-Ansicht. Die eigentliche Datenanbindung (GridOS-API) folgt separat.
    /// </summary>
    public static class SimGridProfile
    {
        private const string BaseUrl = "https://www.thesimgrid.com/drivers/";

        // Slug = fuehrende Fahrer-Id, optional gefolgt von "-name" (URL-sicher).
        private static readonly Regex SlugRegex =
            new(@"^[0-9]+(?:-[A-Za-z0-9_-]+)?$", RegexOptions.Compiled);

        /// <summary>
        /// Versucht, aus <paramref name="input"/> ein SimGrid-Profil zu lesen. Erlaubt
        /// sind die volle URL (mit/ohne Schema, mit/ohne <c>/activities</c>, www
        /// optional), ein nackter Slug ("8444-ranokar") oder eine reine Id ("8444").
        /// Bei Erfolg liefert <paramref name="profileUrl"/> die kanonische Profil-URL.
        /// </summary>
        public static bool TryParse(string? input, out string slug, out int driverId, out string profileUrl)
        {
            slug = string.Empty;
            driverId = 0;
            profileUrl = string.Empty;

            if (string.IsNullOrWhiteSpace(input)) return false;
            var text = input.Trim();

            string candidate;
            if (text.Contains('/') || text.Contains('.'))
            {
                // Als URL behandeln; fehlt das Schema, https:// voranstellen.
                var withScheme = text.Contains("://") ? text : "https://" + text;
                if (!Uri.TryCreate(withScheme, UriKind.Absolute, out var uri)) return false;

                var host = uri.Host.ToLowerInvariant();
                if (host != "thesimgrid.com" && host != "www.thesimgrid.com") return false;

                var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var idx = Array.FindIndex(segments, s => s.Equals("drivers", StringComparison.OrdinalIgnoreCase));
                if (idx < 0 || idx + 1 >= segments.Length) return false;
                candidate = segments[idx + 1];
            }
            else
            {
                candidate = text; // nackter Slug oder reine Id
            }

            candidate = candidate.Trim();
            if (!SlugRegex.IsMatch(candidate)) return false;

            var digits = new string(candidate.TakeWhile(char.IsDigit).ToArray());
            if (!int.TryParse(digits, out driverId) || driverId <= 0)
            {
                driverId = 0;
                return false;
            }

            slug = candidate;
            profileUrl = BaseUrl + slug;
            return true;
        }
    }
}
