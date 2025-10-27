using VCashApp.Services.CentroEfectivo.Shared.Domain;
using VCashApp.Data;

namespace VCashApp.Services.CentroEfectivo.Shared.Infrastructure
{
    public sealed class EfUnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _db;
        public EfUnitOfWork(AppDbContext db) => _db = db;
        public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
    }
}