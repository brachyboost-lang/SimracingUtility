using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SimracingUtility.Models
{
    /// <summary>
    /// Eingabemodell für das Upload-Formular. Vom <see cref="Setup"/>-Entity getrennt,
    /// damit der Datei-Upload (IFormFile) und die Validierung sauber abbildbar sind.
    /// </summary>
    public class SetupUploadViewModel
    {
        [Required(ErrorMessage = "Bitte eine Simulation wählen.")]
        public SimGame? Sim { get; set; }

        [Required(ErrorMessage = "Bitte ein Auto wählen.")]
        public int? CarId { get; set; }

        [Required(ErrorMessage = "Bitte eine Strecke wählen.")]
        public int? TrackId { get; set; }

        [Required(ErrorMessage = "Bitte eine Setup-Datei wählen.")]
        public IFormFile? File { get; set; }

        [StringLength(150)]
        [Display(Name = "Setup-Name")]
        public string? Name { get; set; }

        [StringLength(2000)]
        [Display(Name = "Beschreibung")]
        public string? Description { get; set; }

        [StringLength(20)]
        [Display(Name = "Rundenzeit (z. B. 1:58.231)")]
        public string? LapTime { get; set; }

        [Range(-20, 80, ErrorMessage = "Bitte eine realistische Temperatur angeben.")]
        [Display(Name = "Spurtemperatur (°C)")]
        public double? TrackTempCelsius { get; set; }

        [StringLength(100)]
        [Display(Name = "Ersteller-Name")]
        public string? CreatorName { get; set; }
    }
}
