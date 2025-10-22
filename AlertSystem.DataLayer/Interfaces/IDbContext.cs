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
        DbSet<ExpedType> ExpedType { get; }
        DbSet<Etat> Etat { get; }
        DbSet<Statut> Statut { get; }
        DbSet<HistoriqueAlerte> HistoriqueAlertes { get; }
        DbSet<RappelSuivant> RappelSuivant { get; }
        DbSet<WebPushSubscription> WebPushSubscriptions { get; }
        DbSet<ApiClient> ApiClients { get; }
        DbSet<User> Users { get; }
        DbSet<PlateformeEnvoie> PlateformeEnvoie { get; }
        
        DatabaseFacade Database { get; }
        
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
