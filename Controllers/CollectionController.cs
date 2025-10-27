using DocumentFormat.OpenXml.Vml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Runtime.Intrinsics.X86;
using VCashApp.Controllers;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Filters;
using VCashApp.Models;
using VCashApp.Models.ViewModels.CentroEfectivo.Shared;
using VCashApp.Services;
using VCashApp.Services.Cef;
using VCashApp.Services.CentroEfectivo.Collection.Application;
using VCashApp.Services.CentroEfectivo.Shared.Domain;
using VCashApp.Services.CentroEfectivo.Shared.Incidents;
using VCashApp.Services.DTOs;

[Authorize]
[Route("Collection")]
public sealed class CollectionController : BaseController
{
    private readonly ICollectionService _svc;
    private readonly ICollectionReadService _read;
    private readonly ICefCatalogRepository _catalogs;
    private readonly ICefIncidentService _incidents;
    private readonly ICefContainerRepository _containers;
    private readonly UserManager<ApplicationUser> _um;
    private readonly ICefTransactionQueries _transactions;
    private readonly AppDbContext _db;

    public CollectionController(
        AppDbContext context,
        UserManager<ApplicationUser> um,
        ICollectionService svc,
        ICollectionReadService read,
        ICefCatalogRepository catalogs,
        ICefIncidentService incidents,
        ICefContainerRepository containers,
        ICefTransactionQueries transactions
    ) : base(context, um)
    {
        _svc = svc;
        _read = read;
        _um = um;
        _catalogs = catalogs;
        _incidents = incidents;
        _db = context;
        _containers = containers;
        _transactions = transactions;
    }

    /// <summary>
    /// Método auxiliar para configurar ViewBags comunes específicos de CEF.
    /// Hereda y extiende el SetCommonViewBagsBaseAsync del BaseController.
    /// </summary>
    /// <param name="currentUser">El usuario actual autenticado.</param>
    /// <param name="pageName">El nombre de la página para el ViewBag.</param>
    /// <returns>Tarea asíncrona completada.</returns>
    private async Task SetCommonViewBagsCefAsync(ApplicationUser currentUser, string pageName, params string[] codVistas)
    {
        await base.SetCommonViewBagsBaseAsync(currentUser, pageName);
        bool isAdmin = ViewBag.IsAdmin;

        var (sucursales, estados) = await _transactions.GetDropdownListsAsync(currentUser.Id, isAdmin);
        ViewBag.AvailableBranches = sucursales;
        ViewBag.TransactionStatuses = estados;

        var vistas = (codVistas != null && codVistas.Length > 0)
            ? codVistas
            : new[] { "CEF_REC", "CEF_DEL", "CEF_COL", "CEF_SUP" };

        var userRoles = await _userManager.GetRolesAsync(currentUser);

        async Task<bool> HasAsync(PermissionType p)
        {
            foreach (var v in vistas)
            {
                if (await HasPermisionForView(userRoles, v, p))
                    return true;
            }
            return false;
        }

        ViewBag.HasCreate = await HasAsync(PermissionType.Create);
        ViewBag.HasEdit = await HasAsync(PermissionType.Edit);
        ViewBag.HasView = await HasAsync(PermissionType.View);
        // ViewBag.HasDeletePermission = await HasPermisionForView(userRoles, "CEF", PermissionType.Delete);
    }

    [HttpGet("Checkin")]
    [RequiredPermission(PermissionType.Create, "CEF_REC")]
    public async Task<IActionResult> Checkin(string? serviceOrderId, string? routeId)
    {
        var user = await GetCurrentApplicationUserAsync();
        if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

        await SetCommonViewBagsCefAsync(user, "CEF - Recolección", "CEF_COL", "CEF_REC", "CEF_SUP", "CEF_DEL");

        var roleNames = await _userManager.GetRolesAsync(user);
        ViewBag.HasCreate = await HasPermisionForView(roleNames, "CEF_REC", PermissionType.Create);

        var vm = await _read.GetCheckinAsync(serviceOrderId, user.Id, IpAddressForLogging);
        if (!string.IsNullOrWhiteSpace(routeId)) vm.RouteId = routeId;

        return View("~/Views/Collection/Checkin.cshtml", vm);
    }

