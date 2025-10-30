using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using VCashApp.Infrastructure.Branches;
using VCashApp.Services.Employee.Domain;
using VCashApp.Services.Employee.Infrastructure;
using VCashApp.Models.ViewModels.Employee;

namespace VCashApp.Services.Employee.Application
{
    /// <summary>
    /// Metodo de manejo de consultas de empleados.
    /// </summary>
    public class EmployeeReadService : IEmployeeReadService
    {
        private readonly IEmployeeRepository _repo;
        private readonly IBranchContext _branchCtx;

        /// <summary>
        /// Manejo de consultas de empleados.
        /// </summary>
        /// <param name="repo">Servicio de repositorio de empleados</param>
        /// <param name="branchCtx">Servicio de contexto de sucursal</param>
        public EmployeeReadService(IEmployeeRepository repo, IBranchContext branchCtx)
        {
            _repo = repo;
            _branchCtx = branchCtx;
        }

        /// <summary>
        /// Obtiene una lista paginada de empleados según los filtros proporcionados.s
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
        public async Task<(IEnumerable<EmployeeViewModel> Items, int Total)> GetPagedAsync(
            string userId, int? cargoId, int? branchId, int? employeeStatus,
            string? search, string? gender, int page, int pageSize, bool isAdmin)
        {
            var (items, total) = await _repo.SearchAsync(
                cargoId, branchId, employeeStatus, search, gender, page, pageSize,
                _branchCtx.AllBranches, _branchCtx.CurrentBranchId, _branchCtx.PermittedBranchIds);

            return (items.Select(e => e.ToViewModel()), total);
        }

        /// <summary>
        /// Obtiene los datos de un empleado para edición.
        /// </summary>
        /// <param name="id">Id del empleado</param>
        /// <param name="userId">Usuario que realiza la petición</param>
        /// <param name="isAdmin">Es administrador</param>
        /// <returns>Empleado o nulo si no existe</returns>
        public async Task<EmployeeViewModel?> GetForEditAsync(int id, string userId, bool isAdmin)
        {
            var e = await _repo.GetByIdAsync(id);
            return e?.ToViewModel();
        }

        /// <summary>
        /// Obtiene los datos de un empleado para detalle.
        /// </summary>
        /// <param name="id">Id del empleado</param>
        /// <param name="userId">Usuario que realiza la petición</param>
        /// <param name="isAdmin">Es administrador</param>
        /// <returns>Empleado o nulo si no existe</returns>
        public async Task<EmployeeViewModel?> GetForDetailsAsync(int id, string userId, bool isAdmin)
        {
            var e = await _repo.GetByIdAsync(id);
            return e?.ToViewModel();
        }

        /// <summary>
        /// Obtiene los datos para los select de cargos, sucursales y ciudades
        /// </summary>
        /// <param name="userId">Usuario que realiza la petición</param>
        /// <param name="isAdmin">Es administrador</param>
        /// <returns>Tupla con listas de select items para cargos, sucursales y ciudades</returns>
        public async Task<(List<SelectListItem> Cargos, List<SelectListItem> Sucursales, List<SelectListItem> Ciudades)>
            GetLookupsAsync(string userId, bool isAdmin)
        {
            var cargos = (await _repo.GetCargosAsync())
                .Select(x => new SelectListItem { Value = x.Value.ToString(), Text = x.Text })
                .ToList();

            var sucursales = (await _repo.GetSucursalesAsync(
                    _branchCtx.AllBranches, _branchCtx.CurrentBranchId, _branchCtx.PermittedBranchIds))
                .Select(x => new SelectListItem { Value = x.Value.ToString(), Text = x.Text })
                .ToList();

            var ciudades = (await _repo.GetCiudadesAsync())
                .Select(x => new SelectListItem { Value = x.Value.ToString(), Text = x.Text })
                .ToList();

            return (cargos, sucursales, ciudades);
        }
    }
}
