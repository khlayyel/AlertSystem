using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AlertSystem.Controllers.Api.V1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public sealed class AlertsController : ControllerBase
    {
        private readonly IConfiguration _cfg;
        private string Conn => _cfg.GetConnectionString("DefaultConnection") ?? "";

        public AlertsController(IConfiguration cfg){ _cfg = cfg; }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] int? domaineId, [FromQuery] int? statutId, [FromQuery] int? etatId,
                                                 [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            await using var conn = new SqlConnection(Conn);
            await conn.OpenAsync();

            var where = new List<string>();
            if (domaineId.HasValue) where.Add("DomaineId = @d");
            if (statutId.HasValue) where.Add("StatutId = @s");
            if (etatId.HasValue) where.Add("EtatId = @e");
            var whereSql = where.Count > 0 ? (" WHERE " + string.Join(" AND ", where)) : "";

            // total
            await using (var cmdCount = conn.CreateCommand())
            {
                cmdCount.CommandText = $"SELECT COUNT(1) FROM dbo.Alerte{whereSql}";
                if (domaineId.HasValue) cmdCount.Parameters.Add(new SqlParameter("@d", SqlDbType.Int){ Value = domaineId.Value });
                if (statutId.HasValue) cmdCount.Parameters.Add(new SqlParameter("@s", SqlDbType.Int){ Value = statutId.Value });
                if (etatId.HasValue) cmdCount.Parameters.Add(new SqlParameter("@e", SqlDbType.Int){ Value = etatId.Value });
                var total = (int) (await cmdCount.ExecuteScalarAsync() ?? 0);

                // page
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = $@"SELECT AlertRecordId, AlertGroupId, DomaineId, TypeId, TitreAlerte, DescriptionAlerte,
                                                DateCreationAlerte, StatutId, EtatId, PlateformeEnvoieId, Destinataire,
                                                DateLecture, RappelSuivant, ProcessedByWorker, AttemptCount
                                        FROM dbo.Alerte{whereSql}
                                        ORDER BY DateCreationAlerte DESC
                                        OFFSET @off ROWS FETCH NEXT @ps ROWS ONLY";
                if (domaineId.HasValue) cmd.Parameters.Add(new SqlParameter("@d", SqlDbType.Int){ Value = domaineId.Value });
                if (statutId.HasValue) cmd.Parameters.Add(new SqlParameter("@s", SqlDbType.Int){ Value = statutId.Value });
                if (etatId.HasValue) cmd.Parameters.Add(new SqlParameter("@e", SqlDbType.Int){ Value = etatId.Value });
                cmd.Parameters.Add(new SqlParameter("@off", SqlDbType.Int){ Value = Math.Max(0,(page-1)*pageSize) });
                cmd.Parameters.Add(new SqlParameter("@ps", SqlDbType.Int){ Value = Math.Max(1,pageSize) });

                var rows = new List<object>();
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    rows.Add(new {
                        alertRecordId = r.GetInt64(0),
                        alertGroupId = r.GetGuid(1),
                        domaineId = r.GetInt32(2),
                        typeId = r.GetInt32(3),
                        titre = r.GetString(4),
                        description = r.IsDBNull(5)?null:r.GetString(5),
                        dateCreation = r.GetDateTime(6),
                        statutId = r.GetInt32(7),
                        etatId = r.GetInt32(8),
                        plateformeEnvoieId = r.GetInt32(9),
                        destinataire = r.GetString(10),
                        dateLecture = r.IsDBNull(11)? (DateTime?)null : r.GetDateTime(11),
                        rappelSuivant = r.IsDBNull(12)? (DateTime?)null : r.GetDateTime(12),
                        processedByWorker = r.GetBoolean(13),
                        attemptCount = r.GetInt32(14)
                    });
                }

                return Ok(new { total, page, pageSize, data = rows });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            await using var conn = new SqlConnection(Conn);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT AlertRecordId, AlertGroupId, DomaineId, TypeId, TitreAlerte, DescriptionAlerte,
                                         DateCreationAlerte, StatutId, EtatId, PlateformeEnvoieId, Destinataire,
                                         DateLecture, RappelSuivant, ProcessedByWorker, AttemptCount
                                  FROM dbo.Alerte WHERE AlertRecordId=@id";
            cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.BigInt){ Value = id });
            await using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return NotFound();
            var row = new {
                alertRecordId = r.GetInt64(0),
                alertGroupId = r.GetGuid(1),
                domaineId = r.GetInt32(2),
                typeId = r.GetInt32(3),
                titre = r.GetString(4),
                description = r.IsDBNull(5)?null:r.GetString(5),
                dateCreation = r.GetDateTime(6),
                statutId = r.GetInt32(7),
                etatId = r.GetInt32(8),
                plateformeEnvoieId = r.GetInt32(9),
                destinataire = r.GetString(10),
                dateLecture = r.IsDBNull(11)? (DateTime?)null : r.GetDateTime(11),
                rappelSuivant = r.IsDBNull(12)? (DateTime?)null : r.GetDateTime(12),
                processedByWorker = r.GetBoolean(13),
                attemptCount = r.GetInt32(14)
            };
            return Ok(row);
        }

        public sealed class UpdateStateDto { public int targetEtatId { get; set; } }

        [HttpPost("{id}/update-state")]
        public async Task<IActionResult> UpdateState(long id, [FromBody] UpdateStateDto dto)
        {
            await using var conn = new SqlConnection(Conn);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE dbo.Alerte SET EtatId=@e, DateLecture = CASE WHEN @e=2 THEN SYSUTCDATETIME() ELSE DateLecture END WHERE AlertRecordId=@id";
            cmd.Parameters.Add(new SqlParameter("@e", SqlDbType.Int){ Value = dto.targetEtatId });
            cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.BigInt){ Value = id });
            var n = await cmd.ExecuteNonQueryAsync();
            return Ok(new { success = n>0 });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancel(long id)
        {
            await using var conn = new SqlConnection(Conn);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE dbo.Alerte SET StatutId = 3 WHERE AlertRecordId=@id";
            cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.BigInt){ Value = id });
            var n = await cmd.ExecuteNonQueryAsync();
            return Ok(new { success = n>0 });
        }
    }
}
