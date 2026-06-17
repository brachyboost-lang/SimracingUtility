using System;
using System.Collections.Generic;
using System.Linq;

namespace SimracingUtility.Models
{
    /// <summary>
    /// Kuratierte Übersicht „Was steht an in Le Mans Ultimate" – offizielle
    /// Special Events und (Team-)Meisterschaften. Bewusst getrennt vom LMU-Agent:
    /// hier geht es nicht um die Termine einzelner Personen, sondern um das
    /// allgemeine Angebot. Quelle ist eine gepflegte Datendatei
    /// (<c>wwwroot/data/lmu-events.json</c>), keine Live-Abfrage/kein Scraping.
    /// </summary>
    public class LmuEventCatalog
    {
        public List<LmuSpecialEvent> SpecialEvents { get; set; } = new();
        public List<LmuChampionship> Championships { get; set; } = new();

        /// <summary>Kommende Special Events (Datum ab <paramref name="today"/>), aufsteigend.</summary>
        public List<LmuSpecialEvent> UpcomingSpecialEvents(DateOnly today) =>
            SpecialEvents
                .Where(e => e.Date >= today)
                .OrderBy(e => e.Date)
                .ToList();

        /// <summary>
        /// Meisterschaften sortiert: Team-Events zuerst, dann nach Startdatum
        /// (ohne Datum ans Ende), dann nach Name.
        /// </summary>
        public List<LmuChampionship> SortedChampionships() =>
            Championships
                .OrderByDescending(c => c.IsTeam)
                .ThenBy(c => c.StartsOn ?? DateOnly.MaxValue)
                .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
    }

    /// <summary>Offizielles LMU-Special-Event (zeitlich datiert).</summary>
    public class LmuSpecialEvent
    {
        public string Title { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public string Track { get; set; } = string.Empty;
        public string Classes { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
    }

    /// <summary>Community-Meisterschaft (z. B. auf SimGrid), als Deep-Link kuratiert.</summary>
    public class LmuChampionship
    {
        public string Name { get; set; } = string.Empty;
        public string Organizer { get; set; } = string.Empty;
        public DateOnly? StartsOn { get; set; }
        public bool IsTeam { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}
