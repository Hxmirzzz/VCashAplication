using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Filters;
using VCashApp.Models;
using VCashApp.Models.ViewModels.Range;
using VCashApp.Services.DTOs;
using VCashApp.Services;
using VCashApp.Services.Range;

namespace VCashApp.Controllers
{
    /// <summary>
    /// Controlador para administrar rangos de atención (AdmRangos).
    /// Mantiene el mismo patrón de tus controladores (ruta, permisos, ViewBags, AJAX/JSON, logging).
    /// </summary>
    [Authorize]
    [Route("/Range")]
    public class RangeController : BaseController
    {
        private readonly IRangeService _rangeService;
        private readonly ILogger<RangeController> _logger;
        private const string CodVista = "RANGE";

        public RangeController(
            IRangeService rangeService,
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<RangeController> logger)
            : base(context, userManager)
        {
            _rangeService = rangeService;
            _logger = logger;
        }

        /// <summary>
        /// Setea los ViewBags comunes para la UI de rangos (drop-downs, permisos, datos de cabecera).
        /// </summary>
        private async Task SetCommonViewBagsRangesAsync(ApplicationUser currentUser, string pageName)
        {
            await base.SetCommonViewBagsBaseAsync(currentUser, pageName);

            ViewBag.AvailableClients = await _rangeService.GetClientsForDropdownAsync();

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            ViewBag.HasCreatePermission = await HasPermisionForView(userRoles, CodVista, PermissionType.Create);
            ViewBag.HasEditPermission = await HasPermisionForView(userRoles, CodVista, PermissionType.Edit);
            ViewBag.HasViewPermission = await HasPermisionForView(userRoles, CodVista, PermissionType.View);
        }

        /// <summary>
        /// Muestra el dashboard principal de Rangos, permitiendo filtrar y ver los registros.
        /// </summary>
        [HttpGet("Index")]
        [RequiredPermission(PermissionType.View, CodVista)]
        public async Task<IActionResult> Index(
            string? search, int? clientId, bool? rangeStatus, int page = 1, int pageSize = 15)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsRangesAsync(currentUser, "Rangos de Atención");
            bool isAdmin = ViewBag.IsAdmin;

            RangeDashboardViewModel vm = await _rangeService.GetPagedAsync(search, clientId, rangeStatus, page, pageSize);

            ViewBag.CurrentPage = vm.CurrentPage;
            ViewBag.PageSize = vm.PageSize;
            ViewBag.TotalPages = vm.TotalPages;
            ViewBag.TotalData = vm.TotalData;
            ViewBag.SearchTerm = vm.SearchTerm;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_RangeTablePartial", vm.Ranges);
            }

