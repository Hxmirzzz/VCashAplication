using VCashApp.Models.Dtos.EmployeeLog;
using VCashApp.Models.ViewModels.EmployeeLog;

namespace VCashApp.Extensions
{
    public static class EmployeeLogMappingExtensions
    {
        public static EmployeeLogSummaryViewModel ToSummaryViewModel(this EmployeeLogListDto dto)
        {
            return new EmployeeLogSummaryViewModel
            {
                Id = dto.Id,
                CodCedula = dto.CodCedula,
                EmpleadoNombre = dto.NombreCompletoEmpleado,
                NombreSucursal = dto.NombreSucursalEmpleado,
                NombreCargo = dto.NombreCargoEmpleado,
                NombreUnidad = dto.NombreUnidadEmpleado,

                // Datos del registro
                FechaEntrada = dto.FechaEntrada,
                HoraEntrada = dto.HoraEntrada,
                FechaSalida = dto.FechaSalida,
                HoraSalida = dto.HoraSalida,
                IndicadorEntrada = dto.IndicadorEntrada,
                IndicadorSalida = dto.IndicadorSalida
            };
        }

        public static IEnumerable<EmployeeLogSummaryViewModel> ToSummaryViewModels(
            this IEnumerable<EmployeeLogListDto> dtos)
        {
            return dtos.Select(dto => dto.ToSummaryViewModel());
        }
    }
}