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
                SELECT AlerteId, AlertTypeId, DestinataireId, PlateformeEnvoieId, 
                       TitreAlerte, DescriptionAlerte, DateCreationAlerte, ProcessedByWorker
                FROM dbo.Alerte 
                WHERE ProcessedByWorker = 0 
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
                    ProcessedByWorker = reader.GetBoolean("ProcessedByWorker")
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
            command.CommandText = "UPDATE dbo.Alerte SET ProcessedByWorker = 1 WHERE AlerteId = @AlerteId";
            command.Parameters.Add(new SqlParameter("@AlerteId", SqlDbType.Int) { Value = alerteId });

            await command.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Marked alert {AlerteId} as processed", alerteId);
        }

        public async Task CreateHistoriqueAlerteAsync(int alerteId, int userId, string email, string phoneNumber, string desktopToken, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO dbo.HistoriqueAlerte 
                (AlerteId, DestinataireUserId, EtatAlerte, DateLecture, RappelSuivant, 
                 DestinataireEmail, DestinatairePhoneNumber, DestinataireDesktop)
                VALUES 
                (@AlerteId, @UserId, 'Non Lu', NULL, NULL, @Email, @PhoneNumber, @DesktopToken)";

            command.Parameters.Add(new SqlParameter("@AlerteId", SqlDbType.Int) { Value = alerteId });
            command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
            command.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar) { Value = email ?? (object)DBNull.Value });
            command.Parameters.Add(new SqlParameter("@PhoneNumber", SqlDbType.NVarChar) { Value = phoneNumber ?? (object)DBNull.Value });
            command.Parameters.Add(new SqlParameter("@DesktopToken", SqlDbType.NVarChar) { Value = desktopToken ?? (object)DBNull.Value });

            await command.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogDebug("Created HistoriqueAlerte for alert {AlerteId} and user {UserId}", alerteId, userId);
        }
    }
}
