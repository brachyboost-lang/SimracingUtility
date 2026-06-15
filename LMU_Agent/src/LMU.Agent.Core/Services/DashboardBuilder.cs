using LMU.Agent.Core.Models;

namespace LMU.Agent.Core.Services;

/// <summary>
/// Baut aus allen Rennergebnissen die Auswertung für den Agent-Besitzer.
///
/// Wichtige Regeln (aus der Analyse echter LMU-Dateien):
/// - <b>KI-Rennen sind Trainingsrennen</b>: Sessions, die einen Bot
///   (<c>isPlayer=0</c>) enthalten, werden komplett ignoriert.
/// - <b>Teamkollegen sind nicht ableitbar</b>: die Ergebnisse listen genau einen
///   Fahrer pro Auto, Fahrerwechsel tauchen nicht auf und <c>TeamName</c> ist
///   keine eindeutige Auto-Kennung. Stattdessen "Mitstreiter": menschliche Fahrer,
///   mit denen man am häufigsten im selben Rennen war.
/// - <b>Sprint vs. Endurance</b> nach Renndauer (ab 90 Minuten Endurance).
/// - <b>Beste Runde je Strecke</b> statt eines aussagelosen Gesamtwerts.
/// </summary>
public static class DashboardBuilder
{
    private const int EnduranceMinutes = 90;

    public static UserDashboard Build(IReadOnlyList<RaceResult> all, int topN = 10)
    {
        var dashboard = new UserDashboard();

        // KI-/Trainingsrennen ausschließen: jede Session, in der ein Bot auftaucht.
        var aiSessions = all
            .Where(r => !r.IsPlayer)
            .Select(r => r.SessionId)
            .ToHashSet();

        var competitive = all.Where(r => !aiSessions.Contains(r.SessionId)).ToList();
        if (competitive.Count == 0)
        {
            return dashboard;
        }

        // Besitzer = Fahrer mit den meisten Ergebnissen in den Wettkampfrennen.
        var me = competitive
            .GroupBy(r => r.DriverName)
            .OrderByDescending(g => g.Count())
            .First().Key;

        var myResults = competitive.Where(r => r.DriverName == me).ToList();

        dashboard.DriverName = me;
        dashboard.Sprint = ComputeCategory(myResults.Where(r => !r.IsEndurance));
        dashboard.Endurance = ComputeCategory(myResults.Where(r => r.IsEndurance));
        dashboard.BestLapsByTrack = BestLapsByTrack(myResults);
        dashboard.MostRacedWith = MostRacedWith(competitive, myResults, me, topN);
        dashboard.MostRacedAgainstTeams = MostRacedAgainstTeams(all, competitive, myResults, me, topN);
        return dashboard;
    }

    private static CategoryStats ComputeCategory(IEnumerable<RaceResult> resultsEnum)
    {
        var results = resultsEnum.ToList();
        var finished = results.Where(r => !r.IsDnf && r.Position > 0).ToList();

        return new CategoryStats
        {
            TotalRaces = results.Count,
            Wins = finished.Count(r => r.Position == 1),
            Podiums = finished.Count(r => r.Position <= 3),
            Top5 = finished.Count(r => r.Position <= 5),
            Top10 = finished.Count(r => r.Position <= 10),
            TopHalf = finished.Count(IsInTopHalf),
            Dnf = results.Count(r => r.IsDnf),
            BestPosition = finished.Count > 0 ? finished.Min(r => r.Position) : 0,
            LastRaceDate = results.Count > 0 ? results.Max(r => r.RaceDate) : DateTime.MinValue,
        };
    }

    private static bool IsInTopHalf(RaceResult r)
    {
        if (r.FieldSize <= 0) return false;
        var half = (int)Math.Ceiling(r.FieldSize / 2.0);
        return r.Position <= half;
    }

    private static List<TrackBestLap> BestLapsByTrack(List<RaceResult> myResults)
        => myResults
            .Where(r => r.BestLapTime > 0 && !string.IsNullOrWhiteSpace(r.TrackName))
            .GroupBy(r => r.TrackName)
            .Select(g => new TrackBestLap { Track = g.Key, BestLapTime = g.Min(r => r.BestLapTime) })
            .OrderBy(t => t.Track)
            .ToList();

    private static List<CompanionCount> MostRacedWith(
        List<RaceResult> competitive, List<RaceResult> myResults, string me, int topN)
    {
        var mySessions = myResults.Select(r => r.SessionId).ToHashSet();

        return competitive
            .Where(r => r.DriverName != me && mySessions.Contains(r.SessionId))
            // ein Fahrer kann pro Session nur einmal zählen
            .GroupBy(r => r.DriverName)
            .Select(g => new CompanionCount
            {
                Name = g.Key,
                RacesShared = g.Select(r => r.SessionId).Distinct().Count()
            })
            .OrderByDescending(c => c.RacesShared)
            .ThenBy(c => c.Name)
            .Take(topN)
            .ToList();
    }

    private static List<CompanionCount> MostRacedAgainstTeams(
        IReadOnlyList<RaceResult> all, List<RaceResult> competitive,
        List<RaceResult> myResults, string me, int topN)
    {
        var standard = StandardLiveries(all);
        var myTeams = myResults
            .Select(r => r.TeamName)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var mySessions = myResults.Select(r => r.SessionId).ToHashSet();

        return competitive
            .Where(r => r.DriverName != me && mySessions.Contains(r.SessionId))
            .Where(r => !string.IsNullOrWhiteSpace(r.TeamName)
                        && !standard.Contains(r.TeamName)
                        && !myTeams.Contains(r.TeamName))
            .GroupBy(r => r.TeamName)
            .Select(g => new CompanionCount
            {
                Name = g.Key,
                RacesShared = g.Select(r => r.SessionId).Distinct().Count()
            })
            .OrderByDescending(c => c.RacesShared)
            .ThenBy(c => c.Name)
            .Take(topN)
            .ToList();
    }

    /// <summary>
    /// Standard-/Default-Liverys: ein TeamName, der in irgendeinem Rennen von
    /// mehreren verschiedenen Fahrern verwendet wird (kann also kein echtes Team
    /// sein – jeder fährt sein eigenes Auto). Custom-Teamnamen sind je Rennen
    /// eindeutig.
    /// </summary>
    private static HashSet<string> StandardLiveries(IReadOnlyList<RaceResult> all)
        => all
            .Where(r => !string.IsNullOrWhiteSpace(r.TeamName))
            .GroupBy(r => r.SessionId)
            .SelectMany(session => session
                .GroupBy(r => r.TeamName)
                .Where(g => g.Select(x => x.DriverName).Distinct().Count() >= 2)
                .Select(g => g.Key))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
