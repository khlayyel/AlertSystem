using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Controllers.Api.V1
{
    [ApiController]
    [Route("api/v1/alerts/ingest")]
    public sealed class AlertsIngestController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public AlertsIngestController(ApplicationDbContext db){ _db = db; }

        public sealed class IngestPayload
        {
            public int appId { get; set; }
            public JsonElement alertes { get; set; } // array
        }

        [HttpPost]
        public async Task<IActionResult> Ingest([FromBody] IngestPayload payload)
        {
            if (payload == null || payload.appId <= 0 || payload.alertes.ValueKind != JsonValueKind.Array)
            {
                return BadRequest(new { error = "Invalid payload. Expect { appId: <int>, alertes:[...] }" });
            }

            var connString = _db.Database.GetConnectionString();
            var totalInserted = 0;

            await using var conn = new SqlConnection(connString);
            await conn.OpenAsync();

            foreach (var item in payload.alertes.EnumerateArray())
            {
                var nomProduit = item.TryGetProperty("nomProduit", out var np) ? np.GetString() ?? string.Empty : (item.TryGetProperty("Nom produit", out var np2) ? (np2.GetString() ?? string.Empty) : string.Empty);
                var etat = item.TryGetProperty("etat", out var e) ? (e.GetString() ?? string.Empty).ToUpperInvariant() : string.Empty;
                var typeEnvoieId = item.TryGetProperty("typeEnvoieId", out var t) && t.ValueKind == JsonValueKind.Number ? t.GetInt32() : 1;

                string title = "ALERTE";
                string description = string.IsNullOrEmpty(etat) ? nomProduit : (etat switch
                {
                    "EPUISE" => $"L'article {nomProduit} est épuisé.",
                    "MAX" => $"L'article {nomProduit} a atteint son stock maximum.",
                    _ => nomProduit
                });

                var alertGroupId = Guid.NewGuid();
                // Compat: if legacy 'destinataires' exists, use it; otherwise skip (recipients now resolved côté worker)
                var recips = item.TryGetProperty("destinataires", out var d) && d.ValueKind == JsonValueKind.Array
                    ? d.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                    : new List<string>();

                foreach (var r in recips)
                {
                    var platform = r.Contains('@') ? 1 : 2; // 1=email, 2=whatsapp
                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"INSERT INTO dbo.Alerte
                        (AlertGroupId, AppId, TypeEnvoieId, TitreAlerte, DescriptionAlerte, DateCreationAlerte,
                         StatutId, EtatId, PlateformeEnvoieId, Destinataire, ProcessedByWorker, AttemptCount)
                        VALUES (@g, @app, @typ, @tit, @desc, SYSUTCDATETIME(), 1, 1, @plat, @dest, 0, 0)";
                    cmd.Parameters.Add(new SqlParameter("@g", SqlDbType.UniqueIdentifier){ Value = alertGroupId });
                    cmd.Parameters.Add(new SqlParameter("@app", SqlDbType.Int){ Value = payload.appId });
                    cmd.Parameters.Add(new SqlParameter("@typ", SqlDbType.Int){ Value = typeEnvoieId });
                    cmd.Parameters.Add(new SqlParameter("@tit", SqlDbType.NVarChar, 4000){ Value = title });
                    cmd.Parameters.Add(new SqlParameter("@desc", SqlDbType.NVarChar, 4000){ Value = (object?)description ?? DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@plat", SqlDbType.Int){ Value = platform });
                    cmd.Parameters.Add(new SqlParameter("@dest", SqlDbType.NVarChar, 4000){ Value = r });
                    totalInserted += await cmd.ExecuteNonQueryAsync();
                }
            }

            return Ok(new { inserted = totalInserted });
        }
    }
}