    [HttpPost("Checkin")]
    [ValidateAntiForgeryToken]
    [RequiredPermission(PermissionType.Create, "CEF_REC")]
    public async Task<IActionResult> Checkin([FromForm] CefTransactionCheckinViewModel vm)
    {
        var user = await GetCurrentApplicationUserAsync();
        if (user is null) return Unauthorized();

        vm.Currencies = new List<SelectListItem> { new("COP", "COP"), new("USD", "USD"), new("EUR", "EUR") };
        vm.RegistrationUserName = user.NombreUsuario ?? "N/A";
        vm.IPAddress = IpAddressForLogging;

        if (!ModelState.IsValid)
            return View("~/Views/Collection/Checkin.cshtml", vm);

        try
        {
            var cmd = new CreateCollectionCmd(
                SlipNumber: vm.SlipNumber,
                ServiceOrderId: vm.ServiceOrderId!,
                Currency: vm.Currency ?? "COP",
                DeclaredBagCount: vm.DeclaredBagCount,
                DeclaredTotalValue: vm.TotalDeclaredValue,
                Observations: vm.InformativeIncident
            );

            var txId = await _svc.CreateCollectionAsync(cmd, user.Id);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var url = Url.Action("Reception", "Cef");
                return Json(ServiceResult.SuccessResult("Check-in exitoso.", new { txId, url }));
            }

            return RedirectToAction("Reception", "Cef");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View("~/Views/Collection/Checkin.cshtml", vm);
        }
        catch
        {
            ModelState.AddModelError("", "Ocurrió un error inesperado al registrar el Check-in.");
            return View("~/Views/Collection/Checkin.cshtml", vm);
        }
    }

    [HttpGet("Process/{txId:int}")]
    [RequiredPermission(PermissionType.Edit, "CEF_COL")]
    public async Task<IActionResult> Process(int txId)
    {
        var user = await GetCurrentApplicationUserAsync();
        if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

        await SetCommonViewBagsCefAsync(user, "CEF - Recolección", "CEF_COL", "CEF_REC", "CEF_SUP", "CEF_DEL");

        var caps = await GetCefCapsAsync(user);
        ViewBag.CanCountBills = caps.CanCountBills;
        ViewBag.CanCountCoins = caps.CanCountCoins;
        ViewBag.CanIncCreateEdit = caps.CanIncCreateEdit;
        ViewBag.CanIncApprove = caps.CanIncApprove;
        ViewBag.CanFinalize = caps.CanFinalize;

        var vm = await _read.GetProcessPageAsync(txId);
        if (vm is null) return NotFound();

        ViewBag.DenomsJson = await _catalogs.BuildDenomsJsonForTransactionAsync(txId);
        ViewBag.QualitiesJson = await _catalogs.BuildQualitiesJsonAsync();
        ViewBag.BanksJson = await _catalogs.BuildBankEntitiesJsonAsync();

        var pointCaps = await _containers.GetPointCapsAsync(vm.Service.ServiceOrderId);
        ViewBag.PointCapsJson = System.Text.Json.JsonSerializer.Serialize(caps);

        ViewBag.IncidentTypesForEdit = (await _incidents.GetAllIncidentTypesAsync())
            .Select(it => new SelectListItem { Value = it.Id.ToString(), Text = it.Description })
            .ToList();

        ViewBag.IncidentTypes = (await _incidents.GetAllIncidentTypesAsync())
           .Select(it => new SelectListItem { Value = (it.Code ?? "").Trim(), Text = it.Description })
           .ToList();

        ViewData["Title"] = "Procesamiento de Bolsas";

        return View("~/Views/Collection/Process.cshtml", vm);
    }

    [HttpPost("Process/{txId:int}")]
    [ValidateAntiForgeryToken]
    [RequiredPermission(PermissionType.Edit, "CEF_COL")]
    public async Task<IActionResult> Process([FromRoute] int txId, [FromForm] SaveCollectionContainersCmd cmd)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { ok = false, message = "Datos inválidos.", errors = ModelState });

        var user = await _um.GetUserAsync(User);
        if (user is null) return Unauthorized();

        try
        {
            await _svc.SaveContainersAsync(txId, cmd, user.Id);
            await _svc.RecalcTotalsAndNetDiffAsync(txId);
            return Json(new { success = true, message = "Guardado." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Error inesperado al guardar." });
        }
    }


    [HttpPost("Finalize/{txId:int}")]
    [ValidateAntiForgeryToken]
    [RequiredPermission(PermissionType.Edit, "CEF_COL")]
    public async Task<IActionResult> Finalize(int txId)
    {
        var user = await _um.GetUserAsync(User);
        if (user is null) return Unauthorized();

        try
        {
            await _svc.FinalizeAsync(txId, user.Id);
            return RedirectToAction("Collections", "Cef");
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Process), new { txId });
        }
        catch
        {
            TempData["Error"] = "Error inesperado al finalizar la transacción.";
            return RedirectToAction(nameof(Process), new { txId });
        }
    }
    // ======== INCIDENTES (AJAX) ========

    [HttpGet("ListIncidents")]
    public async Task<IActionResult> ListIncidents(int transactionId, int? containerId)
    {
        var list = await _incidents.GetIncidentsAsync(transactionId, containerId, null);
        var data = list.Select(i => new
        {
            i.Id,
            Type = i.IncidentType?.Code,
            i.Description,
            i.AffectedAmount,
            i.AffectedDenomination,
            i.AffectedQuantity,
            i.IncidentStatus,
            Date = i.IncidentDate.ToString("yyyy-MM-dd HH:mm")
        });
        return Json(new { ok = true, data });
    }

    [HttpPost("CreateIncident")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateIncident([FromForm] CefIncidentViewModel vm)
    {
        var user = await GetCurrentApplicationUserAsync();
        if (user is null) return Unauthorized();

        if (vm == null)
            return BadRequest(new { ok = false, message = "Datos incompletos para registrar novedad." });

        try
        {
            var code = Request.Form["IncidentTypeCode"].ToString();
            if (!string.IsNullOrWhiteSpace(code)
                && IncidentTypeCode.TryFromCode(code, out var cat))
            {
                vm.IncidentType = cat;
            }

            var inc = await _incidents.RegisterIncidentAsync(vm, user.Id);
            if (inc.CefTransactionId.HasValue)
                await _svc.RecalcTotalsAndNetDiffAsync(inc.CefTransactionId.Value);

            return Json(new { ok = true, message = "Novedad registrada." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { ok = false, message = ex.Message });
        }
    }

    [HttpPost("ResolveIncident")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveIncident(int id, string newStatus = "Ajustada")
    {
        var ok = await _incidents.ResolveIncidentAsync(id, newStatus);
        if (!ok) return BadRequest(new { ok = false, message = "No se pudo resolver la novedad." });

        var inc = await _incidents.GetIncidentByIdAsync(id);
        if (inc?.CefTransactionId != null)
            await _svc.RecalcTotalsAndNetDiffAsync(inc.CefTransactionId.Value);

        return Json(new { ok = true, message = "Novedad actualizada." });
    }

    [HttpGet("GetIncident")]
    public async Task<IActionResult> GetIncident(int id)
    {
        var inc = await _incidents.GetIncidentByIdAsync(id);
        if (inc == null) return NotFound(new { ok = false });

        var typeCode = await _db.CefIncidentTypes
            .Where(t => t.Id == inc.IncidentTypeId)
            .Select(t => t.Code)
            .FirstOrDefaultAsync() ?? "";

        return Json(new
        {
            ok = true,
            data = new
            {
                inc.Id,
                inc.CefTransactionId,
                inc.CefContainerId,
                IncidentTypeId = inc.IncidentTypeId,
                IncTypeCode = typeCode,
                inc.Description,
                inc.AffectedDenomination,
                inc.AffectedQuantity,
                inc.AffectedAmount,
                inc.IncidentStatus
            }
        });
    }

    [HttpPost("UpdateIncident")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateIncident(int id)
    {
        try
        {
            int? typeId = int.TryParse(Request.Form["IncidentTypeId"], out var ti) ? ti : null;
            string? typeCode = Request.Form["IncidentTypeCode"];
            int? denomId = int.TryParse(Request.Form["AffectedDenomination"], out var dd) ? dd : null;
            int? qty = int.TryParse(Request.Form["AffectedQuantity"], out var qq) ? qq : null;
            decimal? amount = decimal.TryParse(Request.Form["AffectedAmount"], out var aa) ? aa : null;
            string? desc = Request.Form["Description"];

            CefIncidentTypeCategoryEnum? typeEnum = null;
            if (!string.IsNullOrWhiteSpace(typeCode))
            {
                if (!IncidentTypeCode.TryFromCode(typeCode, out var parsedType))
                    return BadRequest(new { ok = false, message = $"Código de tipo de novedad no válido: {typeCode}" });
                typeEnum = parsedType;
            }

            var ok = await _incidents.UpdateReportedIncidentAsync(
                id,
                newTypeId: typeId,
                newType: typeEnum,
                newDenominationId: denomId,
                newQuantity: qty,
                newAmount: amount,
                newDescription: desc
            );

            var inc = await _incidents.GetIncidentByIdAsync(id);
            if (inc?.CefTransactionId != null)
                await _svc.RecalcTotalsAndNetDiffAsync(inc.CefTransactionId.Value);

            return Json(new { ok });
        }
        catch (Exception ex)
        {
            return BadRequest(new { ok = false, message = ex.Message });
        }
    }

    [HttpGet("CheckPendingIncidents")]
    public async Task<IActionResult> CheckPendingIncidents(int transactionId)
    {
        var hasPending = await _incidents.HasPendingIncidentsByTransactionAsync(transactionId);
        return Json(new { ok = true, hasPending });
    }

    [HttpPost("DeleteIncident")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteIncident(int id)
    {
        try
        {
            var inc = await _incidents.GetIncidentByIdAsync(id);
            if (inc == null)
                return NotFound(new { ok = false, message = "Novedad no encontrada." });

            var ok = await _incidents.DeleteReportedIncidentAsync(id);

            if (inc.CefTransactionId.HasValue)
                await _svc.RecalcTotalsAndNetDiffAsync(inc.CefTransactionId.Value);

            return Json(new { ok });
        }
        catch (Exception ex)
        {
            return BadRequest(new { ok = false, message = ex.Message });
        }
    }

    // ======== UTILIDADES ========
    [HttpGet("Totals")]
    public async Task<IActionResult> GetTotals(int transactionId)
    {
        var tx = await _db.CefTransactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == transactionId);
        if (tx == null) return NotFound(new { ok = false, message = "Transacción no encontrada." });

        var effect = await _incidents.SumApprovedEffectByTransactionAsync(transactionId);
        var totalDeclared = tx.TotalDeclaredValue;
        var totalCounted = tx.TotalCountedValue;
        var totalOverall = tx.TotalDeclaredValue + tx.TotalCountedValue;
        var netDiff = (totalCounted - totalDeclared) + effect;

        return Json(new { ok = true, totalDeclared, totalCounted, totalOverall, effect, netDiff });
    }
}