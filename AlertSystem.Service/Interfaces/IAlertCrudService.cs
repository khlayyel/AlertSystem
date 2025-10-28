using System.Threading.Tasks;

namespace AlertSystem.Service
{
    public interface IAlertCrudService
    {
        Task<object[]> GetQuickListAsync();
        Task<(bool Success, int AlertId, string? Error)> CreateFromTemplateAsync(string title, string message, string type);
    }
}


