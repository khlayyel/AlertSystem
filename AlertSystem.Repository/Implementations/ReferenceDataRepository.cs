using AlertSystem.DataLayer.Interfaces;
using AlertSystem.Entities.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Repository.Implementations
{
    /// <summary>
    /// EF Core implementation of IReferenceDataRepository
    /// </summary>
    public class ReferenceDataRepository : IReferenceDataRepository
    {
        private readonly IDbContext _context;

        public ReferenceDataRepository(IDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AlertType>> GetAllAlertTypesAsync()
        {
            return await _context.AlertType.ToListAsync();
        }

        public async Task<IEnumerable<ExpedType>> GetAllExpedTypesAsync()
        {
            return await _context.ExpedType.ToListAsync();
        }

        public async Task<IEnumerable<Statut>> GetAllStatutsAsync()
        {
            return await _context.Statut.ToListAsync();
        }

        public async Task<IEnumerable<Etat>> GetAllEtatsAsync()
        {
            return await _context.Etat.ToListAsync();
        }

        public async Task<IEnumerable<PlateformeEnvoie>> GetAllPlateformeEnvoiesAsync()
        {
            return await _context.PlateformeEnvoie.ToListAsync();
        }

        public async Task<AlertType?> GetAlertTypeByIdAsync(int id)
        {
            return await _context.AlertType.FindAsync(id);
        }

        public async Task<ExpedType?> GetExpedTypeByIdAsync(int id)
        {
            return await _context.ExpedType.FindAsync(id);
        }

        public async Task<Statut?> GetStatutByIdAsync(int id)
        {
            return await _context.Statut.FindAsync(id);
        }

        public async Task<Etat?> GetEtatByIdAsync(int id)
        {
            return await _context.Etat.FindAsync(id);
        }

        public async Task<PlateformeEnvoie?> GetPlateformeEnvoieByIdAsync(int id)
        {
            return await _context.PlateformeEnvoie.FindAsync(id);
        }
    }
}
