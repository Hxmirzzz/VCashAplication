using Microsoft.EntityFrameworkCore;
using VCashApp.Infrastructure.Branches;
using VCashApp.Models.Entities;

namespace VCashApp.Services.EmployeeLog.Queries
{
    /// <summary>
    /// Punto de composición si luego quieres sumar includes/joins condicionales.
    /// </summary>
    public static class EmployeeLogQueryBuilder
    {
        public static IQueryable<SegRegistroEmpleado> BuildBase(
            IQueryable<SegRegistroEmpleado> source, IBranchContext branch)
        {
            return source.AsNoTracking().ApplyBranchScope(branch);
        }
    }
}