using AlertSystem.Data;
using AlertSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AlertSystem.Services
{
    public sealed class ReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReminderService> _logger;
        private readonly ReminderConfiguration _config;

        public ReminderService(IServiceProvider serviceProvider, ILogger<ReminderService> logger, ReminderConfiguration config)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ReminderService started with config: FirstDelay={FirstDelay}, Interval={Interval}, MaxReminders={Max}", 
                _config.FirstReminderDelay, _config.SubsequentReminderInterval, _config.MaxReminders);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_config.EnableReminders)
                    {
                        await ProcessReminders();
                    }
                    await Task.Delay(_config.ServiceCheckInterval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ReminderService");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Wait 1 min on error
                }
            }
        }

        private async Task ProcessReminders()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var now = DateTime.UtcNow;
            var pendingReminders = await dbContext.AlertRecipients
                .Include(ar => ar.Alert)
                .Include(ar => ar.User)
                .Where(ar => ar.Alert.AlertType == "Obligatoire" 
                          && !ar.IsConfirmed 
                          && ar.NextReminderAt.HasValue 
                          && ar.NextReminderAt <= now
                          && ar.SendStatus != "Cancelled")
                .ToListAsync();

            _logger.LogInformation("Processing {Count} pending reminders", pendingReminders.Count);

            foreach (var reminder in pendingReminders)
            {
                try
                {
                    var alertInterval = _config.GetIntervalForAlertType(reminder.Alert.AlertType);
                    var title = $"[RAPPEL - {reminder.Alert.AlertType}] {reminder.Alert.Title}";
                    var message = $"RAPPEL AUTOMATIQUE\n\n" +
                                 $"Titre: {reminder.Alert.Title}\n" +
                                 $"Type: {reminder.Alert.AlertType}\n" +
                                 $"Date d'envoi initial: {reminder.Alert.CreatedAt:yyyy-MM-dd HH:mm}\n\n" +
                                 $"Message:\n{reminder.Alert.Message}\n\n" +
                                 $"Cette alerte nécessite votre confirmation. Veuillez la consulter dès que possible.";

                    var url = $"/AlertsCrud/Details/{reminder.Alert.AlertId}";
                    
                    // Send to all platforms
                    var successfulPlatforms = await notificationService.SendToAllPlatformsAsync(
                        reminder.UserId, title, message, url);
                    
                    // Update reminder timing and platforms
                    reminder.LastSentAt = now;
                    reminder.NextReminderAt = now.Add(_config.SubsequentReminderInterval);
                    reminder.DeliveryPlatforms = JsonSerializer.Serialize(successfulPlatforms);
                    
                    _logger.LogInformation("Reminder sent for AlertRecipient {Id} via platforms: {Platforms}", 
                        reminder.AlertRecipientId, string.Join(", ", successfulPlatforms));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send reminder for AlertRecipient {Id}", reminder.AlertRecipientId);
                    reminder.SendStatus = "Failed";
                }
            }

            if (pendingReminders.Any())
            {
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Updated {Count} reminder records", pendingReminders.Count);
            }
        }

    }
}
