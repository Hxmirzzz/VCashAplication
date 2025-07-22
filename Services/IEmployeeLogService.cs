using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VCashApp.Models.Entities;
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
        /// <param name="searchTerm">Término de búsqueda opcional para campos como nombre de empleado, cédula, etc.</param>
        /// <param name="page">El número de página a recuperar (usado para paginación).</param>
        /// <param name="pageSize">El número de registros por página (usado para paginación).</param>
        /// <param name="isAdmin">Indica si el usuario actual tiene rol de administrador (afecta los permisos de filtrado).</param>
        /// <returns>
        /// Un <see cref="Tuple{T1, T2}"/> donde T1 es una <see cref="List{SegRegistroEmpleado}"/> con los registros encontrados
        /// y T2 es el número total de registros que cumplen los criterios de filtro (antes de la paginación).
        /// </returns>
        Task<Tuple<List<SegRegistroEmpleado>, int>> GetFilteredEmployeeLogsAsync(
            string currentUserId,
            int? cargoId,
            string? unitId,
            int? branchId,
            DateOnly? startDate,
            DateOnly? endDate,
            int? logStatus,
            string? searchTerm,
            int page,
            int pageSize,
            bool isAdmin);
    }
}