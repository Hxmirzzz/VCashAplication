using VCashApp.Models.Entities;
using VCashApp.Enums;
using VCashApp.Models.ViewModels.CentroEfectivo.Shared;

namespace VCashApp.Services
{
    /// <summary>
    /// Define las operaciones de servicio para la gestión de Contenedores de Efectivo (Bolsas, Sobres).
    /// </summary>
    public interface ICefContainerService
    {
        /// <summary>
        /// Prepara un ViewModel para el procesamiento de un contenedor, sea nuevo o existente.
        /// </summary>
        /// <param name="cefTransactionId">ID de la transacción CEF a la que pertenece (requerido para nuevos contenedores).</param>
        /// <returns>Un ViewModel listo para la vista de procesamiento del contenedor.</returns>
        Task<CefProcessContainersPageViewModel> PrepareProcessContainersPageAsync(int cefTransactionId);

        /// <summary>
        /// Crea o actualiza un contenedor de efectivo (bolsa/sobre) y sus detalles de valores.
        /// </summary>
        /// <param name="viewModel">ViewModel con los datos del contenedor y sus detalles.</param>
        /// <param name="processingUserId">ID del usuario que procesa el contenedor (cajero).</param>
        /// <returns>El contenedor de CEF creado o actualizado.</returns>
        /// <exception cref="InvalidOperationException">Se lanza si la transacción no es válida o hay errores de guardado.</exception>
        Task<CefContainer> SaveContainerAndDetailsAsync(CefContainerProcessingViewModel viewModel, string processingUserId);

        /// <summary>
        /// Elimina un contenedor de efectivo y sus detalles asociados.
        /// </summary>
        /// <returns>true si se eliminó correctamente, false si no se encontró el contenedor.</returns>
        Task<bool> DeleteContainerAsync(int transactionId, int containerId);

        /// <summary>
        /// Obtiene un contenedor de efectivo por su ID, incluyendo sus detalles de valores y novedades.
        /// </summary>
        /// <param name="containerId">ID del contenedor.</param>
        /// <returns>El contenedor de CEF si se encuentra, de lo contrario, null.</returns>
        Task<CefContainer?> GetContainerWithDetailsAsync(int containerId);

        /// <summary>
        /// Obtiene una lista de contenedores para una transacción específica, incluyendo su jerarquía.
        /// </summary>
        /// <param name="transactionId">ID de la transacción CEF.</param>
        /// <returns>Lista de contenedores.</returns>
        Task<List<CefContainer>> GetContainersByTransactionIdAsync(int transactionId);

        /// <summary>
        /// Obtiene las denominaciones en formato JSON para una transacción específica.
        /// </summary>
        /// <param name="cefTransactionId">ID de la transaccion</param>
        /// <returns>Lista de denominaciones</returns>
        Task<String> BuildDenomsJsonForTransactionAsync(int cefTransactionId);

        /// <summary>
        /// Obtiene las calidades de billetes en formato JSON.
        /// </summary>
        /// <returns>Lista de calidades</returns>
        Task<String> BuildQualitiesJsonAsync();

        /// <summary>
        /// Obtener todas las entidades bancarias.
        /// </summary>
        /// <returns>Lista de las entidades bancarias.</returns>
        Task<string> BuildBankEntitiesJsonAsync();

        /// <summary>
        /// Obtiene las capacidades de puntos para sobres, documentos y cheques.
        /// <param name="serviceOrderId">: ID de la orden de servicio.</param>
        /// <remarks>Las capacidades se determinan según la configuración del punto de servicio asociado a la orden de servicio.</remarks>
        /// <returns>Tupla con las capacidades (sobres, documentos, cheques).</returns>
        Task<(bool sobres, bool documentos, bool cheques)> GetPointCapsAsync(string serviceOrderId);

        /// <summary>
        /// Suma el valor contado (preferentemente desde detalles) para toda la transacción.
        /// </summary>
        Task<decimal> SumCountedValueAsync(int cefTransactionId);
    }
}