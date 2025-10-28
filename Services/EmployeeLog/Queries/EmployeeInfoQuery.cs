using VCashApp.Infrastructure.Branches;
using VCashApp.Models.Entities;

namespace VCashApp.Services.EmployeeLog.Queries
{
    public static class EmployeeInfoQuery
    {
        public static IQueryable<AdmEmpleado> ScopedEmployees(
            this IQueryable<AdmEmpleado> q, IBranchContext branch)
        {
            if (branch.AllBranches)
            {
                var ids = branch.PermittedBranchIds;
                return ids.Count == 0 ? q.Where(_ => false) : q.Where(e => e.CodSucursal.HasValue && ids.Contains(e.CodSucursal.Value));
            }
            if (branch.CurrentBranchId.HasValue)
                return q.Where(e => e.CodSucursal == branch.CurrentBranchId.Value);

            return q.Where(_ => false);
        }

        public static IQueryable<AdmEmpleado> ApplySearch(this IQueryable<AdmEmpleado> q, string term)
        {
            term = term.Trim();
            return q.Where(e =>
                e.CodCedula.ToString().Contains(term) ||
                (e.NombreCompleto ?? ((e.PrimerNombre ?? "") + " " + (e.PrimerApellido ?? ""))).Contains(term));
        }
    }
}