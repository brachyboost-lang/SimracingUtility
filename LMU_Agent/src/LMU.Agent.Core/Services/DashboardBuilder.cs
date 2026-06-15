using System.Text.RegularExpressions;
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

        // Namen, die schon als Mitstreiter gelistet sind, nicht zusätzlich als
        // "gegnerisches Team" zeigen (Dedup-Wunsch).
        var alreadyListed = dashboard.MostRacedWith
            .Select(c => c.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        dashboard.MostRacedAgainstTeams =
            MostRacedAgainstTeams(all, competitive, myResults, me, alreadyListed, topN);
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
        List<RaceResult> myResults, string me, HashSet<string> exclude, int topN)
    {
        var standard = StandardLiveries(all);
        var myTeams = myResults
            .Select(r => r.TeamName)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var mySessions = myResults.Select(r => r.SessionId).ToHashSet();

        // Ein echtes Team hat mehrere Mitglieder. Namen, die nur ein einziger
        // Fahrer nutzt, sind faktisch Einzelpersonen (Teamname = eigener Name) und
        // werden nicht als "Team" gezählt.
        var multiDriverTeams = all
            .Where(r => r.IsPlayer && !string.IsNullOrWhiteSpace(r.TeamName))
            .GroupBy(r => r.TeamName, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Select(r => r.DriverName).Distinct().Count() >= 2)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return competitive
            .Where(r => r.DriverName != me && mySessions.Contains(r.SessionId))
            .Where(r => !string.IsNullOrWhiteSpace(r.TeamName)
                        && multiDriverTeams.Contains(r.TeamName)  // echtes Team (≥2 Fahrer)
                        && !standard.Contains(r.TeamName)   // Stock-Livery
                        && !myTeams.Contains(r.TeamName)    // eigenes Team
                        && !exclude.Contains(r.TeamName))   // schon als Mitstreiter gelistet
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

    // Offizielle Saison-Livery: enthält ein Jahr (20xx) und eine #Startnummer,
    // z. B. "Akkodis ASP Team 2025 #87", "Toyota Gazoo Racing 2025 #7".
    private static readonly Regex StockLiveryPattern =
        new(@"\b20\d{2}\b.*#\s*\d+", RegexOptions.Compiled);

    /// <summary>
    /// Erkennt Standard-/Default-Liverys von LMU möglichst sicher über mehrere
    /// Signale (jeweils hinreichend):
    /// <list type="number">
    /// <item>von einem KI-Bot gefahren – Bots nutzen ausschließlich Stock-Liverys;</item>
    /// <item>offizielles Saison-Muster (Jahr + #Startnummer);</item>
    /// <item>im selben Rennen von mehreren verschiedenen Fahrern genutzt
    ///       (kann kein echtes Team sein – jeder fährt sein eigenes Auto);</item>
    /// <item>insgesamt von sehr vielen (≥ 8) verschiedenen Fahrern genutzt.</item>
    /// </list>
    /// Übrig bleiben echte, frei gewählte custom Teamnamen.
    /// </summary>
    private static HashSet<string> StandardLiveries(IReadOnlyList<RaceResult> all)
    {
        var named = all.Where(r => !string.IsNullOrWhiteSpace(r.TeamName)).ToList();
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // (1) von einem Bot gefahren
        foreach (var r in named.Where(r => !r.IsPlayer))
            result.Add(r.TeamName);

        // (2) offizielles Saison-Muster
        foreach (var r in named)
            if (StockLiveryPattern.IsMatch(r.TeamName))
                result.Add(r.TeamName);

        // (3) im selben Rennen von ≥2 Fahrern genutzt
        foreach (var session in named.GroupBy(r => r.SessionId))
            foreach (var team in session
                         .GroupBy(r => r.TeamName)
                         .Where(g => g.Select(x => x.DriverName).Distinct().Count() >= 2))
                result.Add(team.Key);

        // (4) insgesamt von sehr vielen Fahrern genutzt (Sicherheitsnetz)
        foreach (var team in named
                     .GroupBy(r => r.TeamName)
                     .Where(g => g.Select(r => r.DriverName).Distinct().Count() >= 8))
            result.Add(team.Key);

        return result;
    }
}
