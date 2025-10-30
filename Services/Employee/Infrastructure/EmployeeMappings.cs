using VCashApp.Models.Entities;
using VCashApp.Enums;
using VCashApp.Models.ViewModels.Employee;

namespace VCashApp.Services.Employee.Infrastructure
{
    /// <summary>
    /// Mapeos entre la entidad AdmEmpleado y el ViewModel EmployeeViewModel.
    /// </summary>
    public static class EmployeeMappings
    {
        /// <summary>
        /// Vuelve un EmployeeViewModel a partir de una entidad AdmEmpleado.
        /// </summary>
        /// <param name="e">Entidad AdmEmpleado</param>
        /// <returns>ViewModel EmployeeViewModel</returns>
        public static EmployeeViewModel ToViewModel(this AdmEmpleado e)
        {
            return new EmployeeViewModel
            {
                CodCedula = e.CodCedula,
                TipoDocumento = e.TipoDocumento,
                FirstName = e.PrimerNombre,
                MiddleName = e.SegundoNombre,
                FirstLastName = e.PrimerApellido,
                SecondLastName = e.SegundoApellido,
                NombreCompleto = e.NombreCompleto,
                NumeroCarnet = e.NumeroCarnet,
                FechaNacimiento = e.FechaNacimiento,
                FechaExpedicion = e.FechaExpedicion,
                CiudadExpedicion = e.CiudadExpedicion,

                CargoCode = e.CodCargo,
                NombreCargo = e.Cargo?.NombreCargo,
                NombreUnidad = e.Cargo?.Unidad?.NombreUnidad,

                BranchCode = e.CodSucursal,
                NombreSucursal = e.Sucursal?.NombreSucursal,

                Celular = e.Celular,
                Direccion = e.Direccion,
                Correo = e.Correo,
                BloodType = e.RH,
                Genero = e.Genero,
                OtroGenero = e.OtroGenero,
                FechaVinculacion = e.FecVinculacion,
                FechaRetiro = e.FecRetiro,

                IndicadorCatalogo = e.IndicadorCatalogo,
                IngresoRepublica = e.IngresoRepublica,
                IngresoAeropuerto = e.IngresoAeropuerto,

                PhotoPath = e.FotoUrl,
                SignaturePath = e.FirmaUrl,
                EmployeeStatus = (int?)e.EmpleadoEstado
            };
        }

        /// <summary>
        /// Actualiza la entidad AdmEmpleado con los datos del ViewModel EmployeeViewModel
        /// </summary>
        /// <param name="e">Entidad AdmEmpleado a actualizar</param>
        /// <param name="vm">ViewModel con los datos actualizados</param>
        public static void UpdateEntity(this AdmEmpleado e, EmployeeViewModel vm)
        {
            e.TipoDocumento = vm.TipoDocumento;
            e.PrimerNombre = (vm.FirstName ?? "").Trim().ToUpperInvariant();
            e.SegundoNombre = (vm.MiddleName ?? "").Trim().ToUpperInvariant();
            e.PrimerApellido = (vm.FirstLastName ?? "").Trim().ToUpperInvariant();
            e.SegundoApellido = (vm.SecondLastName ?? "").Trim().ToUpperInvariant();
            e.NombreCompleto = BuildFullName(
                e.PrimerNombre, e.SegundoNombre,
                e.PrimerApellido, e.SegundoApellido
            );
            e.NumeroCarnet = vm.NumeroCarnet;
            e.FechaNacimiento = vm.FechaNacimiento;
            e.FechaExpedicion = vm.FechaExpedicion;
            e.CiudadExpedicion = vm.CiudadExpedicion;
            e.CodCargo = vm.CargoCode;
            e.CodSucursal = vm.BranchCode;
            e.Celular = vm.Celular;
            e.Direccion = vm.Direccion;
            e.Correo = vm.Correo;
            e.RH = vm.BloodType;
            e.Genero = vm.Genero;
            e.OtroGenero = vm.OtroGenero;
            e.FecVinculacion = vm.FechaVinculacion;
            e.FecRetiro = vm.FechaRetiro;
            e.IndicadorCatalogo = vm.IndicadorCatalogo;
            e.IngresoRepublica = vm.IngresoRepublica;
            e.IngresoAeropuerto = vm.IngresoAeropuerto;
            e.FotoUrl = vm.PhotoPath;
            e.FirmaUrl = vm.SignaturePath;
            e.EmpleadoEstado = (EstadoEmpleado?)vm.EmployeeStatus;
        }

        private static string BuildFullName(string pNombre, string sNombre, string pApellido, string sApellido)
        {
            var parts = new[] { pNombre, sNombre, pApellido, sApellido }
                .Where(x => !string.IsNullOrEmpty(x));
            return string.Join(" ", parts);
        }
    }
}
