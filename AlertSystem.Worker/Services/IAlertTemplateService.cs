using System.Text.Json;

namespace AlertSystem.Worker.Services
{
    public interface IAlertTemplateService
    {
        (string Title, string Description) Resolve(int domaineId, string etat, JsonElement payload);
    }
}


