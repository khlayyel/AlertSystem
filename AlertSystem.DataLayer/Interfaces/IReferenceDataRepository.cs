using AlertSystem.Entities.Entities;

namespace AlertSystem.DataLayer.Interfaces
{
    /// <summary>
    /// Repository interface for reference data operations
    /// </summary>
    public interface IReferenceDataRepository
    {
        Task<IEnumerable<DefTypeEnvoie>> GetAllAlertTypesAsync();
        Task<IEnumerable<Statut>> GetAllStatutsAsync();
        Task<IEnumerable<Etat>> GetAllEtatsAsync();
        Task<IEnumerable<PlateformeEnvoie>> GetAllPlateformeEnvoiesAsync();
        Task<DefTypeEnvoie?> GetAlertTypeByIdAsync(int id);
        Task<Statut?> GetStatutByIdAsync(int id);
        Task<Etat?> GetEtatByIdAsync(int id);
        Task<PlateformeEnvoie?> GetPlateformeEnvoieByIdAsync(int id);
    }
}
