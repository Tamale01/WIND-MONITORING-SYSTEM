// File: Services/NotificationService.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using WindMonitoringSystem.Hubs;
using WindMonitoringSystem.Models;
using System.Net.Mail;
using System.Net;

namespace WindMonitoringSystem.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _config;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IHubContext<NotificationHub> hubContext,
            UserManager<IdentityUser> userManager,
            IConfiguration config,
            ILogger<NotificationService> logger)
        {
            _hubContext = hubContext;
            _userManager = userManager;
            _config = config;
            _logger = logger;
        }

        public async Task SendAlertAsync(string userId, string message, NotificationType method)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            switch (method)
            {
                case NotificationType.BrowserPush:
                    await _hubContext.Clients.User(userId).SendAsync("ReceiveAlert", message);
                    _logger.LogInformation("SignalR alert sent to user {UserId}", userId);
                    break;

                case NotificationType.Email:
                    await SendEmailAsync(user.Email!, "Wind Alert!", message);
                    break;

                case NotificationType.SMS:
                    _logger.LogWarning("SMS Notification requested for {User} (Twilio logic placeholder): {Msg}", user.UserName, message);
                    // Implement Twilio logic here
                    break;
            }
        }

        public async Task BroadcastAlertAsync(string message)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveAlert", message);
            _logger.LogInformation("SignalR broadcast alert sent: {Msg}", message);
        }

        private async Task SendEmailAsync(string email, string subject, string body)
        {
            try
            {
                // Placeholder SMTP logic — in production, use SendGrid or actual SMTP server
                _logger.LogInformation("Sending Email to {Email}: {Subject}", email, subject);
                
                // Example using basic SmtpClient (Note: SmtpClient is legacy, use MailKit in production)
                /*
                using var client = new SmtpClient(_config["Smtp:Host"], int.Parse(_config["Smtp:Port"]))
                {
                    Credentials = new NetworkCredential(_config["Smtp:User"], _config["Smtp:Pass"]),
                    EnableSsl = true
                };
                var mailMessage = new MailMessage("no-reply@windmonitor.com", email, subject, body);
                await client.SendMailAsync(mailMessage);
                */
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", email);
            }
        }
    }
}
