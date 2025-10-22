using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AlertSystem.Worker.Models;

namespace AlertSystem.Worker.Services
{
    public class AlertRepository : IAlertRepository
    {
        private readonly ILogger<AlertRepository> _logger;
        private readonly string _connectionString;

        public AlertRepository(ILogger<AlertRepository> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string not found");
        }

        public async Task<List<AlerteModel>> GetUnprocessedAlertsAsync(CancellationToken cancellationToken = default)
        {
            var alerts = new List<AlerteModel>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT AlerteId, AlertTypeId, DestinataireId, PlateformeEnvoieId, StatutId,
                       TitreAlerte, DescriptionAlerte, DateCreationAlerte, ProcessedByWorker
                FROM dbo.Alerte 
                WHERE StatutId IN (1,4) AND (ProcessedByWorker = 0 OR ProcessedByWorker IS NULL)
                ORDER BY DateCreationAlerte ASC";

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                alerts.Add(new AlerteModel
                {
                    AlerteId = reader.GetInt32("AlerteId"),
                    AlertTypeId = reader.IsDBNull("AlertTypeId") ? null : reader.GetInt32("AlertTypeId"),
                    DestinataireId = reader.IsDBNull("DestinataireId") ? null : reader.GetInt32("DestinataireId"),
                    PlateformeEnvoieId = reader.IsDBNull("PlateformeEnvoieId") ? null : reader.GetInt32("PlateformeEnvoieId"),
                    TitreAlerte = reader.GetString("TitreAlerte") ?? string.Empty,
                    DescriptionAlerte = reader.GetString("DescriptionAlerte") ?? string.Empty,
                    DateCreationAlerte = reader.GetDateTime("DateCreationAlerte"),
                    StatutId = reader.GetInt32("StatutId"),
                    ProcessedByWorker = reader.IsDBNull("ProcessedByWorker") ? false : reader.GetBoolean("ProcessedByWorker")
                });
            }

            return alerts;
        }

        public async Task<List<UserModel>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
        {
            var users = new List<UserModel>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT UserId, FullName, Email, PhoneNumber, DesktopDeviceToken, IsActive
                FROM dbo.Users 
                WHERE IsActive = 1";

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                users.Add(new UserModel
                {
                    UserId = reader.GetInt32("UserId"),
                    FullName = reader.GetString("FullName") ?? string.Empty,
                    Email = reader.IsDBNull("Email") ? null : reader.GetString("Email"),
                    PhoneNumber = reader.IsDBNull("PhoneNumber") ? null : reader.GetString("PhoneNumber"),
                    DesktopDeviceToken = reader.IsDBNull("DesktopDeviceToken") ? null : reader.GetString("DesktopDeviceToken"),
                    IsActive = reader.GetBoolean("IsActive")
                });
            }

