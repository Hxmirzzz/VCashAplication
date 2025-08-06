using Microsoft.AspNetCore.Mvc.Rendering;
using VCashApp.Enums;
using VCashApp.Models.ViewModels.CentroEfectivo;

namespace VCashApp.Services
{
    /// <summary>
    /// Define las operaciones de servicio para la creación unificada temporal de Servicios y Transacciones CEF.
    /// </summary>
    public interface ICefServiceCreationService
    {
        /// <summary>
        /// Prepares a ViewModel for unified Service and CEF Transaction creation, including dropdowns.
        /// Prepara un ViewModel para la creación de un nuevo Servicio y Transacción CEF, incluyendo dropdowns.
        /// </summary>
        /// <param name="currentUserId">ID del usuario actual.</param>
        /// <param name="currentIP">IP address of the current user.</param>
        /// <param name="initialServiceConceptCode">Initial service concept code to pre-fill (e.g., "RC").</param>
        /// <returns>A CefServiceCreationViewModel ready for the form.</returns>
        Task<CefServiceCreationViewModel> PrepareCefServiceCreationViewModelAsync(string currentUserId, string currentIP, string? initialServiceConceptCode = null);

        /// <summary>
        /// Obtiene las listas de dropdowns necesarias para la creación de un servicio y una transacción CEF.
        /// </summary>
        /// <param name="currentUserId">El ID del usuario actual.</param>
        /// <param name="isAdmin">Indica si el usuario es administrador.</param>
        /// <returns>Una tupla con las listas de SelectListItem para Sucursales y Modalidades de Servicio.</returns>
        Task<(List<SelectListItem> AvailableBranches, List<SelectListItem> AvailableServiceModalities, List<SelectListItem> AvailableFailedReponsibles)> GetDropdownListsAsync(string currentUserId, bool isAdmin);


        /// <summary>
        /// Processes the ViewModel, creates a new entry in AdmServicio (CgsService) and CefTransaction transactionally.
        /// Procesa el ViewModel, crea una nueva entrada en AdmServicio y CefTransaction de forma transaccional.
        /// </summary>
        /// <param name="viewModel">ViewModel containing all form data.</param>
        /// <param name="currentUserId">ID of the user performing the operation.</param>
        /// <param name="currentIP">User's IP address.</param>
        /// <returns>The generated Service Order ID.</returns>
        /// <exception cref="InvalidOperationException">Thrown if a business logic error occurs.</exception>
        Task<string> ProcessCefServiceCreationAsync(CefServiceCreationViewModel viewModel, string currentUserId, string currentIP);

        /// <summary>
        /// Gets available service concepts for dropdowns.
        /// Obtiene los conceptos de servicio disponibles para los dropdowns.
        /// </summary>
        /// <returns>List of SelectListItem for service concepts.</returns>
        Task<List<SelectListItem>> GetServiceConceptsForDropdownAsync();

        /// <summary>
        /// Gets available clients for dropdowns.
        /// Obtiene los clientes disponibles para los dropdowns.
        /// </summary>
        /// <returns>List of SelectListItem for clients.</returns>
        Task<List<SelectListItem>> GetClientsForDropdownAsync();

        /// <summary>
        /// Gets a list of locations (points or funds) for dynamic dropdowns.
        /// Obtiene una lista de ubicaciones (puntos o fondos) para dropdowns dinámicos.
        /// </summary>
        /// <param name="clientId">Client ID.</param>
        /// <param name="branchId">Branch ID (optional).</param>
        /// <param name="locationType">The type of location (Point or Fund).</param>
        /// <returns>List of SelectListItem for locations.</returns>
        Task<List<SelectListItem>> GetLocationsForDropdownAsync(int clientId, int? branchId, LocationTypeEnum locationType, string? serviceConceptCode);

        /// <summary>
        /// Gets details of a point or fund given its code.
        /// Obtiene los detalles de un punto o fondo dado su código.
        /// </summary>
        /// <param name="code">Point or fund code.</param>
        /// <param name="clientId">Client ID.</param>
        /// <param name="isPoint">True if it's a point, false if it's a fund.</param>
        /// <returns>A dynamic object with details (Name, City, Branch, Range).</returns>
        Task<object?> GetLocationDetailsByCodeAsync(string code, int clientId, bool isPoint);

        /// <summary>
        /// Obtiene la lista de usuarios responsables de entrega o recepción
        /// filtrados por sucursal y concepto de servicio.
        /// </summary>
        /// <param name="branchId">Sucursal seleccionada.</param>
        /// <param name="serviceConceptCode">Código del concepto de servicio.</param>
        /// <param name="isDelivery">True para responsables de entrega, false para recepción.</param>
        /// <param name="currentUserId">ID del usuario actualmente autenticado.</param>
        /// <returns>Lista de SelectListItem con los usuarios disponibles.</returns>
        Task<List<SelectListItem>> GetResponsibleUsersForDropdownAsync(int branchId, string serviceConceptCode, bool isDelivery, string currentUserId);

        /// <summary>
        /// Gets employees (Head of Shift, driver, crew member) filtered by branch and position.
        /// Obtiene los empleados (JT, conductor, tripulante) filtrados por sucursal y cargo.
        /// </summary>
        /// <param name="branchId">Branch ID.</param>
        /// <param name="positionId">Position ID.</param>
        /// <returns>List of SelectListItem for employees.</returns>
        Task<List<SelectListItem>> GetEmployeesForDropdownAsync(int branchId, int positionId);

        /// <summary>
        /// Obtiene las modalidades de servicio disponibles para los dropdowns.
        /// </summary>
        /// <returns>Lista de SelectListItem para modalidades de servicio.</returns>
        Task<List<SelectListItem>> GetServiceModalitiesForDropdownAsync();

        /// <summary>
        /// Obtiene las divisas de servicio disponibles para los dropdowns.
        /// </summary>
        /// <returns>Lista de SelectListItem para divisas de servicio.</returns>
        Task<List<SelectListItem>> GetCurrenciesForDropdownAsync();

        /// <summary>
        /// Obtiene la lista de responsables de fallos para los dropdowns.
        /// </summary>
        Task<List<SelectListItem>> GetFailedResponsiblesForDropdown();
    }
}