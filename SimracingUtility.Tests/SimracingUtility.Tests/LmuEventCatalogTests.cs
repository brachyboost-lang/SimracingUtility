using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using SimracingUtility.Models;

namespace SimracingUtility.Tests
{
    public class LmuEventCatalogTests
    {
        private static LmuEventCatalog Sample() => new()
        {
            SpecialEvents =
            {
                new LmuSpecialEvent { Title = "Vergangen",  Date = new DateOnly(2026, 5, 1),  Track = "Alt" },
                new LmuSpecialEvent { Title = "Heute",      Date = new DateOnly(2026, 9, 1),  Track = "Spa" },
                new LmuSpecialEvent { Title = "Spaeter",    Date = new DateOnly(2026, 10, 20), Track = "Le Mans" },
                new LmuSpecialEvent { Title = "Bald",       Date = new DateOnly(2026, 9, 8),  Track = "COTA" },
            },
            Championships =
            {
                new LmuChampionship { Name = "Solo B", IsTeam = false, StartsOn = new DateOnly(2026, 7, 1) },
                new LmuChampionship { Name = "Team Spaet", IsTeam = true, StartsOn = new DateOnly(2026, 9, 1) },
                new LmuChampionship { Name = "Team Frueh", IsTeam = true, StartsOn = new DateOnly(2026, 6, 1) },
                new LmuChampionship { Name = "Team OhneDatum", IsTeam = true, StartsOn = null },
            },
        };

        [Fact]
        public void UpcomingSpecialEvents_DropsPast_AndSortsAscending()
        {
            var upcoming = Sample().UpcomingSpecialEvents(new DateOnly(2026, 9, 1));

            Assert.Equal(new[] { "Heute", "Bald", "Spaeter" }, upcoming.Select(e => e.Title).ToArray());
            Assert.DoesNotContain(upcoming, e => e.Title == "Vergangen"); // genau am Stichtag bleibt drin
        }

        [Fact]
        public void UpcomingSpecialEvents_IncludesEventOnToday()
        {
            var upcoming = Sample().UpcomingSpecialEvents(new DateOnly(2026, 9, 1));
            Assert.Contains(upcoming, e => e.Title == "Heute");
        }

        [Fact]
        public void SortedChampionships_TeamFirst_ThenByStart_NullLast()
        {
            var sorted = Sample().SortedChampionships().Select(c => c.Name).ToArray();

            // Team zuerst (nach Startdatum, ohne Datum ans Ende), Solo danach.
            Assert.Equal(new[] { "Team Frueh", "Team Spaet", "Team OhneDatum", "Solo B" }, sorted);
        }

        [Fact]
        public void Deserializes_FromJson_WithDateOnly()
        {
            const string json = """
            {
              "specialEvents": [
                { "title": "6 Hours Le Mans", "date": "2026-06-23", "track": "Le Mans",
                  "classes": "Hypercar, WEC LMP2, LMGT3", "duration": "6h" }
              ],
              "championships": [
                { "name": "Team Cup", "organizer": "SimGrid", "startsOn": "2026-09-01",
                  "isTeam": true, "url": "https://www.thesimgrid.com/championships/1" }
              ]
            }
            """;

            var cat = JsonSerializer.Deserialize<LmuEventCatalog>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

            Assert.Single(cat.SpecialEvents);
            Assert.Equal(new DateOnly(2026, 6, 23), cat.SpecialEvents[0].Date);
            Assert.True(cat.Championships[0].IsTeam);
            Assert.Equal(new DateOnly(2026, 9, 1), cat.Championships[0].StartsOn);
        }
    }
}
