using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AlertSystem.Worker.Services
{
    public sealed class AlertIntent
    {
        public int AppId { get; set; }
        public int TypeEnvoieId { get; set; } = 1; // 1=Information, 2=Obligatoire
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public IReadOnlyList<(int PlatformId, string Recipient)> Deliveries { get; set; } = Array.Empty<(int, string)>();
    }

    public interface IAlertInsertService
    {
        Task InsertAsync(AlertIntent intent, CancellationToken ct);
    }
}
