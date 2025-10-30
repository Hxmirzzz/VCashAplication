using VCashApp.Models.ViewModels.Employee;
using VCashApp.Services.DTOs;

namespace VCashApp.Services.Employee.Application
{
    /// <summary>
    /// Interfaz para el manejo de comandos de empleados.
    /// </summary>
    public interface IEmployeeWriteService
    {
        /// <summary>
        /// Crea un nuevo empleado.
        /// </summary>
        /// <param name="model">Modelo con la información del empleado</param>
        /// <param name="userId">Usuario que realiza la petición</param>
        /// <returns>Resultado del servicio</returns>
        Task<ServiceResult> CreateAsync(EmployeeViewModel model, string userId);

        /// <summary>
        /// Actualiza la información de un empleado.
        /// </summary>
        /// <param name="model">Modelo con la información del empleado</param>
        /// <param name="userId">Usuario que realiza la petición</param>
        /// <returns>Resultado del servicio</returns>
        Task<ServiceResult> UpdateAsync(EmployeeViewModel model, string userId);

        /// <summary>
        /// Cambia el estado de un empleado.
        /// </summary>
        /// <param name="id">ID del empleado</param>
        /// <param name="newStatus">Nuevo estado</param>
        /// <param name="reason">Razon del cambio de estado</param>
        /// <param name="userId">Usuario que realiza la petición</param>
        /// <returns>Resultado del servicio</returns>
        Task<ServiceResult> ChangeStatusAsync(int id, int newStatus, string reason, string userId);

        /// <summary>
        /// Obtiene un stream de una imagen de empleado dado su path relativo.
        /// </summary>
        /// <param name="relativePath"> Path relativo de la imagen</param>
        /// <returns>Stream de la imagen o nulo si no existe</returns>
        Task<Stream?> OpenImageAsync(string relativePath);
    }
}
