using Microsoft.AspNetCore.Mvc.Rendering;
using VCashApp.Models.AdmEntities;
using VCashApp.Models.ViewModels.Range;

namespace VCashApp.Services
{
    /// <summary>
    /// Interfaz para el servicio de gestión de rangos.
    /// </summary>
    public interface IRangeService
    {
        /// <summary>
        /// Obtiene un listado paginado de rangos, con filtros opcionales por término de búsqueda, cliente y estado activo.
        /// </summary>
        /// <param name="search">Buscador</param>
        /// <param name="clientId">Codigo de Cliente</param>
        /// <param name="rangeStatus">Estado</param>
        /// <param name="page">Pagina</param>
        /// <param name="pageSize">Cantidad por Pagina</param>
        /// <returns>Lista de rangos</returns>
        Task<RangeDashboardViewModel> GetPagedAsync(string? search, int? clientId, bool? rangeStatus, int page, int pageSize);

        /// <summary>
        /// Prepara el formulario para crear rangos.
        /// </summary>
        /// <returns>Vista del formulario</returns>
        Task<RangeFormViewModel> PrepareCreateAsync();

        /// <summary>
        /// Crea un nuevo rango en la base de datos.
        /// </summary>
        /// <param name="vm">Identificador para cada valor en ViewModel</param>
        /// <returns></returns>
        Task<(bool ok, string? message, int? id)> CreateAsync(RangeFormViewModel vm);

        /// <summary>
        /// Prepara el formulario para editar un rango existente.
        /// </summary>
        /// <param name="id">Identificador del rango</param>
        /// <returns>Vista del formulario con datos cargados</returns>
        Task<RangeFormViewModel?> PrepareEditAsync(int id);

        /// <summary>
        /// Edita un rango existente en la base de datos.
        /// </summary>
        /// <param name="vm">Identificador para cada valor en ViewModel</param>
        /// <returns></returns>
        Task<(bool ok, string? message)> UpdateAsync(RangeFormViewModel vm);

        /// <summary>
        /// Obtiene un rango por su ID.
        /// </summary>
        /// <param name="id">Identificador del rangos</param>
        /// <returns>Rango o nulo si no se encuentra</returns>
        Task<AdmRange?> GetByIdAsync(int id);

        /// <summary>
        /// Accion para eliminar un rango.
        /// </summary>
        /// <param name="id">Identificador del rango</param>
        /// <returns></returns>
        Task<(bool ok, string? message)> DeleteAsync(int id);

        /// <summary>
        /// Obtiene una lista de clientes para un dropdown.
        /// </summary>
        /// <returns>Lista de clientes</returns>
        Task<List<SelectListItem>> GetClientsForDropdownAsync(int? selected = null);

        /// <summary>
        /// Valida que el código de rango sea único para el cliente especificado.
        /// </summary>
        /// <param name="vm">Variable definida para el ViewModel</param>
        /// <returns></returns>
        Task<(bool ok, string? message)> ValidateUniqueAsync(RangeFormViewModel vm);
    }
}
