using VCashApp.Models.Entities;
using VCashApp.Enums;
using VCashApp.Models.ViewModels.CentroEfectivo;

namespace VCashApp.Services
{
    /// <summary>
    /// Define las operaciones de servicio para la gestión de Novedades de Centro de Efectivo.
    /// </summary>
    public interface ICefIncidentService
    {
        /// <summary>
        /// Registra una nueva novedad asociada a una transacción, contenedor o detalle de valor.
        /// </summary>
        /// <param name="incidentViewModel">ViewModel con los datos de la novedad.</param>
        /// <param name="reportedUserId">ID del usuario que reporta la novedad.</param>
        /// <returns>La novedad creada.</returns>
        /// <exception cref="InvalidOperationException">Se lanza si la novedad no puede ser registrada debido a IDs inválidos.</exception>
        Task<CefIncident> RegisterIncidentAsync(CefIncidentViewModel incidentViewModel, string reportedUserId);

        /// <summary>
        /// Resuelve el estado de una novedad.
        /// </summary>
        /// <param name="incidentId">ID de la novedad a resolver.</param>
        /// <param name="newStatus">El nuevo estado de la novedad (ej: 'Resuelta', 'Ajustada').</param>
        /// <returns>Verdadero si la novedad fue resuelta exitosamente, de lo contrario, falso.</returns>
        Task<bool> ResolveIncidentAsync(int incidentId, string newStatus);

        /// <summary>
        /// Obtiene una lista de novedades filtradas por transacción o contenedor.
        /// </summary>
        /// <param name="transactionId">ID de la transacción (opcional).</param>
        /// <param name="containerId">ID del contenedor (opcional).</param>
        /// <param name="valueDetailId">ID del detalle de valor (opcional).</param>
        /// <returns>Lista de novedades.</returns>
        Task<List<CefIncident>> GetIncidentsAsync(int? transactionId = null, int? containerId = null, int? valueDetailId = null);

        /// <summary>
        /// Obtiene una novedad específica por su ID.
        /// </summary>
        /// <param name="incidentId">ID de la novedad.</param>
        /// <returns>La novedad si se encuentra, de lo contrario, null.</returns>
        Task<CefIncident?> GetIncidentByIdAsync(int incidentId);

        /// <summary>
        /// Obtiene todos los tipos de novedad disponibles desde la tabla maestra `Adm_TiposNovedad`.
        /// </summary>
        /// <returns>Lista de entidades AdmTipoNovedad.</returns>
        Task<List<CefIncidentType>> GetAllIncidentTypesAsync();

        /// <summary>
        /// Asynchronously calculates the total approved effect for a specific transaction.
        /// </summary>
        /// <remarks>This method retrieves and sums the approved effects associated with the specified
        /// transaction. Ensure that the transaction ID provided corresponds to a valid transaction in the
        /// system.</remarks>
        /// <param name="transactionId">The unique identifier of the transaction for which the approved effect is to be summed.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the total approved effect as a
        /// decimal value.</returns>
        Task<decimal> SumApprovedEffectByTransactionAsync(int transactionId);
        Task<bool> HasPendingIncidentsByTransactionAsync(int cefTransactionId);
        Task<decimal> SumApprovedEffectByContainerAsync(int containerId);

    }
}