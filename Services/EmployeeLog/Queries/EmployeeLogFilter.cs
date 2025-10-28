using VCashApp.Infrastructure.Branches;
using VCashApp.Models.Entities;

namespace VCashApp.Services.EmployeeLog.Queries
{
    public static class EmployeeLogFilter
    {
        public static IQueryable<SegRegistroEmpleado> ApplyBranchScope(
            this IQueryable<SegRegistroEmpleado> q, IBranchContext branch)
        {
            if (branch.AllBranches)
            {
                var ids = branch.PermittedBranchIds;
                return ids.Count == 0 ? q.Where(_ => false) : q.Where(x => ids.Contains(x.CodSucursal));
            }

            if (branch.CurrentBranchId.HasValue)
                return q.Where(x => x.CodSucursal == branch.CurrentBranchId.Value);

            return q.Where(_ => false);
        }

        public static IQueryable<SegRegistroEmpleado> ApplyFilters(
            this IQueryable<SegRegistroEmpleado> q,
            int? cargoId, string? unitId, int? branchId,
            DateOnly? startDate, DateOnly? endDate, int? logStatus, string? search)
        {
            if (branchId.HasValue) q = q.Where(x => x.CodSucursal == branchId.Value);
            if (cargoId.HasValue) q = q.Where(x => x.CodCargo == cargoId.Value);
            if (!string.IsNullOrWhiteSpace(unitId)) q = q.Where(x => x.CodUnidad == unitId);

            if (startDate.HasValue) q = q.Where(x => x.FechaEntrada >= startDate.Value);
            if (endDate.HasValue) q = q.Where(x => x.FechaEntrada <= endDate.Value);

            if (logStatus.HasValue)
            {
                if (logStatus.Value == 0) q = q.Where(x => x.IndicadorEntrada && !x.IndicadorSalida);
                if (logStatus.Value == 1) q = q.Where(x => x.IndicadorEntrada && x.IndicadorSalida);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(x =>
                    x.CodCedula.ToString().Contains(s) ||
                    ((x.PrimerNombreEmpleado ?? "") + " " + (x.PrimerApellidoEmpleado ?? "")).Contains(s));
            }

            return q;
        }
    }
}