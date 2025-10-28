using System.Threading.Tasks;

namespace VCashApp.Services.EmployeeLog.Queries
{
    public interface IEmployeeLogLookupsService
    {
        /// <summary>
        /// Retorna los dropdowns (sucursales visibles, cargos, unidades, estados de log)
        /// filtrados por el alcance del usuario (admin vs asignadas / sucursal activa).
        /// </summary>
        Task<EmployeeLogDropdownsDto> GetDropdownListsAsync(string currentUserId, bool isAdmin);
    }
}