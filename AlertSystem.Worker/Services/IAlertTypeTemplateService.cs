using System.Text.Json;

namespace AlertSystem.Worker.Services
{
    public interface IAlertTypeTemplateService
    {
        (string Title, string Description) ResolveTemplate(int typeAlertId, string typeAlerteDescription, JsonElement stockItem);
    }
}

