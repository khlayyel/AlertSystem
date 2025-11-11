using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AlertSystem.Worker.Services
{
    public interface IDefAlerteService
    {
        Task<List<DefAlerteModel>> GetActiveAlertesAsync(CancellationToken ct);
    }

    public class DefAlerteModel
    {
        public int DefAlerteId { get; set; }
        public int DefTypeAlerte { get; set; }
        public List<int> DestinatairesId { get; set; } = new();
        public string URL { get; set; } = string.Empty;
        public string TypeAlerteDescription { get; set; } = string.Empty;
        public int AppId { get; set; }
    }
}

