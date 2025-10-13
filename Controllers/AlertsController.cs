using Microsoft.AspNetCore.Mvc;
using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Controllers
{
    public sealed class AlertsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AlertsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            try
            {
                // Count unread alerts in the new model
                var count = await _db.Destinataire
                    .Where(d => d.EtatAlerte == "Non Lu" || d.DateLecture == null)
                    .CountAsync();
                
                return Json(count);
            }
            catch (Exception ex)
            {
                return Json(0);
            }
        }

        [HttpGet]
        public async Task<IActionResult> TodayCount()
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                
                var count = await _db.Alerte
                    .Where(a => a.DateCreationAlerte >= today && a.DateCreationAlerte < tomorrow)
                    .CountAsync();
                
                return Json(count);
            }
            catch (Exception ex)
            {
                return Json(0);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmedMandatoryCount()
        {
            try
            {
                // Count confirmed mandatory alerts
                var count = await _db.Destinataire
                    .Join(_db.Alerte, d => d.AlerteId, a => a.AlerteId, (d, a) => new { d, a })
                    .Join(_db.AlertType, x => x.a.AlertTypeId, at => at.AlertTypeId, (x, at) => new { x.d, x.a, at })
                    .Where(x => x.at.AlertTypeName == "Obligatoire" && 
                               (x.d.EtatAlerte == "Lu" || x.d.DateLecture != null))
                    .CountAsync();
                
                return Json(count);
            }
            catch (Exception ex)
            {
                return Json(0);
            }
        }

        [HttpGet]
        public async Task<IActionResult> HistoryData(string status = "all", int page = 1, int size = 10)
        {
            try
            {
                var query = _db.Alerte
                    .Include(a => a.AlertType)
                    .Include(a => a.Statut)
                    .Include(a => a.Etat)
                    .AsQueryable();

                if (status != "all")
                {
                    query = query.Where(a => a.Etat != null && a.Etat.EtatAlerteName == status);
                }

                var total = await query.CountAsync();
                var alerts = await query
                    .OrderByDescending(a => a.DateCreationAlerte)
                    .Skip((page - 1) * size)
                    .Take(size)
                    .Select(a => new
                    {
                        id = a.AlerteId,
                        title = a.TitreAlerte,
                        message = a.DescriptionAlerte,
                        type = a.AlertType != null ? a.AlertType.AlertTypeName : "Unknown",
                        status = a.Statut != null ? a.Statut.StatutName : "Unknown",
                        state = a.Etat != null ? a.Etat.EtatAlerteName : "Unknown",
                        createdAt = a.DateCreationAlerte,
                        readAt = a.DateLecture
                    })
                    .ToListAsync();

                return Json(new { items = alerts, total, page, size });
            }
            catch (Exception ex)
            {
                return Json(new { items = new object[0], total = 0, page, size });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SentData(int page = 1, int size = 10)
        {
            try
            {
                var query = _db.Alerte
                    .Include(a => a.AlertType)
                    .Include(a => a.Statut)
                    .Include(a => a.Destinataires)
                    .AsQueryable();

                var total = await query.CountAsync();
                var alerts = await query
                    .OrderByDescending(a => a.DateCreationAlerte)
                    .Skip((page - 1) * size)
                    .Take(size)
                    .Select(a => new
                    {
                        id = a.AlerteId,
                        title = a.TitreAlerte,
                        message = a.DescriptionAlerte,
                        type = a.AlertType != null ? a.AlertType.AlertTypeName : "Unknown",
                        status = a.Statut != null ? a.Statut.StatutName : "Unknown",
                        createdAt = a.DateCreationAlerte,
                        recipientCount = a.Destinataires.Count,
                        confirmedCount = a.Destinataires.Count(d => d.EtatAlerte == "Lu" || d.DateLecture != null)
                    })
                    .ToListAsync();

                return Json(new { items = alerts, total, page, size });
            }
            catch (Exception ex)
            {
                return Json(new { items = new object[0], total = 0, page, size });
            }
        }
    }
}
