// File: Services/ISensorSimulator.cs
namespace WindMonitoringSystem.Services
{
    /// <summary>
    /// Interface for simulating wind sensor hardware.
    /// Implementations return a random wind speed value.
    /// </summary>
    public interface ISensorSimulator
    {
        /// <summary>
        /// Returns a simulated current wind speed in m/s (range: 0–30).
        /// </summary>
        decimal GetCurrentWindSpeed();
    }
}
