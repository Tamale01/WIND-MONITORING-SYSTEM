// File: Models/AlertThreshold.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace WindMonitoringSystem.Models
{
    public enum NotificationType
    {
        Email,
        SMS,
        BrowserPush
    }

    public class AlertThreshold
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = null!;
        public IdentityUser? User { get; set; }

        [Required]
        [Range(0, 200)]
        public decimal SpeedThreshold { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public NotificationType NotificationMethod { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Prevents spamming notifications if wind stays high.</summary>
        public DateTime? LastTriggeredAt { get; set; }
    }
}
