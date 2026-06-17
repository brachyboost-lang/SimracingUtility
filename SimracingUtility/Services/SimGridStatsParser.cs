using System.Linq;
using System.Text.RegularExpressions;
using SimracingUtility.Models;

namespace SimracingUtility.Services
{
    /// <summary>
    /// Liest Renn-Kennzahlen aus dem HTML eines öffentlichen SimGrid-Profils.
    /// Die Seite ist serverseitig gerendert (Rails/Hotwire), ohne eingebettetes JSON;
    /// die Werte stehen als Karten „&lt;div&gt;Zahl&lt;/div&gt;&lt;div ...text-uppercase&gt;Label&lt;/div&gt;".
    /// Verankert wird am <b>Label</b> (stabiler als die wechselnden CSS-Klassen).
    ///
    /// Hinweis: Das ist Scraping einer fremden Seite – bewusst fragil (Layout-Änderungen
    /// brechen es) und nur „best effort". Schlägt das Parsen fehl, bleiben die Werte leer.
    /// </summary>
    public static class SimGridStatsParser
    {
        // Label im HTML -> Setter am Ergebnis. Reihenfolge egal.
        private static readonly (string Label, System.Action<SimGridStats, int> Set)[] Targets =
        {
            ("Starts",       (s, v) => s.Starts = v),
            ("Wins",         (s, v) => s.Wins = v),
            ("Podiums",      (s, v) => s.Podiums = v),
            ("Top 5",        (s, v) => s.Top5 = v),
            ("Fastest Laps", (s, v) => s.FastestLaps = v),
        };

        public static SimGridStats Parse(string? html)
        {
            var stats = new SimGridStats();
            if (string.IsNullOrEmpty(html)) return stats;

            foreach (var (label, set) in Targets)
            {
                // Label tolerant gegen Mehrfach-Whitespace bilden (z. B. "Fastest Laps").
                var labelPattern = string.Join(@"\s+", label.Split(' ').Select(Regex.Escape));
                var rx = new Regex(
                    @">([\d][\d,]*)\s*</div>\s*<div[^>]*text-uppercase[^>]*>\s*" + labelPattern + @"\s*</div>",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase);

                var m = rx.Match(html);
                if (m.Success && int.TryParse(m.Groups[1].Value.Replace(",", string.Empty), out var val))
                {
                    set(stats, val);
                }
            }

            return stats;
        }
    }
}
