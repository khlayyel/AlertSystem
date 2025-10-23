using Microsoft.AspNetCore.SignalR;
using AlertSystem.Hubs;
using Microsoft.Extensions.Logging;

namespace AlertSystem.Service
{
    public class KpiUpdateService : IKpiUpdateService
    {
        private readonly IHubContext<NotificationsHub> _hubContext;
        private readonly IAlertReadService _alertReadService;
        private readonly ILogger<KpiUpdateService> _logger;

        public KpiUpdateService(
            IHubContext<NotificationsHub> hubContext,
            IAlertReadService alertReadService,
            ILogger<KpiUpdateService> logger)
        {
            _hubContext = hubContext;
            _alertReadService = alertReadService;
            _logger = logger;
        }

        public async Task SendInboxKpiUpdateAsync(int userId)
        {
            try
            {
                _logger.LogInformation("SendInboxKpiUpdateAsync called for user {UserId}", userId);
                
                var unreadCount = await _alertReadService.GetUnreadCountAsync(userId);
                var mandatoryPendingCount = await _alertReadService.GetMandatoryPendingCountAsync(userId);
                var confirmedMandatoryCount = await _alertReadService.GetConfirmedMandatoryCountAsync(userId);

                var kpiData = new
                {
                    unreadCount,
                    mandatoryPendingCount,
                    confirmedMandatoryCount
                };

                _logger.LogInformation("Sending inbox KPI update to group User_{UserId} with data: {KpiData}", userId, kpiData);
                await _hubContext.Clients.Group($"User_{userId}").SendAsync("UpdateInboxKpis", kpiData);
                _logger.LogInformation("Sent inbox KPI update for user {UserId}: Unread={Unread}, Pending={Pending}, Confirmed={Confirmed}",
                    userId, unreadCount, mandatoryPendingCount, confirmedMandatoryCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send inbox KPI update for user {UserId}", userId);
            }
        }

        public async Task SendOutboxKpiUpdateAsync(int userId)
        {
            try
            {
                _logger.LogInformation("SendOutboxKpiUpdateAsync called for user {UserId}", userId);
                
                var sentTodayCount = await _alertReadService.GetSentTodayCountAsync(userId);
                var failedCount = await _alertReadService.GetFailedCountAsync(userId);

                var kpiData = new
                {
                    sentTodayCount,
                    failedCount
                };

                _logger.LogInformation("Sending outbox KPI update to group User_{UserId} with data: {KpiData}", userId, kpiData);
                await _hubContext.Clients.Group($"User_{userId}").SendAsync("UpdateOutboxKpis", kpiData);
                _logger.LogInformation("Sent outbox KPI update for user {UserId}: Sent={Sent}, Failed={Failed}",
                    userId, sentTodayCount, failedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send outbox KPI update for user {UserId}", userId);
            }
        }

        public async Task SendAllKpiUpdateAsync(int userId)
        {
            await Task.WhenAll(
                SendInboxKpiUpdateAsync(userId),
                SendOutboxKpiUpdateAsync(userId)
            );
        }

        public async Task SendOutboxModalUpdateAsync(int userId, int alerteId, object recipientData)
        {
            try
            {
                await _hubContext.Clients.Group($"User_{userId}").SendAsync("UpdateOutboxModal", new
                {
                    alerteId,
                    recipientData
                });
                _logger.LogInformation("Sent outbox modal update for user {UserId}, alert {AlerteId}", userId, alerteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send outbox modal update for user {UserId}, alert {AlerteId}", userId, alerteId);
            }
        }

        public async Task SendTestOutboxKpiUpdateAsync(int userId, object testData)
        {
            try
            {
                _logger.LogInformation("SendTestOutboxKpiUpdateAsync called for user {UserId} with test data: {TestData}", userId, testData);
                await _hubContext.Clients.Group($"User_{userId}").SendAsync("UpdateOutboxKpis", testData);
                _logger.LogInformation("Sent test outbox KPI update for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test outbox KPI update for user {UserId}", userId);
            }
        }
    }
}
