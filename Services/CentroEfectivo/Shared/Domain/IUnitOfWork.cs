namespace VCashApp.Services.CentroEfectivo.Shared.Domain
{
    /// <summary>UoW mínimo usando AppDbContext.</summary>
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync();
    }
}