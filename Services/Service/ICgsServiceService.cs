using Microsoft.AspNetCore.Mvc.Rendering;
using VCashApp.Models.ViewModels.Service;
using VCashApp.Services.DTOs;

namespace VCashApp.Services.Service
{
    /// <summary>
    /// Interfaz para los servicios de lógica de negocio relacionados con el Centro de Gestión de Servicios (CGS).
    /// </summary>
    public interface ICgsServiceService
    {
        /// <summary>
        /// Prepara un ViewModel para el formulario de creación de una nueva solicitud de servicio.
        /// Incluye la carga de listas desplegables (clientes, sucursales, conceptos, etc.)
        /// y datos del usuario actual.
        /// </summary>
        /// <param name="currentUserId">El ID del usuario actual autenticado.</param>
        /// <param name="currentIP">La dirección IP del cliente.</param>
        /// <returns>Un ViewModel pre-poblado con datos iniciales y listas.</returns>
        Task<CgsServiceRequestViewModel> PrepareServiceRequestViewModelAsync(string currentUserId, string currentIP);

        /// <summary>
        /// Procesa y guarda una nueva solicitud de servicio a partir de un ViewModel.
        /// Incluye la llamada al Stored Procedure para generar el ServiceOrderId.
        /// </summary>
        /// <param name="viewModel">El ViewModel con los datos de la solicitud.</param>
        /// <param name="currentUserId">El ID del usuario que realiza la operación.</param>
        /// <param name="currentIP">La dirección IP del cliente.</param>
        /// <returns>Un ServiceResult indicando éxito o fracaso, y el ServiceOrderId en la propiedad Data si es exitoso.</returns>
        Task<ServiceResult> CreateServiceRequestAsync(CgsServiceRequestViewModel viewModel, string currentUserId, string currentIP);

        /// <summary>
        /// Obtiene una lista de puntos (oficinas/ATMs) para un cliente y sucursal específicos.
        /// Filtrará por el tipo de punto (0=oficina, 1=ATM) según el Concepto de Servicio.
        /// </summary>
        /// <param name="clientCode">Código del cliente.</param>
        /// <param name="branchCode">Código de la sucursal.</param>
        /// <param name="pointType">Tipo de punto a buscar (0 para oficina, 1 para ATM).</param>
        /// <returns>Lista de SelectListItem con códigos y nombres de puntos.</returns>
        Task<List<SelectListItem>> GetPointsByClientAndBranchAsync(int clientCode, int branchCode, int pointType);

        /// <summary>
        /// Obtiene una lista de fondos para un cliente y sucursal específicos.
        /// </summary>
        /// <param name="clientCode">Código del cliente.</param>
        /// <param name="branchCode">Código de la sucursal.</param>
        /// <param name="fundType">Tipo de fondo a buscar (0 para puntos, 1 para ATM).</param>
        /// <returns>Lista de SelectListItem con códigos y nombres de fondos.</returns>
        Task<List<SelectListItem>> GetFundsByClientAndBranchAsync(int clientCode, int branchCode, int fundType);

        /// <summary>
        /// Obtiene los detalles de un punto específico (nombre, ciudad, rango, etc.) para mostrarlos en el frontend.
        /// </summary>
        /// <param name="pointCode">El código del punto.</param>
        /// <returns>Un objeto dinámico o un DTO con los detalles del punto.</returns>
        Task<dynamic?> GetPointDetailsAsync(string pointCode);

        /// <summary>
        /// Obtiene los detalles de un fondo específico (nombre, ciudad, etc.) para mostrarlos en el frontend.
        /// </summary>
        /// <param name="fundCode">El código del fondo.</param>
        /// <returns>Un objeto dinámico o un DTO con los detalles del fondo.</returns>
        Task<dynamic?> GetFundDetailsAsync(string fundCode);

        // Si planeas una vista de listado/dashboard para CGS, podrías añadir un método como este:
        // Task<Tuple<List<CgsServiceSummaryViewModel>, int>> GetFilteredServiceRequestsAsync(...filtros...);
    }
}
