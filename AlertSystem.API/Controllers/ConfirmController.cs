using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlertSystem.Data;
using AlertSystem.Service.Services;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AlertSystem.Controllers.Api.V1
{
    [ApiController]
    [Route("confirm")]
    public sealed class ConfirmController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ConfirmationTokenService _tokens;

        public ConfirmController(ApplicationDbContext db, IConfiguration cfg)
        {
            _db = db;
            _tokens = new ConfirmationTokenService(cfg["TOKEN_SECRET"] ?? "dev-secret-change-me");
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] string t, [FromQuery] long? id)
        {
            if (string.IsNullOrWhiteSpace(t))
            {
                return Content("<h3>Erreur: Token manquant</h3>", "text/html");
            }
            if (!_tokens.TryValidate(t, out var payload))
            {
                return Content("<h3>Erreur: Token invalide ou expiré</h3>", "text/html");
            }

            // Use raw SQL against dbo.Alerte to avoid entity mapping mismatches
            var cs = _db.Database.GetConnectionString();
            int updated = 0;
            await using (var conn = new SqlConnection(cs))
            {
                await conn.OpenAsync();
                // Read base row
                Guid groupId;
                string destinataire;
                int currentEtat;
                await using (var sel = conn.CreateCommand())
                {
                    sel.CommandText = "SELECT AlertGroupId, Destinataire, EtatId FROM dbo.Alerte WHERE AlertRecordId = @id";
                    sel.Parameters.Add(new SqlParameter("@id", SqlDbType.BigInt){ Value = payload.AlerteId });
                    await using var rd = await sel.ExecuteReaderAsync();
                    if (!await rd.ReadAsync())
                    {
                        return Content("<h3>Erreur: Alerte introuvable</h3>", "text/html");
                    }
                    groupId = rd.GetGuid(0);
                    destinataire = rd.IsDBNull(1) ? string.Empty : rd.GetString(1);
                    currentEtat = rd.GetInt32(2);
                }

                var targetEtat = (currentEtat == 3) ? 4 : 2;
                await using (var upd = conn.CreateCommand())
                {
                    upd.CommandText = @"UPDATE dbo.Alerte
                                           SET EtatId = @etat, DateLecture = SYSUTCDATETIME()
                                           WHERE AlertGroupId = @g AND Destinataire = @d AND EtatId <> @etat";
                    upd.Parameters.Add(new SqlParameter("@etat", SqlDbType.Int){ Value = targetEtat });
                    upd.Parameters.Add(new SqlParameter("@g", SqlDbType.UniqueIdentifier){ Value = groupId });
                    upd.Parameters.Add(new SqlParameter("@d", SqlDbType.NVarChar, 4000){ Value = destinataire });
                    updated = await upd.ExecuteNonQueryAsync();
                }
            }

            var html = $@"<!doctype html><html><head><meta charset='utf-8'><title>Confirmation</title>
<style>body{{font-family:Segoe UI,Arial,sans-serif;background:#f5f7fb;margin:0;padding:0}}.card{{max-width:520px;margin:60px auto;background:#fff;border-radius:12px;box-shadow:0 10px 30px rgba(0,0,0,.08);padding:28px}}.title{{font-size:22px;font-weight:700;margin:0 0 10px}}.ok{{color:#16a34a}}.muted{{color:#6b7280}}</style>
</head><body><div class='card'>
  <div class='title ok'>Confirmation enregistrée</div>
  <div class='muted'>Nous avons mis à jour {updated} notification(s) pour ce destinataire.</div>
</div></body></html>";
            return Content(html, "text/html");
        }
    }
}


