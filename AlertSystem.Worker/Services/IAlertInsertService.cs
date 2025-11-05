using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AlertSystem.Worker.Services
{
    public sealed class AlertIntent
    {
        public int DomaineId { get; set; }
        public int TypeId { get; set; } = 1; // 1=Information
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IReadOnlyList<(int PlatformId, string Recipient)> Deliveries { get; set; } = Array.Empty<(int,string)>();
    }

    public interface IAlertInsertService
    {
        Task InsertAsync(AlertIntent intent, CancellationToken ct);
    }
}


