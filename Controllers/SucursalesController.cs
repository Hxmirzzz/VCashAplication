using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Serilog;
using Serilog.Context;
using VCashApp.Data;
using VCashApp.Models;
using VCashApp.Models.Entities;
using VCashApp.Enums;
using VCashApp.Filters;
using VCashApp.Services;

namespace VCashApp.Controllers
{
    public class SucursalesController : BaseController
    {
        public SucursalesController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager
        ) : base(context, userManager) // LLAMA AL CONSTRUCTOR DE BaseController
        {
            // private readonly IExportService _exportService;
            // _exportService = exportService;
        }

        // Método auxiliar para configurar ViewBags comunes
        private async Task SetCommonViewBagsAsync(ApplicationUser currentUser, string pageName)
        {
            await base.SetCommonViewBagsBaseAsync(currentUser, pageName);

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            bool hasSUCView = await HasPermisionForView(userRoles, "SUC", PermissionType.View);
            bool hasSUCCreate = await HasPermisionForView(userRoles, "SUC", PermissionType.Create);
            bool hasSUCEdit = await HasPermisionForView(userRoles, "SUC", PermissionType.Edit);
            //bool hasSUCDelete = await HasPermisionForView(userRoles, "SUC", PermissionType.Delete);

            ViewBag.canCreate = (bool)ViewBag.IsAdmin || hasSUCCreate;
            ViewBag.canEdit = (bool)ViewBag.IsAdmin || hasSUCEdit;
            //ViewBag.canDelete = (bool)ViewBag.IsAdmin || hasSUCDelete;
        }

