// File: Data/DbInitializer.cs
using Microsoft.AspNetCore.Identity;
using WindMonitoringSystem.Models;

namespace WindMonitoringSystem.Data
{
    /// <summary>
    /// Seeds initial data into the database on application startup:
    ///   • 50 sample WindReading records spread over the past 7 days
    ///   • One admin user: admin@windmonitor.com / Admin@123
    ///   • "Admin" role creation and assignment
    /// </summary>
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db          = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger      = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

            // Ensure database and migrations are applied
            logger.LogInformation("Ensuring database is created and initialized...");
            await db.Database.EnsureCreatedAsync();
            logger.LogInformation("Database is ready.");

            // ── Seed Roles ─────────────────────────────────────────────────────────
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
                logger.LogInformation("Created 'Admin' role.");
            }

            // ── Seed Admin User ────────────────────────────────────────────────────
            const string adminEmail    = "admin@windmonitor.com";
            const string adminPassword = "Admin@123";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new IdentityUser
                {
                    UserName       = adminEmail,
                    Email          = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                    logger.LogInformation("Seeded admin user: {Email}", adminEmail);
                }
                else
                {
                    logger.LogError("Failed to create admin user: {Errors}",
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            // ── Seed Sample Wind Readings ──────────────────────────────────────────
            if (!db.WindReadings.Any())
            {
                var rng      = new Random(42); // fixed seed for reproducibility
                var readings = new List<WindReading>();
                var now      = DateTime.UtcNow;

                for (int i = 0; i < 50; i++)
                {
                    // Spread readings over past 7 days
                    var minutesAgo = rng.Next(0, 7 * 24 * 60);
                    readings.Add(new WindReading
                    {
                        WindSpeed   = Math.Round((decimal)(rng.NextDouble() * 30), 2),
                        Timestamp   = now.AddMinutes(-minutesAgo),
                        SensorId    = "SEED-001",
                        IsSimulated = true
                    });
                }

                // Sort ascending by timestamp before insert
                readings = readings.OrderBy(r => r.Timestamp).ToList();

                await db.WindReadings.AddRangeAsync(readings);
                await db.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} sample wind readings.", readings.Count);
            }
        }
    }
}
