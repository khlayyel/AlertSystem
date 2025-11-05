using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace AlertSystem.Worker.Services
{
    public sealed class AlertTemplateService : IAlertTemplateService
    {
        private readonly IConfiguration _cfg;
        public AlertTemplateService(IConfiguration cfg){ _cfg = cfg; }

        public (string Title, string Description) Resolve(int domaineId, string etat, JsonElement payload)
        {
            var product = payload.TryGetProperty("nomProduit", out var p) ? p.GetString() ?? string.Empty : string.Empty;
            var domainKey = domaineId == 1 ? "Stock" : "Default";
            var etatKey = (etat ?? string.Empty).ToUpperInvariant();
            var title = _cfg[$"AlertTemplates:{domainKey}:{etatKey}:Title"]
                        ?? _cfg[$"AlertTemplates:{domainKey}:Default:Title"]
                        ?? "Alerte";
            var descTmpl = _cfg[$"AlertTemplates:{domainKey}:{etatKey}:Description"]
                        ?? _cfg[$"AlertTemplates:{domainKey}:Default:Description"]
                        ?? "{nomProduit}";
            var description = descTmpl.Replace("{nomProduit}", product);
            return (title, description);
        }
    }
}