            return users;
        }

        public async Task<UserModel?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT UserId, FullName, Email, PhoneNumber, DesktopDeviceToken, IsActive
                FROM dbo.Users 
                WHERE UserId = @UserId AND IsActive = 1";
            
            command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return new UserModel
                {
                    UserId = reader.GetInt32("UserId"),
                    FullName = reader.GetString("FullName") ?? string.Empty,
                    Email = reader.IsDBNull("Email") ? null : reader.GetString("Email"),
                    PhoneNumber = reader.IsDBNull("PhoneNumber") ? null : reader.GetString("PhoneNumber"),
                    DesktopDeviceToken = reader.IsDBNull("DesktopDeviceToken") ? null : reader.GetString("DesktopDeviceToken"),
                    IsActive = reader.GetBoolean("IsActive")
                };
            }

            return null;
        }

        public async Task MarkAlertAsProcessedAsync(int alerteId, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = @"UPDATE dbo.Alerte 
                                    SET ProcessedByWorker = 1, StatutId = 2  -- Envoyé
                                    WHERE AlerteId = @AlerteId";
            command.Parameters.Add(new SqlParameter("@AlerteId", SqlDbType.Int) { Value = alerteId });

            await command.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Marked alert {AlerteId} as processed", alerteId);
        }

        public async Task MarkAlertAsFailedAsync(int alerteId, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = @"UPDATE dbo.Alerte 
                                    SET ProcessedByWorker = 0, StatutId = 4  -- Échoué
                                    WHERE AlerteId = @AlerteId";
            command.Parameters.Add(new SqlParameter("@AlerteId", SqlDbType.Int) { Value = alerteId });

            await command.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogWarning("Marked alert {AlerteId} as failed", alerteId);
        }

        public async Task CreateHistoriqueAlerteAsync(int alerteId, int userId, string email, string phoneNumber, string desktopToken, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO dbo.HistoriqueAlerte 
                (AlerteId, DestinataireUserId, EtatAlerteId, DateLecture, RappelSuivant, 
                 DestinataireEmail, DestinatairePhoneNumber, DestinataireDesktop)
                VALUES 
                (@AlerteId, @UserId, @EtatAlerteId, NULL, NULL, @Email, @PhoneNumber, @DesktopToken)";

            command.Parameters.Add(new SqlParameter("@AlerteId", SqlDbType.Int) { Value = alerteId });
            command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
            command.Parameters.Add(new SqlParameter("@EtatAlerteId", SqlDbType.Int) { Value = 1 }); // 1 = Non Lu
            command.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar) { Value = email ?? (object)DBNull.Value });
            command.Parameters.Add(new SqlParameter("@PhoneNumber", SqlDbType.NVarChar) { Value = phoneNumber ?? (object)DBNull.Value });
            command.Parameters.Add(new SqlParameter("@DesktopToken", SqlDbType.NVarChar) { Value = desktopToken ?? (object)DBNull.Value });

            await command.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogDebug("Created HistoriqueAlerte for alert {AlerteId} and user {UserId}", alerteId, userId);
        }

        public async Task<List<AlerteModel>> GetReminderAlertsAsync(CancellationToken cancellationToken = default)
        {
            var alerts = new List<AlerteModel>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT DISTINCT a.AlerteId, a.AlertTypeId, a.DestinataireId, a.PlateformeEnvoieId, a.StatutId,
                       a.TitreAlerte, a.DescriptionAlerte, a.DateCreationAlerte, a.ProcessedByWorker
                FROM dbo.Alerte a
                INNER JOIN dbo.HistoriqueAlerte h ON a.AlerteId = h.AlerteId
                WHERE a.AlertTypeId = 2  -- acquittementNécessaire
                  AND h.RappelSuivant <= GETUTCDATE()
                  AND h.EtatAlerteId = 1  -- Non Lu
                  AND a.StatutId = 2  -- Envoyé
                ORDER BY a.DateCreationAlerte ASC";

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                alerts.Add(new AlerteModel
                {
                    AlerteId = reader.GetInt32("AlerteId"),
                    AlertTypeId = reader.IsDBNull("AlertTypeId") ? null : reader.GetInt32("AlertTypeId"),
                    DestinataireId = reader.IsDBNull("DestinataireId") ? null : reader.GetInt32("DestinataireId"),
                    PlateformeEnvoieId = reader.IsDBNull("PlateformeEnvoieId") ? null : reader.GetInt32("PlateformeEnvoieId"),
                    TitreAlerte = reader.GetString("TitreAlerte") ?? string.Empty,
                    DescriptionAlerte = reader.GetString("DescriptionAlerte") ?? string.Empty,
                    DateCreationAlerte = reader.GetDateTime("DateCreationAlerte"),
                    StatutId = reader.GetInt32("StatutId"),
                    ProcessedByWorker = reader.IsDBNull("ProcessedByWorker") ? false : reader.GetBoolean("ProcessedByWorker")
                });
            }

            return alerts;
        }

        public async Task SetInitialReminderAsync(int alerteId, int intervalMinutes, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE dbo.HistoriqueAlerte 
                SET RappelSuivant = DATEADD(minute, @IntervalMinutes, GETUTCDATE())
                WHERE AlerteId = @AlerteId AND EtatAlerteId = 1";

            command.Parameters.Add(new SqlParameter("@AlerteId", SqlDbType.Int) { Value = alerteId });
            command.Parameters.Add(new SqlParameter("@IntervalMinutes", SqlDbType.Int) { Value = intervalMinutes });

            await command.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogDebug("Set initial reminder for alert {AlerteId} in {IntervalMinutes} minutes", alerteId, intervalMinutes);
        }

        public async Task<bool> UpdateReminderStatusAsync(int alerteId, bool success, int intervalMinutes, int maxAttempts, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = @"
                -- Check if all recipients are confirmed (Lu)
                DECLARE @AllConfirmed BIT = 0;
                DECLARE @UnconfirmedCount INT;
                
                SELECT @UnconfirmedCount = COUNT(*)
                FROM dbo.HistoriqueAlerte 
                WHERE AlerteId = @AlerteId AND EtatAlerteId = 1;
                
                IF @UnconfirmedCount = 0
                    SET @AllConfirmed = 1;
                
                -- If all confirmed, stop reminders
                IF @AllConfirmed = 1
                BEGIN
                    UPDATE dbo.HistoriqueAlerte 
                    SET RappelSuivant = NULL
                    WHERE AlerteId = @AlerteId;
                    
                    SELECT 0; -- Stop reminders
                    RETURN;
                END
                
                -- Update reminder attempts and next reminder time
                UPDATE dbo.HistoriqueAlerte 
                SET RappelSuivant = CASE 
                    WHEN (ISNULL(RappelTentatives, 0) + 1) >= @MaxAttempts THEN NULL  -- Stop if max attempts reached
                    ELSE DATEADD(minute, @IntervalMinutes, GETUTCDATE())  -- Schedule next reminder
                END,
                RappelTentatives = ISNULL(RappelTentatives, 0) + 1
                WHERE AlerteId = @AlerteId AND EtatAlerteId = 1;
                
                -- Return whether to continue (1) or stop (0)
                SELECT CASE WHEN @AllConfirmed = 1 OR (ISNULL(RappelTentatives, 0) + 1) >= @MaxAttempts THEN 0 ELSE 1 END;";

            command.Parameters.Add(new SqlParameter("@AlerteId", SqlDbType.Int) { Value = alerteId });
            command.Parameters.Add(new SqlParameter("@IntervalMinutes", SqlDbType.Int) { Value = intervalMinutes });
            command.Parameters.Add(new SqlParameter("@MaxAttempts", SqlDbType.Int) { Value = maxAttempts });

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToBoolean(result);
        }

        public async Task InsertReminderHistoryAsync(int alerteId, int historiqueAlerteId, bool success, int attemptNumber, string? errorDetails, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO dbo.RappelSuivant 
                (AlerteId, HistoriqueAlerteId, DateRappel, StatutRappel, Tentative, DetailsErreur)
                VALUES 
                (@AlerteId, @HistoriqueAlerteId, GETUTCDATE(), @StatutRappel, @Tentative, @DetailsErreur)";

            command.Parameters.Add(new SqlParameter("@AlerteId", SqlDbType.Int) { Value = alerteId });
            command.Parameters.Add(new SqlParameter("@HistoriqueAlerteId", SqlDbType.Int) { Value = historiqueAlerteId });
            command.Parameters.Add(new SqlParameter("@StatutRappel", SqlDbType.NVarChar, 50) { Value = success ? "Envoyé" : "Échoué" });
            command.Parameters.Add(new SqlParameter("@Tentative", SqlDbType.Int) { Value = attemptNumber });
            command.Parameters.Add(new SqlParameter("@DetailsErreur", SqlDbType.NVarChar) { Value = errorDetails ?? (object)DBNull.Value });

            await command.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogDebug("Inserted reminder history for alert {AlerteId}, recipient {HistoriqueAlerteId}, attempt {Attempt}", alerteId, historiqueAlerteId, attemptNumber);
        }

        public async Task<List<int>> GetUnconfirmedRecipientsAsync(int alerteId, CancellationToken cancellationToken = default)
        {
            var recipients = new List<int>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT DestinataireId
                FROM dbo.HistoriqueAlerte 
                WHERE AlerteId = @AlerteId AND EtatAlerteId = 1";

            command.Parameters.Add(new SqlParameter("@AlerteId", SqlDbType.Int) { Value = alerteId });

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                recipients.Add(reader.GetInt32("DestinataireId"));
            }

            return recipients;
        }
    }
}
