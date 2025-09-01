using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.EmployeeLog;
using VCashApp.Services.DTOs;

namespace VCashApp.Services
{
    /// <summary>
    /// Interfaz para los servicios de lógica de negocio relacionados con los registros de empleados (entrada y salida).
    /// </summary>
    public interface IEmployeeLogService
    {
        /// <summary>
        /// Recupera el estado actual de registro de un empleado, indicando si está actualmente dentro o fuera.
        /// </summary>
        /// <param name="employeeId">La cédula del empleado para la cual se desea consultar el estado.</param>
        /// <returns>
        /// Un objeto <see cref="EmployeeLogStateDto"/> que describe el estado actual del registro del empleado,
        /// incluyendo información sobre el último registro abierto si existe.
        /// </returns>
        Task<EmployeeLogStateDto> GetEmployeeLogStatusAsync(int employeeId);

        /// <summary>
        /// Registra la entrada o salida de un empleado, creando un nuevo registro o actualizando uno existente.
        /// Este método es utilizado para registros automáticos o basados en la acción del usuario.
        /// </summary>
        /// <param name="logDto">Objeto <see cref="EmployeeLogDto"/> que contiene los datos del registro (cédula del empleado, fecha, hora, etc.).</param>
        /// <param name="currentUserId">El ID del usuario que está realizando la operación de registro (supervisor, sistema, etc.).</param>
        /// <param name="confirmedValidation">Parámetro opcional para validaciones adicionales o confirmaciones (e.g., código de supervisor).</param>
        /// <returns>
        /// Un objeto <see cref="ServiceResult"/> que indica el éxito o fracaso de la operación,
        /// junto con mensajes de error o información adicional.
        /// </returns>
        Task<ServiceResult> RecordEmployeeEntryExitAsync(
            EmployeeLogDto logDto,
            string currentUserId,
            string? confirmedValidation = null);

        /// <summary>
        /// Actualiza un registro de empleado existente, típicamente para registrar una salida,
        /// modificar detalles de un registro ya creado o corregir una entrada previa.
        /// </summary>
        /// <param name="logId">El ID del registro de empleado a actualizar.</param>
        /// <param name="logDto">Objeto <see cref="EmployeeLogDto"/> con los datos actualizados para el registro.</param>
        /// <param name="currentUserId">El ID del usuario que está realizando la actualización.</param>
        /// <param name="confirmedValidation">Parámetro opcional para validaciones o confirmaciones adicionales.</param>
        /// <returns>
        /// Un objeto <see cref="ServiceResult"/> que indica el éxito o fracaso de la operación,
        /// junto con mensajes de error o información adicional.
        /// </returns>
        Task<ServiceResult> UpdateEmployeeLogAsync(
            int logId,
            EmployeeLogDto logDto,
            string currentUserId,
            string? confirmedValidation = null);

        /// <summary>
        /// Registra una salida manual para un registro de empleado existente que aún se encuentra abierto (sin salida registrada).
        /// Este método permite cerrar un turno que no fue cerrado automáticamente.
        /// </summary>
        /// <param name="logId">El ID del registro de empleado a cerrar manualmente.</param>
        /// <param name="exitDate">La fecha de la salida manual.</param>
        /// <param name="exitTime">La hora de la salida manual.</param>
        /// <param name="currentUserId">El ID del usuario (ej. supervisor) que está registrando la salida manual.</param>
        /// <param name="confirmedValidation">Parámetro opcional para validaciones o confirmaciones adicionales.</param>
        /// <returns>
        /// Un objeto <see cref="ServiceResult"/> que indica el éxito o fracaso de la operación.
        /// </returns>
        Task<ServiceResult> RecordManualEmployeeExitAsync(
            int logId,
            DateOnly exitDate,
            TimeOnly exitTime,
            string currentUserId,
            string? confirmedValidation = null);

        /// <summary>
        /// Recupera una lista de registros de empleados filtrados y paginados.
        /// Este método es utilizado para mostrar el historial o reportes de registros.
        /// </summary>
        /// <param name="currentUserId">El ID del usuario actual que realiza la consulta (para aplicar filtros de permiso por sucursal/unidad si aplica).</param>
        /// <param name="cargoId">Filtro opcional por el ID del cargo del empleado.</param>
        /// <param name="unitId">Filtro opcional por el ID de la unidad del empleado.</param>
        /// <param name="branchId">Filtro opcional por el ID de la sucursal del empleado.</param>
        /// <param name="startDate">Filtro opcional para la fecha de inicio del período de búsqueda.</param>
        /// <param name="endDate">Filtro opcional para la fecha de fin del período de búsqueda.</param>
        /// <param name="logStatus">Filtro opcional por el estado del registro (ej., abierto, cerrado).</param>
        /// <param name="search">Término de búsqueda opcional para campos como nombre de empleado, cédula, etc.</param>
        /// <param name="page">El número de página a recuperar (usado para paginación).</param>
        /// <param name="pageSize">El número de registros por página (usado para paginación).</param>
        /// <param name="isAdmin">Indica si el usuario actual tiene rol de administrador (afecta los permisos de filtrado).</param>
        /// <returns>
        /// Un <see cref="Tuple{T1, T2}"/> donde T1 es una <see cref="List{SegRegistroEmpleado}"/> con los registros encontrados
        /// y T2 es el número total de registros que cumplen los criterios de filtro (antes de la paginación).
        /// </returns>
        Task<Tuple<List<EmployeeLogSummaryViewModel>, int>> GetFilteredEmployeeLogsAsync(
            string currentUserId, int? cargoId, string? unitId, int? branchId,
            DateOnly? startDate, DateOnly? endDate, int? logStatus,
            string? search, int page, int pageSize, bool isAdmin);