        // GET: Sucursales/Index
        [HttpGet]
        [RequiredPermission(PermissionType.View, "SUC")] // Permiso para ver la vista SUC
        public async Task<IActionResult> Index(int? page, int pageSize = 15, string search = "", bool? estado = null)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null) return RedirectToAction("Login", "Account", new { Area = "Identity" });

                await SetCommonViewBagsAsync(currentUser, "Sucursales");

                IQueryable<AdmSucursal> query = _context.AdmSucursales.AsQueryable();
                if (estado.HasValue)
                {
                    query = query.Where(s => s.Estado == estado.Value);
                }
                if (!string.IsNullOrEmpty(search))
                {
                    string trimmedSearch = search.Trim().ToLower();
                    query = query.Where(s => s.NombreSucursal.ToLower().Contains(trimmedSearch) ||
                                             s.SiglasSucursal.ToLower().Contains(trimmedSearch) ||
                                             s.CodSucursal.ToString().Contains(trimmedSearch) ||
                                             (s.CoSucursal != null && s.CoSucursal.ToLower().Contains(trimmedSearch)));
                }

                var totalData = await query.CountAsync();
                page = Math.Max(page ?? 1, 1);
                pageSize = 15; // O el valor que desees
                int totalPages = (int)Math.Ceiling((double)totalData / pageSize);
                page = Math.Min(page.Value, Math.Max(1, totalPages));

                var data = await query.Skip((page.Value - 1) * pageSize).Take(pageSize).ToListAsync();

                ViewBag.CurrentEstado = estado;
                ViewBag.CurrentPage = page.Value;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalData = totalData;
                ViewBag.SearchTerm = search;
                ViewBag.PageSize = pageSize;

                var estadoSelectItems = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "-- Todos --", Selected = !estado.HasValue },
                    new SelectListItem { Value = "true", Text = "Activo", Selected = (estado.HasValue && estado.Value == true) },
                    new SelectListItem { Value = "false", Text = "Inactivo", Selected = (estado.HasValue && estado.Value == false) }
                };
                ViewBag.EstadosFilterList = estadoSelectItems;

                Log.Information("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a Lista de Sucursales | Cantidad de sucursales: {Count} | Respuesta: Acceso permitido | ", currentUser.UserName, ipAddress, data.Count);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("~/Views/Sucursales/_SucursalesTablePartial.cshtml", data);
                }

                return View(data);
            }
        }

        // --- ACCIONES CRUD BÁSICAS (Ejemplos, implementa la lógica completa) ---

        // GET: Sucursales/Details/5
        [HttpGet]
        [RequiredPermission(PermissionType.View, "SUC")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sucursal = await _context.AdmSucursales
                .FirstOrDefaultAsync(m => m.CodSucursal == id);
            if (sucursal == null)
            {
                return NotFound();
            }
            return View(sucursal);
        }

        // GET: Sucursales/Create
        [HttpGet]
        [RequiredPermission(PermissionType.Create, "SUC")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Sucursales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Create, "SUC")]
        public async Task<IActionResult> Create([Bind("CodSucursal,NombreSucursal,LatitudSucursal,LongitudSucursal,SiglasSucursal,CoSucursal,CodBancoRepublica,Estado")] AdmSucursal admSucursal)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null) return Unauthorized();

                if (ModelState.IsValid)
                {
                    _context.Add(admSucursal);
                    await _context.SaveChangesAsync();
                    Log.Information("| Usuario: {User} | Ip: {Ip} | Acción: Crear Sucursal | Sucursal Creada: {SucursalId} - {SucursalNombre} | Respuesta: Éxito |", currentUser.UserName, ipAddress, admSucursal.CodSucursal, admSucursal.NombreSucursal);
                    TempData["SuccessMessage"] = "Sucursal creada exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Crear Sucursal | Detalles: Validación de modelo fallida | Errores: {ModelStateErrors} | Respuesta: Fallo |", currentUser.UserName, ipAddress, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList());
                TempData["ErrorMessage"] = "No se pudo crear la sucursal. Revise los datos.";
                return View(admSucursal);
            }
        }

        // GET: Sucursales/Edit/5
        [HttpGet]
        [RequiredPermission(PermissionType.Edit, "SUC")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var admSucursal = await _context.AdmSucursales.FindAsync(id);
            if (admSucursal == null)
            {
                return NotFound();
            }
            return View(admSucursal);
        }

        // POST: Sucursales/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "SUC")]
        public async Task<IActionResult> Edit(int id, [Bind("CodSucursal,NombreSucursal,LatitudSucursal,LongitudSucursal,SiglasSucursal,CoSucursal,CodBancoRepublica,Estado")] AdmSucursal admSucursal)
        {
            if (id != admSucursal.CodSucursal)
            {
                return NotFound();
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null) return Unauthorized();

                if (ModelState.IsValid)
                {
                    try
                    {
                        _context.Update(admSucursal);
                        await _context.SaveChangesAsync();
                        Log.Information("| Usuario: {User} | Ip: {Ip} | Acción: Editar Sucursal | Sucursal Editada: {SucursalId} - {SucursalNombre} | Respuesta: Éxito |", currentUser.UserName, ipAddress, admSucursal.CodSucursal, admSucursal.NombreSucursal);
                        TempData["SuccessMessage"] = "Sucursal actualizada exitosamente.";
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        if (!AdmSucursalExists(admSucursal.CodSucursal))
                        {
                            return NotFound();
                        }
                        else
                        {
                            Log.Error(ex, "| Usuario: {User} | Ip: {Ip} | Acción: Editar Sucursal | Error de concurrencia al editar Sucursal: {SucursalId} | Respuesta: Fallo |", currentUser.UserName, ipAddress, admSucursal.CodSucursal);
                            TempData["ErrorMessage"] = "Error de concurrencia al actualizar la sucursal. Intente de nuevo.";
                            throw;
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }
                Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Editar Sucursal | Detalles: Validación de modelo fallida | Errores: {ModelStateErrors} | Respuesta: Fallo |", currentUser.UserName, ipAddress, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList());
                TempData["ErrorMessage"] = "No se pudo actualizar la sucursal. Revise los datos.";
                return View(admSucursal);
            }
        }

        // GET: Sucursales/Delete/5
        [HttpGet]
        //[RequiredPermission(PermissionType.Delete, "SUC")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var admSucursal = await _context.AdmSucursales
                .FirstOrDefaultAsync(m => m.CodSucursal == id);
            if (admSucursal == null)
            {
                return NotFound();
            }

            return View(admSucursal);
        }

        // POST: Sucursales/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        //[RequiredPermission(PermissionType.Delete, "SUC")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null) return Unauthorized();

                var admSucursal = await _context.AdmSucursales.FindAsync(id);
                if (admSucursal != null)
                {
                    _context.AdmSucursales.Remove(admSucursal);
                    await _context.SaveChangesAsync();
                    Log.Information("| Usuario: {User} | Ip: {Ip} | Acción: Eliminar Sucursal | Sucursal Eliminada: {SucursalId} - {SucursalNombre} | Respuesta: Éxito |", currentUser.UserName, ipAddress, admSucursal.CodSucursal, admSucursal.NombreSucursal);
                    TempData["SuccessMessage"] = "Sucursal eliminada exitosamente.";
                }
                else
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Eliminar Sucursal | Detalles: Sucursal con ID {SucursalId} no encontrada para eliminación | Respuesta: Fallo (NotFound) |", currentUser.UserName, ipAddress, id);
                    TempData["ErrorMessage"] = "Sucursal no encontrada para eliminar.";
                    return NotFound();
                }
                return RedirectToAction(nameof(Index));
            }
        }

        private bool AdmSucursalExists(int id)
        {
            return _context.AdmSucursales.Any(e => e.CodSucursal == id);
        }
    }
}