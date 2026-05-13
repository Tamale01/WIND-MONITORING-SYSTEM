// File: Hubs/NotificationHub.cs
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace WindMonitoringSystem.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time browser notifications.
    /// Only authenticated users can receive targeted alerts.
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            // Optional: Log connection or add to groups
            await base.OnConnectedAsync();
        }
    }
}
