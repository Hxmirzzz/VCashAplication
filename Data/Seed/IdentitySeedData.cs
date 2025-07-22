using Microsoft.AspNetCore.Identity;
using VCashApp.Models; // Para ApplicationUser
using VCashApp.Models.Entities; // Para AdmSucursal, PermisosPerfil, AdmVista
using System.Linq;
using System.Threading.Tasks;
using System;
using Serilog; // Para logging
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; // Para Claims

namespace VCashApp.Data.Seed
{
    public static class IdentitySeedData
    {
        public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roleNames = {
                "Admin", "Planeador", "CEF", "Supervisor",
                "ADATM01", "ADCEF01", "ADCF01", "ADGRG01", "ADMTF01",
                "ADSER01", "ADSER02", "ADTH01", "ADTI01",
                "COCEF01", "COSTV01", "GRSER001", "SUPTV01", "Seguridad", "SupervisorSeguridad", "TalentoHumano"
            };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                    Log.Information("[SeedData] Rol '{RoleName}' creado.", roleName);
                }
                else
                {
                    Log.Information("[SeedData] Rol '{RoleName}' ya existe.", roleName);
                }
            }
        }

        public static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, AppDbContext context)
        {
            // Usuario Admin
            var adminUser = await userManager.FindByNameAsync("adminvcash");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser { UserName = "adminvcash", Email = "admin@vcash.com", NombreUsuario = "Administrador del Sistema", EmailConfirmed = true, PhoneNumberConfirmed = true };
                var result = await userManager.CreateAsync(adminUser, "Vatco2026*");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Log.Information($"[SeedData] Usuario 'adminvcash' creado y asignado al rol 'Admin'.");
                }
                else Log.Error($"[SeedData] Error al crear usuario 'adminvcash': {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
            else { Log.Information("[SeedData] Usuario 'adminvcash' ya existe."); }

            // Usuario Planeador
            var planeadorUser = await userManager.FindByNameAsync("planeador1");
            if (planeadorUser == null)
            {
                planeadorUser = new ApplicationUser { UserName = "planeador1", Email = "planeador1@vcash.com", NombreUsuario = "Juan Planeador", EmailConfirmed = true, PhoneNumberConfirmed = true };
                var result = await userManager.CreateAsync(planeadorUser, "Password123!");
                if (result.Succeeded) await userManager.AddToRoleAsync(planeadorUser, "Planeador");
                Log.Information("[SeedData] Usuario 'planeador1' creado y asignado al rol 'Planeador'.");
            }
            else { Log.Information("[SeedData] Usuario 'planeador1' ya existe."); }

            var planeadorMedellin = await userManager.FindByNameAsync("planeador2");
            if (planeadorMedellin == null)
            {
                planeadorMedellin = new ApplicationUser { UserName = "planeador2", Email = "planeador2@vatco.com.co", NombreUsuario = "Ana Planeadora Medellín", EmailConfirmed = true, PhoneNumberConfirmed = true };
                var result = await userManager.CreateAsync(planeadorMedellin, "Password123!");
                if (result.Succeeded) await userManager.AddToRoleAsync(planeadorMedellin, "Planeador");
                Log.Information("[SeedData] Usuario 'planeador2' creado y asignado al rol 'Planeador'.");
            }
            else { Log.Information("[SeedData] Usuario 'planeador2' ya existe."); }

            // Usuario CEF
            var cefUser = await userManager.FindByNameAsync("cef1");
            if (cefUser == null)
            {
                cefUser = new ApplicationUser { UserName = "cef1", Email = "cef1@vcash.com", NombreUsuario = "Maria del Centro", EmailConfirmed = true, PhoneNumberConfirmed = true };
                var result = await userManager.CreateAsync(cefUser, "Password123!");
                if (result.Succeeded) await userManager.AddToRoleAsync(cefUser, "CEF");
                Log.Information($"[SeedData] Usuario 'cef1' creado y asignado al rol 'CEF'.");
            }
            else { Log.Information($"[SeedData] Usuario 'cef1' ya existe."); }

            // Usuario Supervisor
            var supervisorUser = await userManager.FindByNameAsync("supervisor1");
            if (supervisorUser == null)
            {
                supervisorUser = new ApplicationUser { UserName = "supervisor1", Email = "supervisor1@vcash.com", NombreUsuario = "Carlos Supervisor", EmailConfirmed = true, PhoneNumberConfirmed = true };
                var result = await userManager.CreateAsync(supervisorUser, "Password123!");
                if (result.Succeeded) await userManager.AddToRoleAsync(supervisorUser, "Supervisor");
                Log.Information("[SeedData] Usuario '{UserName}' creado y asignado al rol 'Supervisor'.", supervisorUser.UserName);
            }
            else { Log.Information($"[SeedData] Usuario 'supervisor1' ya existe."); }

            var supervisorBogota = await userManager.FindByNameAsync("supervisor2");
            if (supervisorBogota == null)
            {
                supervisorBogota = new ApplicationUser { UserName = "supervisor2", Email = "supervisor2@vcash.com", NombreUsuario = "Laura Supervisora Bogotá", EmailConfirmed = true, PhoneNumberConfirmed = true };
                var result = await userManager.CreateAsync(supervisorBogota, "Password123!");
                if (result.Succeeded) await userManager.AddToRoleAsync(supervisorBogota, "Supervisor");
                Log.Information("[SeedData] Usuario '{UserName}' creado y asignado al rol 'Supervisor'.", supervisorBogota.UserName);
            }
            else { Log.Information($"[SeedData] Usuario 'supervisor2' ya existe."); }

            var seguridadUser = await userManager.FindByNameAsync("seguridad1");
            if (seguridadUser == null)
            {
                seguridadUser = new ApplicationUser { UserName = "seguridad1", Email = "seguridad1@vcash.com", NombreUsuario = "Seguridad Central", EmailConfirmed = true, PhoneNumberConfirmed = true };
                var result = await userManager.CreateAsync(seguridadUser, "Password123!");
                if (result.Succeeded) await userManager.AddToRoleAsync(seguridadUser, "Seguridad");
                Log.Information("[SeedData] Usuario 'seguridad1' creado y asignado al rol 'Seguridad'.");
            }
            else { Log.Information("[SeedData] Usuario 'seguridad1' ya existe."); }

            var supervisorSeguridad = await userManager.FindByNameAsync("supervisorSeguridad");
            if (supervisorSeguridad == null)
            {
                supervisorSeguridad = new ApplicationUser { UserName = "supervisorseguridad1", Email = "supervisorseguridad@vcash.com", NombreUsuario = "Supervisor Seguridad", EmailConfirmed = true, PhoneNumberConfirmed = true };
                var result = await userManager.CreateAsync(supervisorSeguridad, "Password123!");
                if (result.Succeeded) await userManager.AddToRoleAsync(supervisorSeguridad, "SupervisorSeguridad");
                Log.Information("[SeedData] Usuario 'supervisorSeguridad' creado y asignado al rol 'SupervisorSeguridad'.");
            }
            else { Log.Information("[SeedData] Usuario 'supervisorSeguridad' ya existe."); }

            var talentoHumanoUser = await userManager.FindByNameAsync("talentohumano1");
            if (talentoHumanoUser == null)
            {
                talentoHumanoUser = new ApplicationUser { UserName = "talentohumano1", Email = "talentohumano@vcash.com", NombreUsuario = "Talento Humano", EmailConfirmed = true, PhoneNumberConfirmed = true };
                var result = await userManager.CreateAsync(talentoHumanoUser, "Password123!");
                if (result.Succeeded) await userManager.AddToRoleAsync(talentoHumanoUser, "TalentoHumano");
            }
            else { Log.Information("[SeedData] Usuario 'talentohumano1' ya existe."); }
        }

        public static async Task SeedBranchPermissionsAsync(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            Log.Information("[SeedData] Iniciando seeding de permisos por sucursal...");
            try
            {
                var bogotaSucursal = await context.AdmSucursales.FirstOrDefaultAsync(s => s.CodSucursal == 1);
                var medellinSucursal = await context.AdmSucursales.FirstOrDefaultAsync(s => s.CodSucursal == 2);

                var planeadorUser = await userManager.FindByNameAsync("planeador1");
                var cefUser = await userManager.FindByNameAsync("cef1");
                var supervisorUser = await userManager.FindByNameAsync("supervisor1");
                var planeadorMedellin = await userManager.FindByNameAsync("planeador2");
                var supervisorBogota = await userManager.FindByNameAsync("supervisor2");
                var seguridadUser = await userManager.FindByNameAsync("seguridad1");
                var supervisorSeguridad = await userManager.FindByNameAsync("supervisorseguridad1");
                var talentoHumanoUser = await userManager.FindByNameAsync("talentohumano1");

                if (planeadorUser != null && bogotaSucursal != null)
                {
                    var existingClaims = await userManager.GetClaimsAsync(planeadorUser);
                    if (!existingClaims.Any(c => c.Type == "SucursalId" && c.Value == bogotaSucursal.CodSucursal.ToString()))
                    {
                        await userManager.AddClaimAsync(planeadorUser, new Claim("SucursalId", bogotaSucursal.CodSucursal.ToString()));
                        Log.Information("[SeedData] Claim 'SucursalId:{SucursalId}' añadido a usuario '{UserName}'.", bogotaSucursal.CodSucursal, planeadorUser.UserName);
                    }
                }
                if (cefUser != null && bogotaSucursal != null)
                {
                    var existingClaims = await userManager.GetClaimsAsync(cefUser);
                    if (!existingClaims.Any(c => c.Type == "SucursalId" && c.Value == bogotaSucursal.CodSucursal.ToString()))
                    {
                        await userManager.AddClaimAsync(cefUser, new Claim("SucursalId", bogotaSucursal.CodSucursal.ToString()));
                        Log.Information("[SeedData] Claim 'SucursalId:{SucursalId}' añadido a usuario '{UserName}'.", bogotaSucursal.CodSucursal, cefUser.UserName);
                    }
                }
                if (supervisorUser != null && medellinSucursal != null)
                {
                    var existingClaims = await userManager.GetClaimsAsync(supervisorUser);
                    if (!existingClaims.Any(c => c.Type == "SucursalId" && c.Value == medellinSucursal.CodSucursal.ToString()))
                    {
                        await userManager.AddClaimAsync(supervisorUser, new Claim("SucursalId", medellinSucursal.CodSucursal.ToString()));
                        Log.Information("[SeedData] Claim 'SucursalId:{SucursalId}' añadido a usuario '{UserName}'.", medellinSucursal.CodSucursal, supervisorUser.UserName);
                    }
                }
                if (planeadorMedellin != null && medellinSucursal != null)
                {
                    var existingClaims = await userManager.GetClaimsAsync(planeadorMedellin);
                    if (!existingClaims.Any(c => c.Type == "SucursalId" && c.Value == medellinSucursal.CodSucursal.ToString()))
                    {
                        await userManager.AddClaimAsync(planeadorMedellin, new Claim("SucursalId", medellinSucursal.CodSucursal.ToString()));
                        Log.Information("[SeedData] Claim 'SucursalId:{SucursalId}' añadido a usuario '{UserName}'.", medellinSucursal.CodSucursal, planeadorMedellin.UserName);
                    }
                }

                if (supervisorBogota != null && bogotaSucursal != null)
                {
                    var existingClaims = await userManager.GetClaimsAsync(supervisorBogota);
                    if (!existingClaims.Any(c => c.Type == "SucursalId" && c.Value == bogotaSucursal.CodSucursal.ToString()))
                    {
                        await userManager.AddClaimAsync(supervisorBogota, new Claim("SucursalId", bogotaSucursal.CodSucursal.ToString()));
                        Log.Information("[SeedData] Claim 'SucursalId:{SucursalId}' añadido a usuario '{UserName}'.", bogotaSucursal.CodSucursal, supervisorBogota.UserName);
                    }
                }

                if (seguridadUser != null && bogotaSucursal != null)
                {
                    var existingClaims = await userManager.GetClaimsAsync(seguridadUser);
                    if (!existingClaims.Any(c => c.Type == "SucursalId" && c.Value == bogotaSucursal.CodSucursal.ToString()))
                    {
                        await userManager.AddClaimAsync(seguridadUser, new Claim("SucursalId", bogotaSucursal.CodSucursal.ToString()));
                        Log.Information("[SeedData] Claim 'SucursalId:{SucursalId}' añadido a usuario '{UserName}'.", bogotaSucursal.CodSucursal, seguridadUser.UserName);
                    }
                }

                if (supervisorSeguridad != null && bogotaSucursal != null)
                {
                    var existingClaims = await userManager.GetClaimsAsync(supervisorSeguridad);
                    if (!existingClaims.Any(c => c.Type == "SucursalId" && c.Value == bogotaSucursal.CodSucursal.ToString()))
                    {
                        await userManager.AddClaimAsync(supervisorSeguridad, new Claim("SucursalId", bogotaSucursal.CodSucursal.ToString()));
                        Log.Information("[SeedData] Claim 'SucursalId:{SucursalId}' añadido a usuario '{UserName}'.", bogotaSucursal.CodSucursal, supervisorSeguridad.UserName);
                    }
                }

                if (talentoHumanoUser != null && bogotaSucursal != null)
                {
                    var existingClaims = await userManager.GetClaimsAsync(talentoHumanoUser);
                    if (!existingClaims.Any(c => c.Type == "SucursalId" && c.Value == bogotaSucursal.CodSucursal.ToString()))
                    {
                        await userManager.AddClaimAsync(talentoHumanoUser, new Claim("SucursalId", bogotaSucursal.CodSucursal.ToString()));
                        Log.Information("[SeedData] Claim 'SucursalId:{SucursalId}' añadido a usuario '{UserName}'.", bogotaSucursal.CodSucursal, talentoHumanoUser.UserName);
                    }
                }

                if (talentoHumanoUser != null && medellinSucursal != null)
                {
                    var existingClaims = await userManager.GetClaimsAsync(talentoHumanoUser);
                    if (!existingClaims.Any(c => c.Type == "SucursalId" && c.Value == medellinSucursal.CodSucursal.ToString()))
                    {
                        await userManager.AddClaimAsync(talentoHumanoUser, new Claim("SucursalId", medellinSucursal.CodSucursal.ToString()));
                        Log.Information("[SeedData] Claim 'SucursalId:{SucursalId}' añadido a usuario '{UserName}'.", medellinSucursal.CodSucursal, talentoHumanoUser.UserName);
                    }
                }

                Log.Information("[SeedData] Seeding de permisos por sucursal completado.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[SeedData] ERROR durante el seeding de permisos por sucursal: {ErrorMessage}", ex.Message);
            }
        }

        public static async Task SeedPermissionsAsync(AppDbContext context, RoleManager<IdentityRole> roleManager)
        {
            Log.Information("[SeedData] Iniciando seeding de permisos de perfiles y vistas...");
            try
            {
                // --- 1. Crear Vistas (si no existen) ---
                string[] vistaNames = { "RUD", "RUDHIS", "REG", "CTY", "SUC", "REGHIS", "EMP" };
                foreach (var vistaName in vistaNames)
                {
                    if (!await context.AdmVistas.AnyAsync(v => v.CodVista == vistaName))
                    {
                        string nombreVistaDisplay = vistaName switch
                        {
                            "RUD" => "Rutas Diarias Operativas",
                            "RUDHIS" => "Historial de Rutas Diarias",
                            "REG" => "Registro de Empleados",
                            "REGHIS" => "Historial de Registro de Empleados",
                            "CTY" => "Ciudades",
                            "SUC" => "Sucursales",
                            "EMP" => "Empleados",
                            _ => $"Vista {vistaName}"
                        };
                        context.AdmVistas.Add(new AdmVista { CodVista = vistaName, NombreVista = nombreVistaDisplay });
                    }
                }
                // Guardar los cambios de vistas primero para que estén disponibles
                await context.SaveChangesAsync();
                Log.Information("[SeedData] Vistas creadas/actualizadas.");

                // --- 2. Asignar Permisos a Roles ---
                var adminRoleId = (await roleManager.FindByNameAsync("Admin"))?.Id;
                var planeadorRoleId = (await roleManager.FindByNameAsync("Planeador"))?.Id;
                var cefRoleId = (await roleManager.FindByNameAsync("CEF"))?.Id;
                var supervisorRoleId = (await roleManager.FindByNameAsync("Supervisor"))?.Id;
                var seguridadRoleId = (await roleManager.FindByNameAsync("Seguridad"))?.Id;
                var supervisorSeguridadRoleId = (await roleManager.FindByNameAsync("SupervisorSeguridad"))?.Id;
                var talentoHumanoRoleId = (await roleManager.FindByNameAsync("TalentoHumano"))?.Id;

                var rolesConIds = new Dictionary<string, string?>
                {
                    { "Admin", adminRoleId },
                    { "Planeador", planeadorRoleId },
                    { "CEF", cefRoleId },
                    { "Supervisor", supervisorRoleId },
                    { "Seguridad", seguridadRoleId },
                    { "SupervisorSeguridad", supervisorSeguridadRoleId },
                    { "TalentoHumano", talentoHumanoRoleId }
                };

                // Permisos para RUD
                var rudPermissions = new List<(string RoleName, bool CanView, bool CanCreate, bool CanEdit)>
                {
                    ( "Admin", true, true, true ),
                    ( "Planeador", true, true, true ),
                    ( "CEF", true, true, true ),
                    ( "Supervisor", true, false, true )
                };

                foreach (var perm in rudPermissions)
                {
                    if (rolesConIds.TryGetValue(perm.RoleName, out string? roleId) && roleId != null)
                    {
                        var permiso = await context.PermisosPerfil.FirstOrDefaultAsync(p => p.CodPerfilId == roleId && p.CodVista == "RUD");
                        if (permiso == null)
                        {
                            context.PermisosPerfil.Add(new PermisoPerfil
                            {
                                CodPerfilId = roleId,
                                CodVista = "RUD",
                                PuedeVer = perm.CanView,
                                PuedeCrear = perm.CanCreate,
                                PuedeEditar = perm.CanEdit
                            });
                            Log.Information("[SeedData] Permisos RUD asignados al rol {RoleName}.", perm.RoleName);
                        }
                        else
                        {
                            permiso.PuedeVer = perm.CanView;
                            permiso.PuedeCrear = perm.CanCreate;
                            permiso.PuedeEditar = perm.CanEdit;
                            context.PermisosPerfil.Update(permiso);
                            Log.Information("[SeedData] Permisos RUD actualizados para rol {RoleName}.", perm.RoleName);
                        }
                    }
                }

                // Permisos para RUDHIS (Historial de Rutas Diarias)
                var rudhisPermissions = new List<(string RoleName, bool CanView, bool CanCreate, bool CanEdit)>
                {
                    ( "Admin", true, false, false ),
                    ( "Planeador", true, false, false ),
                    ( "CEF", true, false, false ),
                    ( "Supervisor", true, false, false )
                };

                foreach (var perm in rudhisPermissions)
                {
                    if (rolesConIds.TryGetValue(perm.RoleName, out string? roleId) && roleId != null)
                    {
                        var permiso = await context.PermisosPerfil.FirstOrDefaultAsync(p => p.CodPerfilId == roleId && p.CodVista == "RUDHIS");
                        if (permiso == null)
                        {
                            context.PermisosPerfil.Add(new PermisoPerfil
                            {
                                CodPerfilId = roleId,
                                CodVista = "RUDHIS",
                                PuedeVer = perm.CanView,
                                PuedeCrear = perm.CanCreate,
                                PuedeEditar = perm.CanEdit
                            });
                            Log.Information("[SeedData] Permisos RUDHIS asignados al rol {RoleName}.", perm.RoleName);
                        }
                        else
                        {
                            permiso.PuedeVer = perm.CanView;
                            permiso.PuedeCrear = perm.CanCreate;
                            permiso.PuedeEditar = perm.CanEdit;
                            context.PermisosPerfil.Update(permiso);
                            Log.Information("[SeedData] Permisos RUDHIS actualizados para rol {RoleName}.", perm.RoleName);
                        }
                    }
                }

                var regPermissions = new List<(string RoleName, bool CanView, bool CanCreate, bool CanEdit)>
                {
                    ( "Admin", true, true, true ),
                    ( "Seguridad", true, true, false ),
                    ( "SupervisorSeguridad", true, true, true)
                };

                foreach (var perm in regPermissions)
                {
                    if (rolesConIds.TryGetValue(perm.RoleName, out string? roleId) && roleId != null)
                    {
                        var permiso = await context.PermisosPerfil.FirstOrDefaultAsync(p => p.CodPerfilId == roleId && p.CodVista == "REG");
                        if (permiso == null)
                        {
                            context.PermisosPerfil.Add(new PermisoPerfil
                            {
                                CodPerfilId = roleId,
                                CodVista = "REG",
                                PuedeVer = perm.CanView,
                                PuedeCrear = perm.CanCreate,
                                PuedeEditar = perm.CanEdit
                            });
                            Log.Information("[SeedData] Permisos REG asignados al rol {RoleName}.", perm.RoleName);
                        }
                        else
                        {
                            permiso.PuedeVer = perm.CanView;
                            permiso.PuedeCrear = perm.CanCreate;
                            permiso.PuedeEditar = perm.CanEdit;
                            context.PermisosPerfil.Update(permiso);
                            Log.Information("[SeedData] Permisos REG actualizados para rol {RoleName}.", perm.RoleName);
                        }
                    }
                }

                var regHisPermissions = new List<(string RoleName, bool CanView, bool CanCreate, bool CanEdit)>
                {
                    ( "Admin", true, true, true ),
                    ( "Seguridad", true, true, false ),
                    ( "SupervisorSeguridad", true, true, true )
                };

                foreach (var perm in regHisPermissions)
                {
                    if (rolesConIds.TryGetValue(perm.RoleName, out string? roleId) && roleId != null)
                    {
                        var permiso = await context.PermisosPerfil.FirstOrDefaultAsync(p => p.CodPerfilId == roleId && p.CodVista == "REGHIS");
                        if (permiso == null)
                        {
                            context.PermisosPerfil.Add(new PermisoPerfil
                            {
                                CodPerfilId = roleId,
                                CodVista = "REGHIS",
                                PuedeVer = perm.CanView,
                                PuedeCrear = perm.CanCreate,
                                PuedeEditar = perm.CanEdit
                            });
                            Log.Information("[SeedData] Permisos REGHIS asignados al rol {RoleName}.", perm.RoleName);
                        }
                        else
                        {
                            permiso.PuedeVer = perm.CanView;
                            permiso.PuedeCrear = perm.CanCreate;
                            permiso.PuedeEditar = perm.CanEdit;
                            context.PermisosPerfil.Update(permiso);
                            Log.Information("[SeedData] Permisos REGHIS actualizados para rol {RoleName}.", perm.RoleName);
                        }
                    }
                }

                var empPermissions = new List<(string RoleName, bool CanView, bool CanCreate, bool CanEdit)>
                {
                    ( "Admin", true, true, true ),
                    ( "TalentoHumano", true, true, true )
                };

                foreach (var perm in empPermissions)
                {
                    if (rolesConIds.TryGetValue(perm.RoleName, out string? roleId) && roleId != null)
                    {
                        var permiso = await context.PermisosPerfil.FirstOrDefaultAsync(p => p.CodPerfilId == roleId && p.CodVista == "EMP");
                        if (permiso == null)
                        {
                            context.PermisosPerfil.Add(new PermisoPerfil
                            {
                                CodPerfilId = roleId,
                                CodVista = "EMP",
                                PuedeVer = perm.CanView,
                                PuedeCrear = perm.CanCreate,
                                PuedeEditar = perm.CanEdit
                            });
                            Log.Information("[SeedData] Permisos EMP asignados al rol {RoleName}.", perm.RoleName);
                        }
                        else
                        {
                            permiso.PuedeVer = perm.CanView;
                            permiso.PuedeCrear = perm.CanCreate;
                            permiso.PuedeEditar = perm.CanEdit;
                            context.PermisosPerfil.Update(permiso);
                            Log.Information("[SeedData] Permisos EMP actualizados para rol {RoleName}.", perm.RoleName);
                        }
                    }
                }

                await context.SaveChangesAsync();
                Log.Information("[SeedData] Seeding de permisos de perfiles y vistas completado.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[SeedData] ERROR durante el seeding de permisos de perfiles y vistas: {ErrorMessage}", ex.Message);
            }
        }
    }
}