using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VCashApp.Models.ViewModels;
using VCashApp.Services.DTOs;

namespace VCashApp.Services
{
    /// <summary>
    /// Define el contrato para el servicio de gestión de empleados.
    /// </summary>
    public interface IEmployeeService
    {
        /// <summary>
        /// Obtiene las listas necesarias para los dropdowns en las vistas de empleado.
        /// </summary>
        /// <returns>Una tupla con listas para Cargos, Sucursales y Ciudades.</returns>
        Task<(List<SelectListItem> Cargos, List<SelectListItem> Sucursales, List<SelectListItem> Ciudades)> GetDropdownListsAsync(string currentUserId, bool isAdmin);

        /// <summary>
        /// Obtiene una lista paginada y filtrada de empleados como ViewModels.
        /// </summary>
        /// <returns>Una tupla que contiene la lista de empleados para la página actual y el conteo total de registros filtrados.</returns>
        Task<(IEnumerable<EmpleadoViewModel> Employees, int TotalCount)> GetFilteredEmployeesAsync(
            string currentUserId, int? cargoId, int? branchId, int? employeeStatus,
            string search, string gender, int page, int pageSize, bool isAdmin);

        /// <summary>
        /// Obtiene los datos de un empleado específico para el formulario de edición.
        /// </summary>
        /// <param name="employeeId">El ID (cédula) del empleado.</param>
        /// <param name="currentUserId">El ID del usuario que realiza la solicitud.</param>
        /// <param name="isAdmin">Indica si el usuario es administrador.</param>
        /// <returns>Un EmpleadoViewModel con los datos del empleado, o null si no se encuentra o no hay permiso.</returns>
        Task<EmpleadoViewModel> GetEmployeeForEditAsync(int employeeId, string currentUserId, bool isAdmin);

        /// <summary>
        /// Crea un nuevo registro de empleado en la base de datos.
        /// </summary>
        /// <param name="model">El ViewModel con los datos del nuevo empleado.</param>
        /// <param name="currentUserId">El ID del usuario que crea el registro.</param>
        /// <returns>Un ServiceResult indicando el resultado de la operación.</returns>
        Task<ServiceResult> CreateEmployeeAsync(EmpleadoViewModel model, string currentUserId);

        /// <summary>
        /// Actualiza un registro de empleado existente.
        /// </summary>
        /// <param name="model">El ViewModel con los datos actualizados del empleado.</param>
        /// <param name="currentUserId">El ID del usuario que actualiza el registro.</param>
        /// <returns>Un ServiceResult indicando el resultado de la operación.</returns>
        Task<ServiceResult> UpdateEmployeeAsync(EmpleadoViewModel model, string currentUserId);

        /// <summary>
        /// Obtiene el stream de un archivo de imagen (foto/firma) de un empleado.
        /// </summary>
        /// <param name="filePath">La ruta relativa del archivo (ej. "Fotos/12345P.jpg").</param>
        /// <returns>Un FileStream de la imagen, o null si no se encuentra.</returns>
        Task<Stream> GetEmployeeImageStreamAsync(string filePath);

        /// <summary>
        /// Obtiene los detalles de un empleado específico para visualización.
        /// Aplica la lógica de permisos basada en el usuario actual y si es administrador.
        /// </summary>
        /// <param name="employeeId">El ID del empleado a buscar (CodCedula).</param>
        /// <param name="currentUserId">El ID del usuario actual para validación de permisos.</param>
        /// <param name="isAdmin">Indica si el usuario actual tiene rol de administrador.</param>
        /// <returns>Un EmpleadoViewModel con los datos del empleado, o null si no se encuentra o el usuario no tiene permisos.</returns>
        Task<EmpleadoViewModel?> GetEmployeeForDetailsAsync(int employeeId, string currentUserId, bool isAdmin);

        /// <summary>
        /// Cambia el estado de un empleado específico.
        /// </summary>
        /// <param name="employeeId">El ID del empleado (CodCedula).</param>
        /// <param name="newStatus">El nuevo estado deseado (valor numérico de EstadoEmpleado).</param>
        /// <param name="reasonForChange">Una razón para el cambio de estado (opcional, para historial).</param>
        /// <param name="currentUserId">ID del usuario que realiza el cambio.</param>
        /// <returns>Un ServiceResult indicando éxito o fallo.</returns>
        Task<ServiceResult> ChangeEmployeeStatusAsync(int employeeId, int newStatus, string reasonForChange, string currentUserId);
    }
}