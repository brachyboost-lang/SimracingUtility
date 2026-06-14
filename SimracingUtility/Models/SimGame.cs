using System.ComponentModel.DataAnnotations;

namespace SimracingUtility.Models
{
    /// <summary>
    /// Die unterstützten Rennsimulationen. Der Wert wird als int in der DB persistiert.
    /// </summary>
    public enum SimGame
    {
        [Display(Name = "iRacing")]
        iRacing = 1,

        [Display(Name = "Le Mans Ultimate")]
        LMU = 2,

        [Display(Name = "Assetto Corsa Competizione")]
        ACC = 3
    }

    /// <summary>
    /// Statische Metadaten zu den Simulationen (Anzeigename, erlaubte Datei-Endungen
    /// für Setups). Wird sowohl im Controller (Validierung) als auch in den Views genutzt.
    /// </summary>
    public static class SimGameInfo
    {
        public static readonly IReadOnlyDictionary<SimGame, string> DisplayNames =
            new Dictionary<SimGame, string>
            {
                [SimGame.iRacing] = "iRacing",
                [SimGame.LMU] = "Le Mans Ultimate",
                [SimGame.ACC] = "Assetto Corsa Competizione"
            };

        /// <summary>Erlaubte (kleingeschriebene) Datei-Endungen pro Simulation.</summary>
        public static readonly IReadOnlyDictionary<SimGame, string[]> AllowedExtensions =
            new Dictionary<SimGame, string[]>
            {
                [SimGame.iRacing] = new[] { ".sto" },
                [SimGame.LMU] = new[] { ".svm" },
                [SimGame.ACC] = new[] { ".json" }
            };

        public static string DisplayName(SimGame sim) =>
            DisplayNames.TryGetValue(sim, out var n) ? n : sim.ToString();

        public static string[] ExtensionsFor(SimGame sim) =>
            AllowedExtensions.TryGetValue(sim, out var e) ? e : System.Array.Empty<string>();
    }
}
