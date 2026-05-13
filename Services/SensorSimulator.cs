// File: Services/SensorSimulator.cs
namespace WindMonitoringSystem.Services
{
    /// <summary>
    /// Simulates a physical wind sensor by generating random wind speed values.
    /// In a real deployment, this would be replaced by hardware SDK calls or MQTT subscriptions.
    /// </summary>
    public class SensorSimulator : ISensorSimulator
    {
        private readonly Random _random = new();

        /// <summary>
        /// Returns a simulated wind speed between 0 and 30 m/s with realistic variance.
        /// Uses a weighted distribution to make medium speeds more common.
        /// </summary>
        public decimal GetCurrentWindSpeed()
        {
            // Simulate realistic wind patterns: mostly calm/moderate, occasional strong gusts
            double baseSpeed = _random.NextDouble() * 20;           // 0–20 base
            double gust     = _random.NextDouble() < 0.1 ? _random.NextDouble() * 10 : 0; // 10% chance of gust
            double speed    = Math.Min(baseSpeed + gust, 30);
            return Math.Round((decimal)speed, 2);
        }
    }
}
