using System.Security.Claims;
using AlertSystem.Data;
using AlertSystem.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using AlertSystem.Hubs;

namespace AlertSystem.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public sealed class AlertsController : Controller
	{
        private readonly ApplicationDbContext _db;
        private readonly IHubContext<NotificationsHub> _hub;

        public AlertsController(ApplicationDbContext db, IHubContext<NotificationsHub> hub)
		{
            _db = db;
            _hub = hub;
		}

		[HttpGet]
		public IActionResult History()
		{
			return View();
		}

        [HttpGet]
        public async Task<IActionResult> HistoryData(string? type, string? status, DateTime? from, DateTime? to, int page = 1, int size = 10)
		{
            int userId = await ResolveUserIdAsync();

			var query = from ar in _db.AlertRecipients.AsNoTracking()
					    join a in _db.Alerts.AsNoTracking() on ar.AlertId equals a.AlertId
					    where ar.UserId == userId
					    select new
					    {
						    ar.AlertRecipientId,
						    a.AlertId,
						    a.Title,
						    a.Message,
						    a.AlertType,
						    a.CreatedAt,
						    ar.IsRead,
						    ar.IsConfirmed,
						    ar.ConfirmedAt,
						    ar.LastSentAt,
						    ar.NextReminderAt
					    };

			if (!string.IsNullOrWhiteSpace(type))
			{
				query = query.Where(x => x.AlertType == type);
			}
			if (!string.IsNullOrWhiteSpace(status) && status != "all")
			{
				query = status switch
				{
					"unread" => query.Where(x => !x.IsRead),
					"pending" => query.Where(x => !x.IsConfirmed),
					"confirmed" => query.Where(x => x.IsConfirmed),
					_ => query
				};
			}
			if (from.HasValue) query = query.Where(x => x.CreatedAt >= from.Value);
			if (to.HasValue) query = query.Where(x => x.CreatedAt < to.Value.AddDays(1));

			int total = await query.CountAsync();
			var items = await query
				.OrderByDescending(x => x.CreatedAt)
				.Skip((page - 1) * size)
				.Take(size)
				.ToListAsync();

			return Json(new { items, totalCount = total, page, size });
		}

        // Admin: liste globale avec filtres (département/type/statut/période)
        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> AllData(int? departmentId, string? type, string? status, DateTime? from, DateTime? to, int page = 1, int size = 20)
        {
            var query = from ar in _db.AlertRecipients.AsNoTracking()
                        join a in _db.Alerts.AsNoTracking() on ar.AlertId equals a.AlertId
                        join u in _db.Users.AsNoTracking() on ar.UserId equals u.UserId
                        select new
                        {
                            ar.AlertRecipientId,
                            a.AlertId,
                            a.Title,
                            a.Message,
                            a.AlertType,
                            a.CreatedAt,
                            a.DepartmentId,
                            ar.IsRead,
                            ar.IsConfirmed,
                            ar.ConfirmedAt,
                            u.Username,
                            u.Email
                        };

            if (departmentId.HasValue) query = query.Where(x => x.DepartmentId == departmentId.Value);
            if (!string.IsNullOrWhiteSpace(type)) query = query.Where(x => x.AlertType == type);
            if (!string.IsNullOrWhiteSpace(status) && status != "all")
            {
                query = status switch
                {
                    "unread" => query.Where(x => !x.IsRead),
                    "pending" => query.Where(x => !x.IsConfirmed),
                    "confirmed" => query.Where(x => x.IsConfirmed),
                    _ => query
                };
            }
            if (from.HasValue) query = query.Where(x => x.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(x => x.CreatedAt < to.Value.AddDays(1));

            int total = await query.CountAsync();
            var items = await query.OrderByDescending(x => x.CreatedAt)
                                   .Skip((page - 1) * size)
                                   .Take(size)
                                   .ToListAsync();
            return Json(new { items, totalCount = total, page, size });
        }

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            int userId = await ResolveUserIdAsync();
            if (userId == 0) return Json(new { count = 0 });
            var count = await _db.AlertRecipients
                .AsNoTracking()
                .Where(x => x.UserId == userId && !x.IsRead)
                .CountAsync();
            return Json(new { count });
        }

        // Export CSV de l'historique de l'utilisateur courant (avec filtres)
        [HttpGet]
        public async Task<IActionResult> ExportMineCsv(string? type, string? status, DateTime? from, DateTime? to)
        {
            int userId = await ResolveUserIdAsync();
            var query = from ar in _db.AlertRecipients.AsNoTracking()
                        join a in _db.Alerts.AsNoTracking() on ar.AlertId equals a.AlertId
                        where ar.UserId == userId
                        select new
                        {
                            a.CreatedAt,
                            a.AlertType,
                            a.Title,
                            ar.IsRead,
                            ar.IsConfirmed,
                            ar.ConfirmedAt
                        };

            if (!string.IsNullOrWhiteSpace(type)) query = query.Where(x => x.AlertType == type);
            if (!string.IsNullOrWhiteSpace(status) && status != "all")
            {
                query = status switch
                {
                    "unread" => query.Where(x => !x.IsRead),
                    "pending" => query.Where(x => !x.IsConfirmed),
                    "confirmed" => query.Where(x => x.IsConfirmed),
                    _ => query
                };
            }
            if (from.HasValue) query = query.Where(x => x.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(x => x.CreatedAt < to.Value.AddDays(1));

            var rows = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Date,Type,Titre,Lu,Confirmé,DateConfirmation");
            foreach (var r in rows)
            {
                string line = string.Join(',', new string[] {
                    r.CreatedAt.ToString("s"),
                    Escape(r.AlertType),
                    Escape(r.Title),
                    r.IsRead?"1":"0",
                    r.IsConfirmed?"1":"0",
                    r.ConfirmedAt.HasValue? r.ConfirmedAt.Value.ToString("s") : ""
                });
                sb.AppendLine(line);
            }
            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv; charset=utf-8", $"my_alerts_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");

            static string Escape(string? s)
            {
                if (string.IsNullOrEmpty(s)) return "";
                var needQuote = s.Contains(',') || s.Contains('"') || s.Contains('\n');
                var val = s.Replace("\"", "\"\"");
                return needQuote ? $"\"{val}\"" : val;
            }
        }

        // Admin: export CSV des données globales
        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportCsv(int? departmentId, string? type, string? status, DateTime? from, DateTime? to)
        {
            var query = from ar in _db.AlertRecipients.AsNoTracking()
                        join a in _db.Alerts.AsNoTracking() on ar.AlertId equals a.AlertId
                        join u in _db.Users.AsNoTracking() on ar.UserId equals u.UserId
                        select new
                        {
                            a.CreatedAt,
                            a.AlertType,
                            a.Title,
                            DepartmentId = a.DepartmentId,
                            u.Username,
                            u.Email,
                            ar.IsRead,
                            ar.IsConfirmed,
                            ar.ConfirmedAt
                        };

            if (departmentId.HasValue) query = query.Where(x => x.DepartmentId == departmentId.Value);
            if (!string.IsNullOrWhiteSpace(type)) query = query.Where(x => x.AlertType == type);
            if (!string.IsNullOrWhiteSpace(status) && status != "all")
            {
                query = status switch
                {
                    "unread" => query.Where(x => !x.IsRead),
                    "pending" => query.Where(x => !x.IsConfirmed),
                    "confirmed" => query.Where(x => x.IsConfirmed),
                    _ => query
                };
            }
            if (from.HasValue) query = query.Where(x => x.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(x => x.CreatedAt < to.Value.AddDays(1));

            var rows = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Date,Type,Titre,DepartmentId,UserName,Email,Lu,Confirmé,DateConfirmation");
            foreach (var r in rows)
            {
                string line = string.Join(',', new string[] {
                    r.CreatedAt.ToString("s"),
                    Escape(r.AlertType),
                    Escape(r.Title),
                    (r.DepartmentId?.ToString() ?? ""),
                    Escape(r.Username),
                    Escape(r.Email),
                    r.IsRead?"1":"0",
                    r.IsConfirmed?"1":"0",
                    r.ConfirmedAt.HasValue? r.ConfirmedAt.Value.ToString("s") : ""
                });
                sb.AppendLine(line);
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv; charset=utf-8", $"alerts_export_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");

            static string Escape(string? s)
            {
                if (string.IsNullOrEmpty(s)) return "";
                var needQuote = s.Contains(',') || s.Contains('"') || s.Contains('\n');
                var val = s.Replace("\"", "\"\"");
                return needQuote ? $"\"{val}\"" : val;
            }
        }

		[HttpPost]
        public async Task<IActionResult> Confirm(int alertRecipientId)
		{
            int userId = await ResolveUserIdAsync();
			var row = await _db.AlertRecipients.FirstOrDefaultAsync(x => x.AlertRecipientId == alertRecipientId && x.UserId == userId);
			if (row == null) return BadRequest();
			row.IsConfirmed = true;
			row.IsRead = true;
			row.ConfirmedAt = DateTime.UtcNow;
			await _db.SaveChangesAsync();
            // push updates: recipient and sender
            var alert = await _db.Alerts.AsNoTracking().FirstOrDefaultAsync(a => a.AlertId == row.AlertId);
            await _hub.Clients.User(userId.ToString()).SendAsync("historyChanged");
            if (alert != null)
            {
                await _hub.Clients.User(alert.CreatedBy.ToString()).SendAsync("sentChanged");
                // dept viewers
                await _hub.Clients.All.SendAsync("deptChanged");
                await _hub.Clients.User(userId.ToString()).SendAsync("badgeChanged");
            }
			return Ok();
		}

		[HttpPost]
        public async Task<IActionResult> MarkRead(int alertRecipientId)
		{
            int userId = await ResolveUserIdAsync();
			var row = await _db.AlertRecipients.FirstOrDefaultAsync(x => x.AlertRecipientId == alertRecipientId && x.UserId == userId);
			if (row == null) return BadRequest();
            row.IsRead = true;
            row.IsConfirmed = true;
            if (!row.ConfirmedAt.HasValue)
            {
                row.ConfirmedAt = DateTime.UtcNow;
            }
			await _db.SaveChangesAsync();
            var alert = await _db.Alerts.AsNoTracking().FirstOrDefaultAsync(a => a.AlertId == row.AlertId);
            await _hub.Clients.User(userId.ToString()).SendAsync("historyChanged");
            if (alert != null)
            {
                await _hub.Clients.User(alert.CreatedBy.ToString()).SendAsync("sentChanged");
                await _hub.Clients.All.SendAsync("deptChanged");
                await _hub.Clients.User(userId.ToString()).SendAsync("badgeChanged");
            }
			return Ok();
		}

        private async Task<int> ResolveUserIdAsync()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(claim, out var id)) return id;
            return 0;
        }

        [HttpGet]
        public async Task<IActionResult> SentData(int page = 1, int size = 10)
        {
            int userId = await ResolveUserIdAsync();
            var q = from ar in _db.AlertRecipients.AsNoTracking()
                    join a in _db.Alerts.AsNoTracking() on ar.AlertId equals a.AlertId
                    join uDest in _db.Users.AsNoTracking() on ar.UserId equals uDest.UserId
                    join uSrc in _db.Users.AsNoTracking() on a.CreatedBy equals uSrc.UserId
                    where a.CreatedBy == userId
                    select new
                    {
                        a.CreatedAt,
                        a.AlertType,
                        a.Title,
                        Sender = uSrc.Username,
                        Recipient = uDest.Username,
                        ar.IsRead,
                        ar.ConfirmedAt
                    };

            int total = await q.CountAsync();
            var items = await q.OrderByDescending(x => x.CreatedAt)
                               .Skip((page - 1) * size)
                               .Take(size)
                               .ToListAsync();
            return Json(new { items, totalCount = total, page, size });
        }

        [HttpGet]
        public async Task<IActionResult> DeptData(string? type, string? status, DateTime? from, DateTime? to, int page = 1, int size = 10)
        {
            var depClaim = User.FindFirst("department")?.Value;
            if (!int.TryParse(depClaim, out var depId)) return Json(new { items = Array.Empty<object>(), totalCount = 0, page, size });

            var q = from ar in _db.AlertRecipients.AsNoTracking()
                    join a in _db.Alerts.AsNoTracking() on ar.AlertId equals a.AlertId
                    join uDest in _db.Users.AsNoTracking() on ar.UserId equals uDest.UserId
                    join uSrc in _db.Users.AsNoTracking() on a.CreatedBy equals uSrc.UserId
                    where uDest.DepartmentId == depId
                    select new
                    {
                        a.CreatedAt,
                        a.AlertType,
                        a.Title,
                        Sender = uSrc.Username,
                        Recipient = uDest.Username,
                        ar.IsRead,
                        ar.ConfirmedAt
                    };

            if (!string.IsNullOrWhiteSpace(type)) q = q.Where(x => x.AlertType == type);
            if (!string.IsNullOrWhiteSpace(status) && status != "all")
            {
                q = status switch
                {
                    "unread" => q.Where(x => !x.IsRead),
                    "pending" => q.Where(x => !x.IsRead),
                    "confirmed" => q.Where(x => x.IsRead),
                    _ => q
                };
            }
            if (from.HasValue) q = q.Where(x => x.CreatedAt >= from.Value);
            if (to.HasValue) q = q.Where(x => x.CreatedAt < to.Value.AddDays(1));

            int total = await q.CountAsync();
            var items = await q.OrderByDescending(x => x.CreatedAt)
                               .Skip((page - 1) * size)
                               .Take(size)
                               .ToListAsync();
            return Json(new { items, totalCount = total, page, size });
        }
	}
}
