using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlertSystem.Data;
using AlertSystem.Entities.Entities;

namespace AlertSystem.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class HistoriqueAlerteController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public HistoriqueAlerteController(ApplicationDbContext db) { _db = db; }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var data = await _db.HistoriqueAlertes
                .Select(h => new {
                    h.DestinataireId,
                    h.AlerteId,
                    h.DestinataireUserId,
                    etatAlerteId = h.EtatAlerteId,
                    h.DateLecture
                })
                .ToListAsync();
            return Ok(data);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> Stats()
        {
            var groups = await _db.HistoriqueAlertes
                .GroupBy(h => h.AlerteId)
                .Select(g => new {
                    alerteId = g.Key,
                    alertesLues = g.Count(h => h.EtatAlerteId == 2),
                    alertesNonLues = g.Count(h => h.EtatAlerteId == 1),
                    tauxLecture = g.Count() > 0 ? (double)g.Count(h => h.EtatAlerteId == 2) / g.Count() * 100 : 0
                })
                .ToListAsync();
            return Ok(groups);
        }

        [HttpPost("mark-read/{destId}")]
        public async Task<IActionResult> MarkRead(int destId)
        {
            var historique = await _db.HistoriqueAlertes.FirstOrDefaultAsync(h => h.DestinataireId == destId);
            if (historique == null) return NotFound();
            if (historique.EtatAlerteId == 2)
                return Ok();
            historique.EtatAlerteId = 2; // Lu
            historique.DateLecture = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("by-alert/{alerteId}")]
        public async Task<IActionResult> ByAlert(int alerteId)
        {
            var list = await _db.HistoriqueAlertes
                .Where(h => h.AlerteId == alerteId && h.EtatAlerteId == 1)
                .ToListAsync();
            return Ok(list);
        }
    }
}
