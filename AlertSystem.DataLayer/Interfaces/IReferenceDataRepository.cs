using AlertSystem.Entities.Entities;

namespace AlertSystem.DataLayer.Interfaces
{
    /// <summary>
    /// Repository interface for reference data operations
    /// </summary>
    public interface IReferenceDataRepository
    {
        Task<IEnumerable<AlertType>> GetAllAlertTypesAsync();
        Task<IEnumerable<ExpedType>> GetAllExpedTypesAsync();
        Task<IEnumerable<Statut>> GetAllStatutsAsync();
        Task<IEnumerable<Etat>> GetAllEtatsAsync();
        Task<IEnumerable<PlateformeEnvoie>> GetAllPlateformeEnvoiesAsync();
        Task<AlertType?> GetAlertTypeByIdAsync(int id);
        Task<ExpedType?> GetExpedTypeByIdAsync(int id);
        Task<Statut?> GetStatutByIdAsync(int id);
        Task<Etat?> GetEtatByIdAsync(int id);
        Task<PlateformeEnvoie?> GetPlateformeEnvoieByIdAsync(int id);
    }
}
