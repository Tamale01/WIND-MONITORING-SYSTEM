// File: Services/INotificationService.cs
using WindMonitoringSystem.Models;

namespace WindMonitoringSystem.Services
{
    public interface INotificationService
    {
        Task SendAlertAsync(string userId, string message, NotificationType method);
    }
}
