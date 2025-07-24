using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using VCashApp.Models.ViewModels;
using VCashApp.Services.DTOs;

namespace VCashApp.Services
{
    /// <summary>
    /// Interfaz para el servicio que maneja la lógica de negocio relacionada con los usuarios.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Obtiene una lista paginada y filtrada de usuarios para la visualización en tablas.
        /// </summary>
        /// <param name="search">Término de búsqueda (nombre de usuario, email).</param>
        /// <param name="selectedRoleFilter">Filtro por nombre de rol.</param>
        /// <param name="selectedBranchFilter">Filtro por ID de sucursal.</param>
        /// <param name="page">Número de página actual.</param>
        /// <param name="pageSize">Tamaño de página.</param>
        /// <returns>Una tupla que contiene una colección de UserViewModel y el conteo total.</returns>
        Task<(IEnumerable<UserViewModel> Users, int TotalCount)> GetFilteredUsersAsync(
            string? search, string? selectedRoleFilter, int? selectedBranchFilter,
            int page, int pageSize);

        /// <summary>
        /// Obtiene un UserViewModel pre-populado con listas para la creación de un nuevo usuario (roles, sucursales disponibles).
        /// </summary>
        /// <returns>Un UserViewModel.</returns>
        Task<UserCreateViewModel> GetUserForCreateAsync();

        /// <summary>
        /// Obtiene la lista de vistas del sistema y los permisos (ver, crear, editar) asociados a un rol específico.
        /// </summary>
        /// <param name="roleName">El nombre del rol para el cual obtener los permisos.</param>
        /// <returns>Una lista de ViewPermissionViewModel con los detalles de la vista y los permisos para el rol.</returns>
        Task<List<ViewPermissionViewModel>> GetViewsAndPermissionsForRoleAsync(string roleName);

        /// <summary>
        /// Crea un nuevo usuario en el sistema.
        /// </summary>
        /// <param name="model">El UserViewModel con los datos del nuevo usuario.</param>
        /// <returns>Un ServiceResult indicando el éxito o fracaso de la operación.</returns>
        Task<ServiceResult> CreateUserAsync(UserCreateViewModel model);

        /// <summary>
        /// Obtiene un UserViewModel con los datos de un usuario existente para edición.
        /// Incluye los roles y sucursales asignadas, así como las disponibles.
        /// </summary>
        /// <param name="userId">El ID del usuario a editar.</param>
        /// <returns>Un UserViewModel si el usuario es encontrado, de lo contrario null.</returns>
        Task<UserEditViewModel?> GetUserForEditAsync(string userId);

        /// <summary>
        /// Actualiza un usuario existente en el sistema.
        /// </summary>
        /// <param name="model">El UserViewModel con los datos actualizados del usuario.</param>
        /// <returns>Un ServiceResult indicando el éxito o fracaso de la operación.</returns>
        Task<ServiceResult> UpdateUserAsync(UserEditViewModel model);

        /// <summary>
        /// Obtiene los detalles de un usuario específico para visualización de solo lectura.
        /// </summary>
        /// <param name="userId">El ID del usuario a buscar.</param>
        /// <returns>Un UserViewModel con los datos del usuario, o null si no se encuentra.</returns>
        Task<UserViewModel?> GetUserForDetailsAsync(string userId);

        /// <summary>
        /// Obtiene las listas de roles y sucursales para dropdowns/multiselección.
        /// </summary>
        /// <returns>Una tupla con List<SelectListItem> para Roles y Sucursales.</returns>
        Task<(List<SelectListItem> Roles, List<SelectListItem> Sucursales)> GetDropdownListsAsync();

        /// <summary>
        /// Obtiene una colección COMPLETA de usuarios filtrados para propósitos de exportación, sin paginación.
        /// </summary>
        /// <param name="searchTerm">Término de búsqueda (nombre de usuario, email).</param>
        /// <param name="selectedRoleFilter">Filtro por nombre de rol.</param>
        /// <param name="selectedBranchFilter">Filtro por ID de sucursal.</param>
        /// <returns>Una colección de UserViewModel con todos los datos necesarios para la exportación.</returns>
        Task<IEnumerable<UserViewModel>> GetExportableUsersAsync(
            string? searchTerm, string? selectedRoleFilter, int? selectedBranchFilter);
    }
}