using Microsoft.Extensions.Logging;

namespace AlertSystem.Service.Services
{
    /// <summary>
    /// Service simplifié pour la mise à jour des KPIs - respecte le principe SRP
    /// Responsabilité unique : Calculer et fournir les métriques KPIs
    /// </summary>
    public class KpiUpdateService : IKpiUpdateService
    {
        private readonly IAlertReadService _alertReadService;
        private readonly ILogger<KpiUpdateService> _logger;

        public KpiUpdateService(
            IAlertReadService alertReadService,
            ILogger<KpiUpdateService> logger)
        {
            _alertReadService = alertReadService;
            _logger = logger;
        }

        public async Task SendInboxKpiUpdateAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Calculating inbox KPIs for user {UserId}", userId);
                
                var unreadCount = await _alertReadService.GetUnreadCountAsync();
                var mandatoryPendingCount = await _alertReadService.GetUnconfirmedMandatoryCountAsync();
                var confirmedMandatoryCount = await _alertReadService.GetConfirmedMandatoryCountAsync();

                _logger.LogInformation("Inbox KPIs for user {UserId}: Unread={Unread}, Pending={Pending}, Confirmed={Confirmed}",
                    userId, unreadCount, mandatoryPendingCount, confirmedMandatoryCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate inbox KPIs for user {UserId}", userId);
            }
        }

        public async Task SendOutboxKpiUpdateAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Calculating outbox KPIs for user {UserId}", userId);
                
                var sentTodayCount = await _alertReadService.GetCountByStatusAsync(2); // Envoyé
                var failedCount = await _alertReadService.GetCountByStatusAsync(4); // Échoué

                _logger.LogInformation("Outbox KPIs for user {UserId}: Sent={Sent}, Failed={Failed}",
                    userId, sentTodayCount, failedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate outbox KPIs for user {UserId}", userId);
            }
        }

        public async Task SendAllKpiUpdateAsync(int userId)
        {
            await Task.WhenAll(
                SendInboxKpiUpdateAsync(userId),
                SendOutboxKpiUpdateAsync(userId)
            );
        }

        public Task SendOutboxModalUpdateAsync(int userId, int alerteId, object recipientData)
        {
            try
            {
                _logger.LogInformation("Outbox modal update for user {UserId}, alert {AlerteId}", userId, alerteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox modal update for user {UserId}, alert {AlerteId}", userId, alerteId);
            }

            return Task.CompletedTask;
        }

        public Task SendNewAlertToOutboxAsync(int userId, object alertData)
        {
            try
            {
                _logger.LogInformation("New alert added to outbox for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process new alert to outbox for user {UserId}", userId);
            }

            return Task.CompletedTask;
        }

        public Task SendNewAlertToInboxAsync(int userId, object alertData)
        {
            try
            {
                _logger.LogInformation("New alert added to inbox for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process new alert to inbox for user {UserId}", userId);
            }

            return Task.CompletedTask;
        }

        public Task SendTestOutboxKpiUpdateAsync(int userId, object testData)
        {
            try
            {
                _logger.LogInformation("Test outbox KPI update for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process test outbox KPI update for user {UserId}", userId);
            }

            return Task.CompletedTask;
        }
    }
}