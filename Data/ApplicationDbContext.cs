// File: Data/ApplicationDbContext.cs
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WindMonitoringSystem.Models;

namespace WindMonitoringSystem.Data
{
    /// <summary>
    /// Main database context extending IdentityDbContext to include ASP.NET Core Identity tables.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>Wind readings table</summary>
        public DbSet<WindReading> WindReadings { get; set; }

        /// <summary>User alert thresholds</summary>
        public DbSet<AlertThreshold> AlertThresholds { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Index on Timestamp for fast time-range queries
            builder.Entity<WindReading>()
                .HasIndex(w => w.Timestamp);

            // Index on SensorId for sensor-based filtering
            builder.Entity<WindReading>()
                .HasIndex(w => w.SensorId);
        }
    }
}