        /// <summary>
        /// Obtiene la información básica de empleados para propósitos de selección o asignación,
        /// </summary>
        /// <param name="userId">El ID del usuario actual para validación de permisos.</param>
        /// <param name="permittedBranchIds">Lista de IDs de sucursales a las que el usuario tiene acceso (si aplica).</param>
        /// <param name="searchInput">Buscador</param>
        /// <param name="isAdmin">Indica si el usuario actual tiene rol de administrador (afecta los permisos de filtrado).</param>
        /// <returns>Una lista de empleados que cumplen con los criterios de búsqueda y permisos.</returns>
        Task<List<AdmEmpleado>> GetEmployeeInfoAsync(string userId, List<int> permittedBranchIds, string? searchInput, bool isAdmin);

        /// <summary>
        /// Obtiene los datos necesarios para mostrar los detalles de un registro de empleado específico.
        /// </summary>
        /// <param name="id">El ID del registro de empleado a consultar.</param>
        /// <param name="canEditLog">Indica si el usuario actual tiene permiso para editar el registro (afecta los datos retornados).</param>
        /// <returns>Muestra el formulario de edición del registro de empleado o null si no se encuentra.</returns>
        Task<EmployeeLogEditViewModel?> GetEditViewModelAsync(int id, bool canEditLog);

        /// <summary>
        /// Obtiene los datos necesarios para mostrar los detalles de un registro de empleado específico en una vista de solo lectura.
        /// </summary>
        /// <param name="id">El ID del registro de empleado a consultar.</param>
        /// <returns>Muestra el formulario de detalles del registro de empleado o null si no se encuentra.</returns>
        Task<EmployeeLogDetailsViewModel?> GetDetailsViewModelAsync(int id);

        /// <summary>
        /// Obtiene los datos necesarios para mostrar el formulario de creación de un nuevo registro de empleado.
        /// </summary>
        /// <param name="id">El ID del empleado para el cual se creará el registro.</param>
        /// <param name="canCreateLog">Permiso para crear</param>
        /// <param name="canEditLog">Permiso para editar</param>
        Task<EmployeeLogManualExitViewModel?> GetManualExitViewModelAsync(int id, bool canCreateLog, bool canEditLog);

        /// <summary>
        /// Obtiene los datos necesarios para mostrar el formulario de creación de un nuevo registro de empleado.
        /// </summary>
        /// <param name="userName">Nombre de usuario</param>
        /// <param name="unidadName">Nombre de unidad</param>
        /// <param name="branchName">Nombre de sucursal</param>
        /// <param name="fullName">Nombre completo del empleado</param>
        /// <param name="canCreate">Permiso para crear</param>
        /// <param name="canEdit">Permiso para editar</param>
        Task<EmployeeLogEntryViewModel> GetEntryViewModelAsync(string userName, string? unidadName, string? branchName, string? fullName, bool canCreate, bool canEdit);

        /// <summary>
        /// Gestiona el registro de entrada o salida de un empleado basado en la información proporcionada en el ViewModel.
        /// </summary>
        /// <param name="currentUserId">Usuario</param>
        /// <param name="confirmedValidation">Validación</param>
        /// <param name="vm">ViewModel con los datos del registro</param>
        /// <returns>Resultado del servicio indicando éxito o fracaso de la operación.</returns>
        Task<ServiceResult> RecordEmployeeEntryExitAsync(EmployeeLogEntryViewModel vm, string currentUserId, string? confirmedValidation);

        /// <summary>
        /// Gestiona la actualización de un registro de empleado existente basado en la información proporcionada en el ViewModel.
        /// </summary>
        /// <param name="currentUserId">Usuario</param>
        /// <param name="confirmedValidation">Validacion</param>
        /// <param name="vm">ViewModel con los datos del registro a actualizar</param>
        /// <returns>Resultado del servicio indicando éxito o fracaso de la operación.</returns>
        Task<ServiceResult> UpdateEmployeeLogAsync(EmployeeLogEditViewModel vm, string currentUserId, string? confirmedValidation);

        /// <summary>
        /// Gestiona el registro de una salida manual para un registro de empleado existente basado en la información proporcionada en el ViewModel.
        /// </summary>
        /// <param name="currentUserId">Usuario</param>
        /// <param name="confirmedValidation">Validacion</param>
        /// <param name="vm">ViewModel con los datos del registro a actualizar</param>
        /// <returns>Resultado del servicio indicando éxito o fracaso de la operación.</returns>
        Task<ServiceResult> RecordManualEmployeeExitAsync(EmployeeLogManualExitViewModel vm, string currentUserId, string? confirmedValidation);

        /// <summary>
        /// Obtiene el registro de empleado por ID, incluyendo empleado/cargo/unidad/sucursal.
        /// </summary>
        /// <param name="id">ID del registro.</param>
        /// <returns>La entidad <see cref="SegRegistroEmpleado"/> o null si no existe.</returns>
        Task<SegRegistroEmpleado?> GetLogByIdAsync(int id);
    }
}