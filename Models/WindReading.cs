// File: Models/WindReading.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WindMonitoringSystem.Models
{
    public class WindReading
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Wind speed in m/s, valid range 0–200</summary>
        [Required]
        [Range(0, 200, ErrorMessage = "Wind speed must be between 0 and 200 m/s.")]
        [Column(TypeName = "decimal(8,2)")]
        public decimal WindSpeed { get; set; }

        /// <summary>UTC timestamp of the reading</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>Sensor identifier, max 50 chars, nullable</summary>
        [MaxLength(50)]
        public string? SensorId { get; set; }

        /// <summary>True if reading is simulated, false if from real hardware</summary>
        public bool IsSimulated { get; set; } = true;
    }
}
