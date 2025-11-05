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
        public AlertInsertService(ApplicationDbContext db){ _db = db; }

        public async Task InsertAsync(AlertIntent intent, CancellationToken ct)
        {
            var groupId = Guid.NewGuid();
            foreach (var (platform, recipient) in intent.Deliveries)
            {
                await _db.Database.ExecuteSqlRawAsync(@"INSERT INTO dbo.Alerte
                    (AlertGroupId, DomaineId, TypeId, TitreAlerte, DescriptionAlerte, DateCreationAlerte, StatutId, EtatId, PlateformeEnvoieId, Destinataire, ProcessedByWorker, AttemptCount)
                    VALUES (@p0, @p1, @p2, @p3, @p4, SYSUTCDATETIME(), 1, 1, @p5, @p6, 0, 0)",
                    groupId, intent.DomaineId, intent.TypeId, intent.Title, intent.Description, platform, recipient);
            }
        }
    }
}