            return View(vm);
        }

        /// <summary>
        /// Muestra el formulario de creación.
        /// </summary>
        [HttpGet("Create")]
        [RequiredPermission(PermissionType.Create, CodVista)]
        public async Task<IActionResult> Create()
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsRangesAsync(currentUser, "Crear Rango");

            var vm = await _rangeService.PrepareCreateAsync();
            return View(vm);
        }

        /// <summary>
        /// Procesa la creación de un rango. 
        /// Aplica validación de unicidad por (cliente + schedule_key) en DB; aquí se captura el error y se retorna JSON o View.
        /// </summary>
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Create, CodVista)]
        public async Task<IActionResult> Create(RangeFormViewModel vm)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            await SetCommonViewBagsRangesAsync(currentUser, "Crear Rango");

            if (!ModelState.IsValid)
            {
                var fieldErrors = ModelState
                    .Where(kvp => kvp.Value.Errors.Any())
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray());

                _logger.LogWarning(
                    "Usuario: {User} | IP: {IP} | Acción: AdmRangos - Modelo Inválido | Errores: {@Errors}",
                    currentUser.UserName, IpAddressForLogging, fieldErrors);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(ServiceResult.FailureResult("Hay errores en el formulario.", errors: fieldErrors));

                vm.AvailableClients = await _rangeService.GetClientsForDropdownAsync();
                return View(vm);
            }

            try
            {
                var (ok, message, id) = await _rangeService.CreateAsync(vm);
                if (!ok)
                {
                    ModelState.AddModelError(string.Empty, message ?? "No se pudo crear el rango.");

                    _logger.LogWarning(
                        "Usuario: {User} | IP: {IP} | Acción: Error al crear rango | Mensaje: {Msg}",
                        currentUser.UserName, IpAddressForLogging, message);

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(ServiceResult.FailureResult(message ?? "No se pudo crear el rango."));

                    vm.AvailableClients = await _rangeService.GetClientsForDropdownAsync();
                    return View(vm);
                }

                TempData["SuccessMessage"] = "Rango creado correctamente.";
                _logger.LogInformation(
                    "Usuario: {User} | IP: {IP} | Acción: Rango creado | Id={Id}",
                    currentUser.UserName, IpAddressForLogging, id);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(ServiceResult.SuccessResult("Rango creado correctamente.", id));

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Usuario: {User} | IP: {IP} | Acción: Excepción al crear rango.",
                    currentUser.UserName, IpAddressForLogging);

                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al crear el rango.");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(ServiceResult.FailureResult("Ocurrió un error inesperado."));

                vm.AvailableClients = await _rangeService.GetClientsForDropdownAsync();
                return View(vm);
            }
        }

        /// <summary>
        /// Vista de edición.
        /// </summary>
        [HttpGet("Edit/{id:int}")]
        [RequiredPermission(PermissionType.Edit, CodVista)]
        public async Task<IActionResult> Edit(int id)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsRangesAsync(currentUser, "Editar Rango");

            var vm = await _rangeService.PrepareEditAsync(id);
            if (vm == null) return NotFound();

            return View(vm);
        }

        /// <summary>
        /// Procesa la edición (validación de unicidad en DB).
        /// </summary>
        [HttpPost("Edit")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, CodVista)]
        public async Task<IActionResult> Edit(RangeFormViewModel vm)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            await SetCommonViewBagsRangesAsync(currentUser, "Editar Rango");

            if (!ModelState.IsValid)
            {
                var fieldErrors = ModelState
                    .Where(kvp => kvp.Value.Errors.Any())
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray());

                _logger.LogWarning(
                    "Usuario: {User} | IP: {IP} | Acción: AdmRangos - Modelo Inválido (Edit) | Errores: {@Errors}",
                    currentUser.UserName, IpAddressForLogging, fieldErrors);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(ServiceResult.FailureResult("Hay errores en el formulario.", errors: fieldErrors));

                vm.AvailableClients = await _rangeService.GetClientsForDropdownAsync();
                return View(vm);
            }

            try
            {
                var (ok, message) = await _rangeService.UpdateAsync(vm);
                if (!ok)
                {
                    ModelState.AddModelError(string.Empty, message ?? "No se pudo actualizar el rango.");

                    _logger.LogWarning(
                        "Usuario: {User} | IP: {IP} | Acción: Error al actualizar rango | Mensaje: {Msg}",
                        currentUser.UserName, IpAddressForLogging, message);

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(ServiceResult.FailureResult(message ?? "No se pudo actualizar el rango."));

                    vm.AvailableClients = await _rangeService.GetClientsForDropdownAsync();
                    return View(vm);
                }

                TempData["SuccessMessage"] = "Rango actualizado.";
                _logger.LogInformation(
                    "Usuario: {User} | IP: {IP} | Acción: Rango actualizado | Id={Id}",
                    currentUser.UserName, IpAddressForLogging, vm.Id);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(ServiceResult.SuccessResult("Rango actualizado.", vm.Id));

                return RedirectToAction(nameof(Details), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Usuario: {User} | IP: {IP} | Acción: Excepción al actualizar rango.",
                    currentUser.UserName, IpAddressForLogging);

                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al actualizar el rango.");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(ServiceResult.FailureResult("Ocurrió un error inesperado."));

                vm.AvailableClients = await _rangeService.GetClientsForDropdownAsync();
                return View(vm);
            }
        }

        /// <summary>
        /// Detalle del rango.
        /// </summary>
        [HttpGet("Details/{id:int}")]
        [RequiredPermission(PermissionType.View, CodVista)]
        public async Task<IActionResult> Details(int id)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsRangesAsync(currentUser, "Detalle de Rango");

            var dto = await _rangeService.GetByIdAsync(id);
            if (dto == null) return NotFound();

            return View(dto);
        }

        /// <summary>
        /// Desactiva (soft-delete) un rango.
        /// </summary>
        [HttpPost("Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, CodVista)] // usa Edit si no manejas permiso Delete
        public async Task<IActionResult> Delete(int id)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            try
            {
                var (ok, message) = await _rangeService.DeleteAsync(id);
                if (!ok)
                {
                    TempData["ErrorMessage"] = message ?? "No se pudo desactivar el rango.";
                    _logger.LogWarning(
                        "Usuario: {User} | IP: {IP} | Acción: Error al desactivar rango | Mensaje: {Msg}",
                        currentUser.UserName, IpAddressForLogging, message);

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(ServiceResult.FailureResult(message ?? "No se pudo desactivar el rango."));
                }
                else
                {
                    TempData["SuccessMessage"] = "Rango desactivado.";
                    _logger.LogInformation(
                        "Usuario: {User} | IP: {IP} | Acción: Rango desactivado | Id={Id}",
                        currentUser.UserName, IpAddressForLogging, id);

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(ServiceResult.SuccessResult("Rango desactivado.", id));
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Usuario: {User} | IP: {IP} | Acción: Excepción al desactivar rango.",
                    currentUser.UserName, IpAddressForLogging);

                TempData["ErrorMessage"] = "Ocurrió un error inesperado.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(ServiceResult.FailureResult("Ocurrió un error inesperado."));

                return RedirectToAction(nameof(Index));
            }
        }

        // =======================
        //  Endpoints utilitarios
        // =======================

        /// <summary>
        /// Valida en caliente (AJAX) si la combinación de cliente + horarios ya existe (usa la misma lógica del servicio).
        /// Útil en la UI para avisar antes de postear el form.
        /// </summary>
        [HttpPost("ValidateUnique")]
        [RequiredPermission(PermissionType.View, CodVista)]
        public async Task<IActionResult> ValidateUnique([FromBody] RangeFormViewModel vm)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null)
                return Json(new { success = false, message = "No autorizado.", code = "unauthorized" });

            if (vm == null || vm.ClientId <= 0)
                return Json(new { success = false, message = "Solicitud inválida.", code = "bad_request" });

            try
            {
                var (ok, msg) = await _rangeService.ValidateUniqueAsync(vm);

                if (ok)
                    return Json(new { success = true, message = "Combinación disponible.", code = "ok" });

                // Duplicado real
                return Json(new { success = false, message = msg ?? "La combinación ya existe.", code = "duplicate" });
            }
            catch
            {
                return Json(new { success = false, message = "Error al validar.", code = "error" });
            }
        }
    }
}