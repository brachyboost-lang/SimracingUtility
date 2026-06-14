using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace SimracingUtility.Models
{
    /// <summary>
    /// Ein von einem Nutzer hochgeladenes Setup. Die Setup-Datei selbst wird als
    /// <see cref="FileData"/> (bytea in PostgreSQL) direkt in der DB gespeichert.
    /// </summary>
    public class Setup
    {
        public int Id { get; set; }

        // --- Eigentümer (Identity-User) ---
        [Required]
        public string OwnerId { get; set; } = string.Empty;
        public IdentityUser? Owner { get; set; }

        // --- Zuordnung Simulation / Auto / Strecke ---
        public SimGame Sim { get; set; }

        public int CarId { get; set; }
        public SimCar? Car { get; set; }

        public int TrackId { get; set; }
        public SimTrack? Track { get; set; }

        // --- Optionale Metadaten ---
        [StringLength(150)]
        public string? Name { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        /// <summary>Rundenzeit als Text, z. B. "1:58.231".</summary>
        [StringLength(20)]
        public string? LapTime { get; set; }

        /// <summary>Spur-/Streckentemperatur in °C.</summary>
        public double? TrackTempCelsius { get; set; }

        [StringLength(100)]
        public string? CreatorName { get; set; }

        // --- Datei ---
        [Required]
        [StringLength(260)]
        public string FileName { get; set; } = string.Empty;

        [StringLength(150)]
        public string ContentType { get; set; } = "application/octet-stream";

        public long FileSize { get; set; }

        [Required]
        public byte[] FileData { get; set; } = Array.Empty<byte>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
