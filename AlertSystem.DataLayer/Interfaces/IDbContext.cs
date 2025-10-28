using AlertSystem.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace AlertSystem.DataLayer.Interfaces
{
    /// <summary>
    /// Abstraction for database context operations
    /// </summary>
    public interface IDbContext : IDisposable
    {
        DbSet<Alerte> Alerte { get; }
        DbSet<AlertType> AlertType { get; }
        DbSet<Etat> Etat { get; }
        DbSet<Statut> Statut { get; }
        DbSet<RappelSuivant> RappelSuivant { get; }
        DbSet<WebPushSubscription> WebPushSubscriptions { get; }
        DbSet<ApiClient> ApiClients { get; }
        DbSet<DefUtilisateur> DefUtilisateurs { get; }
        DbSet<PlateformeEnvoie> PlateformeEnvoie { get; }
        DbSet<AlertProcessingQueue> AlertProcessingQueue { get; }
        
        DatabaseFacade Database { get; }
        
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
