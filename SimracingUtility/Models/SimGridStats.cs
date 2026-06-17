namespace SimracingUtility.Models
{
    /// <summary>
    /// Aus dem öffentlichen SimGrid-Profil gelesene Renn-Kennzahlen. Nullable:
    /// fehlt ein Wert (Layout-Änderung o. Ä.), bleibt er offen statt 0. Bewusst nur
    /// Renn-/Leistungszahlen – keine PII (Mail/Discord/Steam werden nie gelesen).
    /// </summary>
    public class SimGridStats
    {
        public int? Starts { get; set; }
        public int? Wins { get; set; }
        public int? Podiums { get; set; }
        public int? Top5 { get; set; }
        public int? FastestLaps { get; set; }

        public bool HasAny =>
            Starts.HasValue || Wins.HasValue || Podiums.HasValue || Top5.HasValue || FastestLaps.HasValue;
    }
}
