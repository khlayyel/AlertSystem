using System;
using System.Threading;
using System.Threading.Tasks;
using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Worker.Services
{
    public sealed class AlertInsertService : IAlertInsertService
    {
        private readonly ApplicationDbContext _db;
        public AlertInsertService(ApplicationDbContext db) { _db = db; }

        public async Task InsertAsync(AlertIntent intent, CancellationToken ct)
        {
            var groupId = Guid.NewGuid();
            foreach (var (platform, recipient) in intent.Deliveries)
            {
                var desc = string.IsNullOrWhiteSpace(intent.Description) ? (object)DBNull.Value : intent.Description;
                var title = string.IsNullOrWhiteSpace(intent.Title) ? "Alerte" : intent.Title;
                var safeRecipient = string.IsNullOrWhiteSpace(recipient) ? string.Empty : recipient;

                await _db.Database.ExecuteSqlRawAsync(@"INSERT INTO dbo.Alerte
                    (AlertGroupId, AppId, TypeEnvoieId, TitreAlerte, DescriptionAlerte, DateCreationAlerte, StatutId, EtatId, PlateformeEnvoieId, Destinataire, ProcessedByWorker, AttemptCount)
                    VALUES (@p0, @p1, @p2, @p3, @p4, SYSUTCDATETIME(), 1, 1, @p5, @p6, 0, 0)",
                    new object[] { groupId, intent.AppId, intent.TypeEnvoieId, title, desc, platform, safeRecipient });
            }
        }
    }
}
