using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using AlertSystem.Utils.Abstractions;
using Microsoft.Extensions.Configuration;
using AlertSystem.Service.Services;

namespace AlertSystem.Worker.Watchers
{
    public sealed class AlertSenderWorker : BackgroundService
    {
        private readonly ILogger<AlertSenderWorker> _logger;
        private readonly ApplicationDbContext _db;
        private readonly AlertSystem.Utils.Abstractions.INotificationService _notificationService;
        private readonly AlertSystem.Service.Services.IEmailTemplateService _emailTemplateService;
        private readonly AlertSystem.Service.Services.IWhatsAppTemplateService _whatsAppTemplateService;
        private readonly IConfiguration _configuration;
        private readonly ConfirmationTokenService _confirmationTokenService;

        public AlertSenderWorker(ILogger<AlertSenderWorker> logger,
                                 ApplicationDbContext db,
                                 AlertSystem.Utils.Abstractions.INotificationService notificationService,
                                 AlertSystem.Service.Services.IEmailTemplateService emailTemplateService,
                                 AlertSystem.Service.Services.IWhatsAppTemplateService whatsAppTemplateService,
                                 IConfiguration configuration,
                                 ConfirmationTokenService confirmationTokenService)
        {
            _logger = logger;
            _db = db;
            _notificationService = notificationService;
            _emailTemplateService = emailTemplateService;
            _whatsAppTemplateService = whatsAppTemplateService;
            _configuration = configuration;
            _confirmationTokenService = confirmationTokenService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AlertSenderWorker started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Use raw SQL against the Alerte table defined in create_alert_db.sql
                    var cs = _db.Database.GetConnectionString();
                    await using DbConnection conn = new SqlConnection(cs);
                    if (conn.State != System.Data.ConnectionState.Open)
                        await conn.OpenAsync(stoppingToken);
                    await using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"SELECT TOP (25) AlertRecordId, PlateformeEnvoieId,
                                                     Destinataire, TitreAlerte, DescriptionAlerte, AttemptCount, TypeEnvoieId
                                           FROM dbo.Alerte
                                           WHERE ProcessedByWorker = 0 AND AttemptCount < 3 AND StatutId = 1
                                           ORDER BY DateCreationAlerte";
                        await using var reader = await cmd.ExecuteReaderAsync(stoppingToken);
                        var items = new List<(long id, int platform, string dest, string title, string? body, int attempts, int typeEnvoieId)>();
                        while (await reader.ReadAsync(stoppingToken))
                        {
                            items.Add((reader.GetInt64(0),
                                       reader.GetInt32(1),
                                       reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                       reader.GetString(3),
                                       reader.IsDBNull(4) ? null : reader.GetString(4),
                                       reader.GetInt32(5),
                                       reader.GetInt32(6)));
                        }

                        foreach (var a in items)
                        {
                            var ok = false;
                            try
                            {
                                switch (a.platform)
                                {
                                    case 1: // Email
                                        if (!string.IsNullOrWhiteSpace(a.dest))
                                        {
                                            var baseUrl = _configuration["App:PublicBaseUrl"] ?? "http://localhost:5002";
                                            var token = _confirmationTokenService.Generate(new ConfirmPayload { AlerteId = (int)a.id, Kind = "email", Value = a.dest });
                                            var confirmUrl = baseUrl.TrimEnd('/') + "/confirm?t=" + token + "&id=" + a.id;
                                            var confirmLabel = a.typeEnvoieId == 2 ? "✅ Confirmer la réception" : "👁️ Marquer comme lu";
                                            var html = _emailTemplateService.CreateAlertEmailTemplate(
                                                a.title,
                                                a.body ?? a.title,
                                                "AlertSystem",
                                                DateTime.Now,
                                                confirmUrl,
                                                confirmLabel);
                                            ok = await _notificationService.SendHtmlEmailAsync(a.dest, a.title, html);
                                            if (ok)
                                            {
                                                _logger.LogInformation("Email sent successfully to {Dest} for AlertRecordId={Id}", a.dest, a.id);
                                            }
                                        }
                                        break;
                                    case 2: // WhatsApp
                                        if (!string.IsNullOrWhiteSpace(a.dest))
                                        {
                                            var baseUrl = _configuration["App:PublicBaseUrl"] ?? "http://localhost:5002";
                                            var token = _confirmationTokenService.Generate(new ConfirmPayload { AlerteId = (int)a.id, Kind = "wa", Value = a.dest });
                                            var confirmUrl = baseUrl.TrimEnd('/') + "/confirm?t=" + token + "&id=" + a.id;
                                            ok = await _whatsAppTemplateService.SendAlertTemplateAsync(
                                                a.dest,
                                                a.title,
                                                a.body ?? a.title,
                                                "AlertSystem",
                                                confirmUrl,
                                                a.typeEnvoieId == 2); // requiresConfirmation = true if TypeEnvoieId == 2 (Obligatoire)
                                            if (ok)
                                            {
                                                _logger.LogInformation("WhatsApp message sent successfully to {Dest} for AlertRecordId={Id}", a.dest, a.id);
                                            }
                                        }
                                        break;
                                    default:
                                        _logger.LogWarning("Unknown platform {Platform} for AlertRecordId={Id}. Only Email (1) and WhatsApp (2) are supported.", a.platform, a.id);
                                        ok = false;
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Send error AlertRecordId={Id}", a.id);
                                ok = false;
                            }

                            var newAttempts = ok ? a.attempts : a.attempts + 1;
                            var finalStatut = ok ? 2 : (newAttempts >= 3 ? 4 : 1);
                            var processed = ok || newAttempts >= 3 ? 1 : 0;
                            // Use independent connection to avoid DbContext concurrency
                            await using (var upd = conn.CreateCommand())
                            {
                                upd.CommandText = @"UPDATE dbo.Alerte SET StatutId = @s, AttemptCount = @att, ProcessedByWorker = @proc WHERE AlertRecordId = @id";
                                var ps = upd.CreateParameter(); ps.ParameterName = "@s"; ps.Value = finalStatut; upd.Parameters.Add(ps);
                                ps = upd.CreateParameter(); ps.ParameterName = "@att"; ps.Value = newAttempts; upd.Parameters.Add(ps);
                                ps = upd.CreateParameter(); ps.ParameterName = "@proc"; ps.Value = processed; upd.Parameters.Add(ps);
                                ps = upd.CreateParameter(); ps.ParameterName = "@id"; ps.Value = a.id; upd.Parameters.Add(ps);
                                await upd.ExecuteNonQueryAsync(stoppingToken);
                            }

                            if (!ok && newAttempts < 3)
                            {
                                // light backoff before next loop
                                await Task.Delay(TimeSpan.FromSeconds(Math.Min(30, 3 * newAttempts)), stoppingToken);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AlertSenderWorker loop error");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }

            _logger.LogInformation("AlertSenderWorker stopping");
        }
    }
}
