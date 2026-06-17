using SimracingUtility.Services;

namespace SimracingUtility.Tests
{
    public class SimGridStatsParserTests
    {
        // Fixture nachgebildet aus dem echten SimGrid-Profil-HTML (Stat-Karten +
        // eine Social-Karte "Followers", die NICHT als Renn-Stat zaehlen darf).
        private const string Html = """
        <div class="row g-1 row-cols-2">
          <div class="col"><div class="card"><div class="fs-lg fw-bold text-info">3</div>
            <div class="text-info fs-xxs fw-bold text-uppercase">Followers</div></div></div>
          <div class="col"><div class="card"><div class="fs-lg fw-bold text-yellow">16</div>
            <div class="text-yellow-500-50 fs-xxs fw-bold text-uppercase">Wins</div></div></div>
          <div class="col"><div class="card"><div class="fs-lg fw-bold text-purple">15</div>
            <div class="text-purple fs-xxs fw-bold text-uppercase">Fastest Laps</div></div></div>
          <div class="col"><div class="card"><div class="fs-lg fw-bold text-white">21</div>
            <div class="text-gray-400 fs-xxs fw-bold text-uppercase">Podiums</div></div></div>
          <div class="col"><div class="card"><div class="fs-lg fw-bold">25</div>
            <div class="fs-xxs fw-bold text-uppercase">Top 5</div></div></div>
          <div class="col"><div class="card"><div class="fs-lg fw-bold">1,027</div>
            <div class="fs-xxs fw-bold text-uppercase">Starts</div></div></div>
        </div>
        """;

        [Fact]
        public void Parse_RealisticHtml_ExtractsRacingStats()
        {
            var s = SimGridStatsParser.Parse(Html);

            Assert.Equal(1027, s.Starts);          // Tausender-Komma korrekt entfernt
            Assert.Equal(16, s.Wins);
            Assert.Equal(21, s.Podiums);
            Assert.Equal(25, s.Top5);
            Assert.Equal(15, s.FastestLaps);
            Assert.True(s.HasAny);
        }

        [Fact]
        public void Parse_IgnoresSocialCards()
        {
            // "Followers" (=3) darf in keinem Renn-Feld landen.
            var s = SimGridStatsParser.Parse(Html);
            Assert.NotEqual(3, s.Wins ?? -1);
            Assert.NotEqual(3, s.Starts ?? -1);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("<html><body>kein Profil</body></html>")]
        public void Parse_NoData_ReturnsEmpty(string? html)
        {
            var s = SimGridStatsParser.Parse(html);
            Assert.False(s.HasAny);
            Assert.Null(s.Wins);
        }

        [Fact]
        public void Parse_DoesNotConfuseTop5WithTop50()
        {
            const string html = """
            <div class="fs-lg fw-bold">999</div><div class="fs-xxs text-uppercase">Top 50%</div>
            <div class="fs-lg fw-bold">25</div><div class="fs-xxs text-uppercase">Top 5</div>
            """;
            var s = SimGridStatsParser.Parse(html);
            Assert.Equal(25, s.Top5);
        }
    }
}
