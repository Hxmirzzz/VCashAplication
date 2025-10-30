using Microsoft.AspNetCore.Mvc.Rendering;
using VCashApp.Models.ViewModels.Employee;

namespace VCashApp.Services.Employee.Application
{
    /// <summary>
    /// Manejo de consultas de empleados.
    /// </summary>
    public interface IEmployeeReadService
    {
        /// <summary>
        /// Obtiene una lista paginada de empleados según los filtros proporcionados.
        /// </summary>
        /// <param name="userId">usuario que realiza la petición</param>
        /// <param name="cargoId">Cargo</param>
        /// <param name="branchId">Sucursal</param>
        /// <param name="employeeStatus">Estado del empleado</param>
        /// <param name="search">búsqueda por nombre o documento</param>
        /// <param name="gender">genero</param>
        /// <param name="page">Pagina</param>
        /// <param name="pageSize">Tamaño de página</param>
        /// <param name="isAdmin">Es administrador</param>
        /// <returns>Tupla con lista de empleados y total de registros</returns>
        Task<(IEnumerable<EmployeeViewModel> Items, int Total)> GetPagedAsync(
            string userId, int? cargoId, int? branchId, int? employeeStatus,
            string? search, string? gender, int page, int pageSize, bool isAdmin);

        /// <summary>
        /// Obtiene los datos de un empleado para edición.
        /// </summary>
        /// <param name="id">ID del empleado</param>
        /// <param name="userId">usuario que realiza la petición</param>
        /// <param name="isAdmin">Es administrador</param>
        /// <returns>Empleado o nulo si no existe</returns>
        Task<EmployeeViewModel?> GetForEditAsync(int id, string userId, bool isAdmin);

        /// <summary>
        /// Obtiene los datos de un empleado para detalle.
        /// </summary>
        /// <param name="id">ID del empleado</param>
        /// <param name="userId">usuario que realiza la petición</param>
        /// <param name="isAdmin">Es administrador</param>
        /// <returns>Empleado o nulo si no existe</returns>
        Task<EmployeeViewModel?> GetForDetailsAsync(int id, string userId, bool isAdmin);

        /// <summary>
        /// Obtiene los datos para los select de cargos, sucursales y ciudades
        /// </summary>
        /// <param name="userId">Usuario que realiza la petición</param>
        /// <param name="isAdmin">Es administrador</param>
        /// <returns>Tupla con listas de select items para cargos, sucursales y ciudades</returns>
        Task<(List<SelectListItem> Cargos, List<SelectListItem> Sucursales, List<SelectListItem> Ciudades)>
            GetLookupsAsync(string userId, bool isAdmin);
    }
}
