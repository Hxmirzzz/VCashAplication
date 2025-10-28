using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.EmployeeLog;

namespace VCashApp.Services.EmployeeLog.Queries
{
    public static class EmployeeLogOrdering
    {
        public static IQueryable<SegRegistroEmpleado> ApplyDefaultOrdering(this IQueryable<SegRegistroEmpleado> q)
            => q.OrderByDescending(x => x.FechaEntrada).ThenByDescending(x => x.HoraEntrada);

        public static IQueryable<EmployeeLogSummaryViewModel> ToSummary(this IQueryable<SegRegistroEmpleado> q)
            => q.Select(x => new EmployeeLogSummaryViewModel
            {
                Id = x.Id,
                CodCedula = x.CodCedula,
                EmpleadoNombre = ((x.PrimerNombreEmpleado ?? "") + " " + (x.PrimerApellidoEmpleado ?? "")).Trim(),
                NombreCargo = x.NombreCargoEmpleado,
                NombreUnidad = x.NombreUnidadEmpleado,
                NombreSucursal = x.NombreSucursalEmpleado,
                FechaEntrada = x.FechaEntrada,
                HoraEntrada = x.HoraEntrada,
                FechaSalida = x.FechaSalida,
                HoraSalida = x.HoraSalida,
                IndicadorEntrada = x.IndicadorEntrada,
                IndicadorSalida = x.IndicadorSalida
            });
    }
}