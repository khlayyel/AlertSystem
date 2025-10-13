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
            // Nouveau modèle: pas de AlertRecipients/Users; neutraliser le service pour l’instant
            var pendingReminders = new List<object>();

            _logger.LogInformation("Processing {Count} pending reminders (neutralized)", pendingReminders.Count);

            // Neutralisé: pas de boucle d’envoi

            await Task.CompletedTask;
        }

    }
}
