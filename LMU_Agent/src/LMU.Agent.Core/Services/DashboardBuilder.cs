using LMU.Agent.Core.Models;

namespace LMU.Agent.Core.Services;

/// <summary>
/// Baut aus allen Rennergebnissen die Auswertung für den Agent-Besitzer. Dieser
/// wird als der Fahrer mit den **meisten Ergebnissen** identifiziert (er ist in
/// jedem seiner Rennen vertreten). Zusätzlich werden die häufigsten Teamkollegen
/// (gleiches Auto im selben Rennen) und Gegner (anderes Auto) ermittelt.
/// </summary>
public static class DashboardBuilder
{
    public static UserDashboard Build(IReadOnlyList<RaceResult> all, int topN = 10)
    {
        var dashboard = new UserDashboard();
        if (all.Count == 0)
        {
            return dashboard;
        }

        // Besitzer = Fahrer mit den meisten Ergebnissen.
        var me = all
            .GroupBy(r => r.DriverName)
            .OrderByDescending(g => g.Count())
            .First().Key;

        var myResults = all.Where(r => r.DriverName == me).ToList();
        dashboard.DriverName = me;
        dashboard.Stats = StatisticsParser.ComputeStatistics(me, myResults);

        var teammateCounts = new Dictionary<string, int>();
        var opponentCounts = new Dictionary<string, int>();

        // Nur Sessions betrachten, in denen ich gefahren bin.
        var mySessions = myResults.Select(r => r.SessionId).ToHashSet();

        foreach (var session in all.Where(r => mySessions.Contains(r.SessionId))
                                   .GroupBy(r => r.SessionId))
        {
            var mine = session.Where(r => r.DriverName == me).ToList();
            if (mine.Count == 0) continue;

            // Auto-/Team-Schlüssel meines Eintrags in diesem Rennen.
            var myCarKeys = mine.Select(r => r.CarKey)
                                .Where(k => !string.IsNullOrWhiteSpace(k))
                                .ToHashSet();

            // Jeden anderen Fahrer einmal pro Session zählen.
            foreach (var other in session.Where(r => r.DriverName != me)
                                         .Select(r => new { r.DriverName, r.CarKey })
                                         .DistinctBy(x => x.DriverName))
            {
                bool teammate = !string.IsNullOrWhiteSpace(other.CarKey)
                                && myCarKeys.Contains(other.CarKey);

                var target = teammate ? teammateCounts : opponentCounts;
                target[other.DriverName] = target.GetValueOrDefault(other.DriverName) + 1;
            }
        }

        dashboard.Teammates = ToTopList(teammateCounts, topN);
        dashboard.Opponents = ToTopList(opponentCounts, topN);
        return dashboard;
    }

    private static List<CompanionCount> ToTopList(Dictionary<string, int> counts, int topN)
        => counts
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key)
            .Take(topN)
            .Select(kv => new CompanionCount { Name = kv.Key, RacesShared = kv.Value })
            .ToList();
}
