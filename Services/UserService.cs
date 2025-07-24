using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Serilog;
using VCashApp.Data;
using VCashApp.Models;
using VCashApp.Models.ViewModels;
using VCashApp.Services.DTOs;

namespace VCashApp.Services
{
    /// <summary>
    /// Servicio que maneja la lógica de negocio relacionada con la gestión de usuarios.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly Serilog.ILogger _logger;

        public UserService(AppDbContext context,
                           UserManager<ApplicationUser> userManager,
                           RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = Log.ForContext<UserService>();
        }

        /// <summary>
        /// Obtiene una lista paginada y filtrada de usuarios para la visualización en tablas.
        /// </summary>
        public async Task<(IEnumerable<UserViewModel> Users, int TotalCount)> GetFilteredUsersAsync(
            string? searchTerm, string? selectedRoleFilter, int? selectedBranchFilter,
            int page, int pageSize)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string lowerSearchTerm = searchTerm.Trim().ToLower();
                query = query.Where(u => u.UserName.ToLower().Contains(lowerSearchTerm) ||
                                         u.NombreUsuario.ToLower().Contains(lowerSearchTerm) ||
                                         u.Email.ToLower().Contains(lowerSearchTerm));
            }

            if (!string.IsNullOrWhiteSpace(selectedRoleFilter))
            {
                // SOLUCIÓN CORREGIDA: Filtrar directamente usando JOIN con las tablas de Identity
                query = from user in query
                        join userRole in _context.UserRoles on user.Id equals userRole.UserId
                        join role in _context.Roles on userRole.RoleId equals role.Id
                        where role.Name == selectedRoleFilter
                        select user;
            }

            if (selectedBranchFilter.HasValue)
            {
                string branchIdAsString = selectedBranchFilter.Value.ToString();
                query = query.Where(u => u.Claims.Any(uc =>
                    uc.ClaimType == "SucursalId" &&
                    uc.ClaimValue == branchIdAsString));
            }

            var totalCount = await query.CountAsync();

            var allActiveBranches = await _context.AdmSucursales
                                                   .Where(s => s.Estado)
                                                   .Select(s => new { s.CodSucursal, s.NombreSucursal })
                                                   .ToListAsync();

            var users = await query
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userViewModels = new List<UserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var claims = await _userManager.GetClaimsAsync(user);
                var branchClaims = claims.Where(c => c.Type == "SucursalId").ToList();

                // Obtener nombres de sucursales
                var assignedBranchIds = branchClaims.Select(c => int.Parse(c.Value)).ToList();
                var assignedBranchNames = allActiveBranches
                                            .Where(s => assignedBranchIds.Contains(s.CodSucursal))
                                            .Select(s => s.NombreSucursal)
                                            .ToList();

                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    NombreUsuario = user.NombreUsuario,
                    Email = user.Email,
                    SelectedRole = roles.FirstOrDefault() ?? "Sin Rol",
                    AssignedBranchIds = assignedBranchIds,
                    AssignedBranchesNames = string.Join(", ", assignedBranchNames)
                });
            }

            return (userViewModels, totalCount);
        }

        /// <summary>
        /// Obtiene un UserViewModel pre-populado con listas para la creación de un nuevo usuario (roles, sucursales disponibles).
        /// </summary>
        public async Task<UserCreateViewModel> GetUserForCreateAsync()
        {
            var (roles, sucursales) = await GetDropdownListsAsync();
            return new UserCreateViewModel
            {
                RolesList = roles,
                AvailableBranchesList = sucursales,
                AssignedBranchIds = new List<int>()
            };
        }

        /// <summary>
        /// Obtiene la lista de vistas del sistema y los permisos (ver, crear, editar) asociados a un rol específico.
        /// </summary>
        public async Task<List<ViewPermissionViewModel>> GetViewsAndPermissionsForRoleAsync(string roleName)
        {
            var viewsAndPermissions = new List<ViewPermissionViewModel>();

            // Primero, obtén el Id del rol
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                _logger.Warning("Role '{RoleName}' not found when retrieving view permissions.", roleName);
                return viewsAndPermissions;
            }

            // Obtener todas las vistas disponibles en el sistema
            var allViews = await _context.AdmVistas.AsNoTracking().ToListAsync();

            // Obtener los permisos de perfil para este rol
            var rolePermissions = await _context.PermisosPerfil.AsNoTracking()
                                                              .Where(pp => pp.CodPerfilId == role.Id)
                                                              .ToListAsync();

            foreach (var view in allViews)
            {
                var permission = rolePermissions.FirstOrDefault(pp => pp.CodVista == view.CodVista);

                viewsAndPermissions.Add(new ViewPermissionViewModel
                {
                    CodVista = view.CodVista,
                    NombreVista = view.NombreVista,
                    PuedeVer = permission?.PuedeVer ?? false,
                    PuedeCrear = permission?.PuedeCrear ?? false,
                    PuedeEditar = permission?.PuedeEditar ?? false
                });
            }

            return viewsAndPermissions;
        }

        /// <summary>
        /// Crea un nuevo usuario en el sistema.
        /// </summary>
        public async Task<ServiceResult> CreateUserAsync(UserCreateViewModel model)
        {
            // 1. Validar si el usuario ya existe
            if (await _userManager.FindByNameAsync(model.UserName) != null)
            {
                return ServiceResult.FailureResult($"Ya existe un usuario con el nombre '{model.UserName}'.");
            }
            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                return ServiceResult.FailureResult($"Ya existe un usuario con el correo electrónico '{model.Email}'.");
            }

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                NombreUsuario = model.NombreUsuario,
                Email = model.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                _logger.Warning("Failed to create user {UserName}: {Errors}", model.UserName, string.Join(", ", result.Errors.Select(e => e.Description)));
                return ServiceResult.FailureResult($"Error al crear el usuario: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            // 4. Asignar rol
            if (!string.IsNullOrWhiteSpace(model.SelectedRole))
            {
                if (!await _roleManager.RoleExistsAsync(model.SelectedRole))
                {
                    await _userManager.DeleteAsync(user);
                    _logger.Warning("Role '{Role}' not found when creating user {UserName}.", model.SelectedRole, model.UserName);
                    return ServiceResult.FailureResult($"El rol '{model.SelectedRole}' no existe.");
                }
                var roleResult = await _userManager.AddToRoleAsync(user, model.SelectedRole);
                if (!roleResult.Succeeded)
                {
                    _logger.Warning("Failed to add user {UserName} to role {Role}: {Errors}", model.UserName, model.SelectedRole, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    return ServiceResult.FailureResult($"Usuario creado, pero hubo un error al asignar el rol: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                }
            }

            // 5. Asignar sucursales como claims
            if (model.AssignedBranchIds != null && model.AssignedBranchIds.Any())
            {
                foreach (var branchId in model.AssignedBranchIds)
                {
                    await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("SucursalId", branchId.ToString()));
                }
            }

            _logger.Information("User {UserName} created successfully with role {Role} and branches {Branches}.", model.UserName, model.SelectedRole, string.Join(", ", model.AssignedBranchIds ?? new List<int>()));
            return ServiceResult.SuccessResult("Usuario creado exitosamente.", userId: user.Id);
        }

        /// <summary>
        /// Obtiene un UserViewModel con los datos de un usuario existente para edición.
        /// Incluye los roles y sucursales asignadas, así como las disponibles.
        /// </summary>
        public async Task<UserEditViewModel?> GetUserForEditAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var (rolesList, availableBranchesList) = await GetDropdownListsAsync();

            var userRoles = await _userManager.GetRolesAsync(user);
            var userClaims = await _userManager.GetClaimsAsync(user);

            var assignedBranchIds = userClaims.Where(c => c.Type == "SucursalId" && int.TryParse(c.Value, out _))
                                              .Select(c => int.Parse(c.Value))
                                              .ToList();

            return new UserEditViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                NombreUsuario = user.NombreUsuario,
                Email = user.Email,
                SelectedRole = userRoles.FirstOrDefault() ?? "",
                RolesList = rolesList,
                AssignedBranchIds = assignedBranchIds,
                AvailableBranchesList = availableBranchesList
            };
        }

        /// <summary>
        /// Actualiza un usuario existente en el sistema.
        /// </summary>
        public async Task<ServiceResult> UpdateUserAsync(UserEditViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return ServiceResult.FailureResult("Usuario no encontrado para actualización.");
            }

            // 1. Verificar si el nombre de usuario o email ya existen para otro usuario
            if (await _userManager.FindByNameAsync(model.UserName) != null && user.UserName != model.UserName)
            {
                return ServiceResult.FailureResult($"Ya existe un usuario con el nombre '{model.UserName}'.");
            }
            if (await _userManager.FindByEmailAsync(model.Email) != null && user.Email != model.Email)
            {
                return ServiceResult.FailureResult($"Ya existe un usuario con el correo electrónico '{model.Email}'.");
            }

            user.UserName = model.UserName;
            user.Email = model.Email;
            user.NombreUsuario = model.NombreUsuario;
            // No actualizar contraseña aquí; debe ser una funcionalidad separada si no se envió una nueva contraseña.
            // Si el ViewModel tiene Password y ConfirmPassword, puedes agregar lógica para cambiar la contraseña si se proporcionan.
            // if (!string.IsNullOrEmpty(model.Password))
            // {
            //     var removePasswordResult = await _userManager.RemovePasswordAsync(user);
            //     if (!removePasswordResult.Succeeded) { /* ... handle error ... */ }
            //     var addPasswordResult = await _userManager.AddPasswordAsync(user, model.Password);
            //     if (!addPasswordResult.Succeeded) { /* ... handle error ... */ }
            // }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.Warning("Failed to update user {UserName}: {Errors}", model.UserName, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                return ServiceResult.FailureResult($"Error al actualizar usuario: {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
            }

            // 3. Actualizar Rol
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any() && currentRoles.FirstOrDefault() != model.SelectedRole)
            {
                var removeRoleResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeRoleResult.Succeeded)
                {
                    _logger.Warning("Failed to remove user {UserName} from current roles: {Errors}", user.UserName, string.Join(", ", removeRoleResult.Errors.Select(e => e.Description)));
                    return ServiceResult.FailureResult($"Error al remover rol antiguo: {string.Join(", ", removeRoleResult.Errors.Select(e => e.Description))}");
                }
            }

            if (!string.IsNullOrWhiteSpace(model.SelectedRole) && !await _userManager.IsInRoleAsync(user, model.SelectedRole))
            {
                if (!await _roleManager.RoleExistsAsync(model.SelectedRole))
                {
                    _logger.Warning("Role '{Role}' not found when updating user {UserName}.", model.SelectedRole, model.UserName);
                    return ServiceResult.FailureResult($"El rol '{model.SelectedRole}' no existe.");
                }
                var addRoleResult = await _userManager.AddToRoleAsync(user, model.SelectedRole);
                if (!addRoleResult.Succeeded)
                {
                    _logger.Warning("Failed to add user {UserName} to new role {Role}: {Errors}", user.UserName, model.SelectedRole, string.Join(", ", addRoleResult.Errors.Select(e => e.Description)));
                    return ServiceResult.FailureResult($"Error al asignar nuevo rol: {string.Join(", ", addRoleResult.Errors.Select(e => e.Description))}");
                }
            }
            // NOTA: Si el usuario ya está en el rol, AddToRoleAsync no hace nada, no hay problema.

            // 4. Actualizar Sucursales (Claims)
            var currentClaims = await _userManager.GetClaimsAsync(user);
            var currentBranchClaims = currentClaims.Where(c => c.Type == "SucursalId").ToList();

            // Eliminar claims de sucursales que ya no están asignadas
            foreach (var existingClaim in currentBranchClaims)
            {
                if (!model.AssignedBranchIds.Any(id => id.ToString() == existingClaim.Value))
                {
                    await _userManager.RemoveClaimAsync(user, existingClaim);
                }
            }

            // Añadir claims de sucursales nuevas
            foreach (var newBranchId in model.AssignedBranchIds)
            {
                if (!currentBranchClaims.Any(c => c.Value == newBranchId.ToString()))
                {
                    await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("SucursalId", newBranchId.ToString()));
                }
            }

            // Validar mínimo una sucursal asignada
            if (!model.AssignedBranchIds.Any())
            {
                if (!currentBranchClaims.Any() && !model.AssignedBranchIds.Any())
                {
                    return ServiceResult.FailureResult("El usuario debe tener al menos una sucursal asignada.");
                }
            }


            _logger.Information("User {UserName} updated successfully. Role: {Role}, Branches: {Branches}.", model.UserName, model.SelectedRole, string.Join(", ", model.AssignedBranchIds));
            return ServiceResult.SuccessResult("Usuario actualizado exitosamente.");
        }

        /// <summary>
        /// Obtiene los detalles de un usuario específico para visualización de solo lectura.
        /// </summary>
        public async Task<UserViewModel?> GetUserForDetailsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var userRoles = await _userManager.GetRolesAsync(user);
            var userClaims = await _userManager.GetClaimsAsync(user);

            var assignedBranchIds = userClaims.Where(c => c.Type == "SucursalId" && int.TryParse(c.Value, out _))
                                              .Select(c => int.Parse(c.Value))
                                              .ToList();

            var allActiveBranches = await _context.AdmSucursales
                                       .Where(s => s.Estado)
                                       .Select(s => new { s.CodSucursal, s.NombreSucursal })
                                       .ToListAsync();

            var assignedBranchNames = allActiveBranches
                            .Where(s => assignedBranchIds.Contains(s.CodSucursal))
                            .Select(s => s.NombreSucursal)
                            .ToList();

            List<ViewPermissionViewModel> roleViewPermissions = new List<ViewPermissionViewModel>();
            string primaryRole = userRoles.FirstOrDefault();
            if (!string.IsNullOrEmpty(primaryRole))
            {
                roleViewPermissions = await GetViewsAndPermissionsForRoleAsync(primaryRole);
            }

            return new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                NombreUsuario = user.NombreUsuario,
                Email = user.Email,
                SelectedRole = userRoles.FirstOrDefault() ?? "Sin Rol",
                AssignedBranchIds = assignedBranchIds,
                AssignedBranchesNames = string.Join(", ", assignedBranchNames),
                ViewPermissions = roleViewPermissions
            };
        }

        /// <summary>
        /// Obtiene las listas de roles y sucursales para dropdowns/multiselección.
        /// </summary>
        public async Task<(List<SelectListItem> Roles, List<SelectListItem> Sucursales)> GetDropdownListsAsync()
        {
            // Roles
            var roles = await _roleManager.Roles
                                          .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                                          .ToListAsync();

            // Sucursales
            var sucursales = await _context.AdmSucursales
                                           .Where(s => s.Estado && s.CodSucursal != 32) // Filtro de sucursales activas y no la 32
                                           .Select(s => new SelectListItem { Value = s.CodSucursal.ToString(), Text = s.NombreSucursal })
                                           .ToListAsync();

            return (roles, sucursales);
        }

        /// <summary>
        /// Obtiene una colección COMPLETA de usuarios filtrados para propósitos de exportación, sin paginación.
        /// </summary>
        public async Task<IEnumerable<UserViewModel>> GetExportableUsersAsync(
            string? searchTerm, string? selectedRoleFilter, int? selectedBranchFilter)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string lowerSearchTerm = searchTerm.Trim().ToLower();
                query = query.Where(u => u.UserName.ToLower().Contains(lowerSearchTerm) ||
                                         u.Email.ToLower().Contains(lowerSearchTerm));
            }

            if (!string.IsNullOrWhiteSpace(selectedRoleFilter))
            {
                query = from user in query
                        join userRole in _context.UserRoles on user.Id equals userRole.UserId
                        join role in _context.Roles on userRole.RoleId equals role.Id
                        where role.Name == selectedRoleFilter
                        select user;
            }

            if (selectedBranchFilter.HasValue)
            {
                string branchIdAsString = selectedBranchFilter.Value.ToString();
                query = query.Where(u => u.Claims.Any(uc => uc.ClaimType == "SucursalId" && uc.ClaimValue == branchIdAsString));
            }

            var users = await query.ToListAsync();

            var allActiveBranches = await _context.AdmSucursales
                                                   .Where(s => s.Estado)
                                                   .Select(s => new { s.CodSucursal, s.NombreSucursal })
                                                   .ToListAsync();

            var userViewModels = new List<UserViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var claims = await _userManager.GetClaimsAsync(user);
                var branchClaims = claims.Where(c => c.Type == "SucursalId").ToList();

                var assignedBranchIds = branchClaims.Select(c => int.Parse(c.Value)).ToList();
                var assignedBranchNames = allActiveBranches // Filtrar en memoria
                                            .Where(s => assignedBranchIds.Contains(s.CodSucursal))
                                            .Select(s => s.NombreSucursal)
                                            .ToList();

                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    NombreUsuario = user.NombreUsuario,
                    Email = user.Email
                });
            }
            return userViewModels;
        }
    }
}