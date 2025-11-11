using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AlertSystem.Worker.Services
{
    public sealed class DefAlerteService : IDefAlerteService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<DefAlerteService> _logger;

        public DefAlerteService(ApplicationDbContext db, ILogger<DefAlerteService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<DefAlerteModel>> GetActiveAlertesAsync(CancellationToken ct)
        {
            // Charger d'abord les enregistrements nécessaires
            var raw = await (from da in _db.DefAlerte
                             join ta in _db.DefTypeAlerte on da.DefTypeAlerte equals ta.TypeAlertId
                             where da.IsActive
                             select new
                             {
                                 da.DefAlerteId,
                                 da.DefTypeAlerte,
                                 da.URL,
                                 TypeAlerteDescription = ta.Description,
                                 ta.AppId,
                                 da.ListDestinatairesId
                             }).ToListAsync(ct);

            var results = new List<DefAlerteModel>(raw.Count);
            foreach (var r in raw)
            {
                List<int> destIds;
                try
                {
                    destIds = string.IsNullOrWhiteSpace(r.ListDestinatairesId)
                        ? new List<int>()
                        : (JsonSerializer.Deserialize<List<int>>(r.ListDestinatairesId) ?? new List<int>());
                }
                catch
                {
                    destIds = new List<int>();
                }

                results.Add(new DefAlerteModel
                {
                    DefAlerteId = r.DefAlerteId,
                    DefTypeAlerte = r.DefTypeAlerte,
                    URL = r.URL,
                    TypeAlerteDescription = r.TypeAlerteDescription,
                    AppId = r.AppId,
                    DestinatairesId = destIds
                });
            }

            _logger.LogInformation("Found {Count} active def_alerte records", results.Count);
            return results;
        }
    }
}

