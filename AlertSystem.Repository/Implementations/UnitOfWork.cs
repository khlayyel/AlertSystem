using AlertSystem.DataLayer.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace AlertSystem.Repository.Implementations
{
    /// <summary>
    /// EF Core implementation of IUnitOfWork
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDbContext _context;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(IDbContext context)
        {
            _context = context;
            Alertes = new AlerteRepository(_context);
            HotelUsers = new HotelUserRepository(_context);
            RappelSuivants = new RappelSuivantRepository(_context);
            ApiClients = new ApiClientRepository(_context);
            ReferenceData = new ReferenceDataRepository(_context);
        }

        public IAlerteRepository Alertes { get; }
        public IHotelUserRepository HotelUsers { get; }
        public IRappelSuivantRepository RappelSuivants { get; }
        public IApiClientRepository ApiClients { get; }
        public IReferenceDataRepository ReferenceData { get; }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
