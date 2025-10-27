using Microsoft.AspNetCore.Mvc.Rendering;
using VCashApp.Enums;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.CentroEfectivo.Shared;

namespace VCashApp.Services
{
    /// <summary>
    /// Define las operaciones de servicio para la gestión de Transacciones de Centro de Efectivo.
    /// </summary>
    public interface ICefTransactionService
    {
        /// <summary>
        /// Prepara el ViewModel para la vista de Check-in de una nueva transacción.
        /// </summary>
        /// <param name="serviceOrderId">La Orden de Servicio para la que se prepara el check-in.</param>
        /// <param name="currentUserId">La ID del usuario actual.</param>
        /// <param name="currentIP">La dirección IP del usuario actual.</param>
        /// <returns>Un ViewModel listo para la vista de Check-in.</returns>
        Task<CefTransactionCheckinViewModel> PrepareCheckinViewModelAsync(string serviceOrderId, string currentUserId, string currentIP);

        /// <summary>
        /// Procesa los datos del Check-in y crea una nueva transacción de Centro de Efectivo.
        /// </summary>
        /// <param name="viewModel">El ViewModel con los datos de Check-in ingresados.</param>
        /// <param name="currentUserId">ID del usuario que realiza la operación.</param>
        /// <param name="currentIP">Dirección IP desde donde se realiza la operación.</param>
        /// <returns>La entidad CefTransaccion creada.</returns>
        /// <exception cref="InvalidOperationException">Se lanza si la Orden de Servicio no es válida o ya tiene una transacción CEF.</exception>
        Task<CefTransaction> ProcessCheckinViewModelAsync(CefTransactionCheckinViewModel viewModel, string currentUserId, string currentIP);

        /// <summary>
        /// Obtiene una transacción de Centro de Efectivo por su ID, incluyendo detalles necesarios para el procesamiento.
        /// </summary>
        /// <param name="transactionId">ID de la transacción de CEF.</param>
        /// <returns>La transacción de CEF si se encuentra, de lo contrario, null.</returns>
        Task<CefTransaction?> GetCefTransactionByIdAsync(int transactionId);

        /// <summary>
        /// Obtiene una lista paginada y filtrada de transacciones de Centro de Efectivo para el dashboard de Check-in o revisión.
        /// </summary>
        /// <param name="currentUserId">Usuario</param>
        /// <param name="branchId">Filtro por ID de sucursal.</param>
        /// <param name="startDate">Fecha de inicio para el filtro.</param>
        /// <param name="endDate">Fecha de fin para el filtro.</param>
        /// <param name="status">Estado de la transacción para el filtro.</param>
        /// <param name="search">Término de búsqueda.</param>
        /// <param name="page">Número de página.</param>
        /// <param name="pageSize">Tamaño de la página.</param>
        /// <param name="isAdmin">Indica si el usuario es administrador.</param>
        /// <returns>Una tupla que contiene la lista de transacciones y el total de registros.</returns>
        Task<Tuple<List<CefTransactionSummaryViewModel>, int>> GetFilteredCefTransactionsAsync(
            string currentUserId, int? branchId, DateOnly? startDate, DateOnly? endDate, CefTransactionStatusEnum? status,
            string? search, int page, int pageSize, bool isAdmin, IEnumerable<string>? conceptTypeCodes = null, IEnumerable<string>? excludedStatuses = null);

        /// <summary>
        /// Obtiene las listas de dropdowns necesarias para el dashboard de CEF.
        /// </summary>
        Task<(List<SelectListItem> Sucursales, List<SelectListItem> Estados)> GetDropdownListsAsync(string currentUserId, bool isAdmin);

        /// <summary>
        /// Actualiza el estado de una transacción de Centro de Efectivo.
        /// </summary>
        /// <param name="transactionId">ID de la transacción a actualizar.</param>
        /// <param name="newStatus">El nuevo estado de la transacción (enum CefTransactionStatusEnum).</param>
        /// <param name="reviewerUserId">ID del usuario que realiza la actualización.</param>
        /// <returns>Verdadero si la actualización fue exitosa, de lo contrario, falso.</returns>
        /// <exception cref="InvalidOperationException">Se lanza si la transacción no se encuentra o no está en un estado válido para la actualización.</exception>
        Task<bool> UpdateTransactionStatusAsync(int transactionId, CefTransactionStatusEnum newStatus, string reviewerUserId);

        /// <summary>
        /// Prepara el ViewModel para la revisión de una transacción de Centro de Efectivo.
        /// </summary>
        /// <param name="transactionId">ID de la transacción a revisar.</param>
        /// <returns>Un ViewModel con los datos de revisión de la transacción.</returns>
        Task<CefTransactionReviewViewModel?> PrepareReviewViewModelAsync(int transactionId);

        /// <summary>
        /// Procesa la aprobación o rechazo final de una transacción por parte del supervisor revisor.
        /// </summary>
        /// <param name="viewModel">El ViewModel con los datos de revisión y el nuevo estado.</param>
        /// <param name="reviewerUserId">ID del usuario revisor.</param>
        /// <returns>Verdadero si el proceso fue exitoso, de lo contrario, falso.</returns>
        Task<bool> ProcessReviewApprovalAsync(CefTransactionReviewViewModel viewModel, string reviewerUserId);

        /// <summary>
        /// Recalcula TotalCountedValue, aplica el efecto de novedades aprobadas y
        /// actualiza ValueDifference de la transacción.
        /// </summary>
        /// <param name="cefTransactionId">ID de la transacción CEF.</param>
        /// <returns>true si se actualizó; false si no existe.</returns>
        Task<bool> RecalcTotalsAndNetDiffAsync(int cefTransactionId);

        /// <summary>
        /// Obtiene las divisas de servicio disponibles para los dropdowns.
        /// </summary>
        /// <returns>Lista de SelectListItem para divisas de servicio.</returns>
        Task<List<SelectListItem>> GetCurrenciesForDropdownAsync();

        /// <summary>
        /// Prepara el ViewModel detallado para la vista de detalles de una transacción de Centro de Efectivo.
        /// </summary>
        /// <param name="transactionId">Identificador de la transacción</param>
        /// <returns>El ViewModel detallado de la transacción, o null si no se encuentra.</returns>
        Task<CefTransactionDetailViewModel?> PrepareDetailViewModelAsync(int transactionId);
    }
}