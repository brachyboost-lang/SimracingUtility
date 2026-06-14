using System.Collections.Generic;

namespace SimracingUtility.Models
{
    /// <summary>
    /// Ein Auto einer bestimmten Simulation. Wird aus wwwroot/data/sim_data.json geseedet.
    /// (Sim, Slug) ist eindeutig.
    /// </summary>
    public class SimCar
    {
        public int Id { get; set; }

        public SimGame Sim { get; set; }

        /// <summary>Stabiler, technischer Schlüssel aus der Seed-Datei (z. B. "ferrari_296_gt3").</summary>
        public string Slug { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public ICollection<Setup> Setups { get; set; } = new List<Setup>();
    }
}
