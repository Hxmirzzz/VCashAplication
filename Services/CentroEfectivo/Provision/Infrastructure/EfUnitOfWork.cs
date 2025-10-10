using System.Threading.Tasks;
using VCashApp.Data;

namespace VCashApp.Services.CentroEfectivo.Provision.Infrastructure
{
    /// <summary>UoW mínimo usando AppDbContext.</summary>
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync();
    }

    public sealed class EfUnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _db;
        public EfUnitOfWork(AppDbContext db) => _db = db;
        public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
    }
}