namespace AlertSystem.DataLayer.Interfaces
{
    /// <summary>
    /// Unit of Work interface for managing transactions and repositories
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        IAlerteRepository Alertes { get; }
        IHotelUserRepository HotelUsers { get; }
        IRappelSuivantRepository RappelSuivants { get; }
        IApiClientRepository ApiClients { get; }
        IReferenceDataRepository ReferenceData { get; }
        
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
