using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Filters;
using VCashApp.Models;
using VCashApp.Models.ViewModels.CentroEfectivo.Provision;
using VCashApp.Models.ViewModels.CentroEfectivo.Shared;
using VCashApp.Services;
using VCashApp.Services.CentroEfectivo.Provision.Application;
using VCashApp.Services.CentroEfectivo.Shared.Domain;
using VCashApp.Services.CentroEfectivo.Shared.Incidents;
using VCashApp.Services.DTOs;

namespace VCashApp.Controllers
{
    [Authorize]
    [Route("Provision")]
    public sealed class ProvisionController : BaseController
    {
        private readonly IProvisionService _svc;
        private readonly IProvisionReadService _read;
        private readonly UserManager<ApplicationUser> _um;
        private readonly ICefCatalogRepository _catalogs;
        private readonly ICefIncidentService _incidents;
        private readonly ICefContainerRepository _containers;
        private readonly ICefTransactionQueries _transactions;
        private readonly AppDbContext _db;

        public ProvisionController(
            IProvisionService svc,
            IProvisionReadService read,
            UserManager<ApplicationUser> um,
            ICefCatalogRepository catalogs,
            ICefIncidentService incidents,
            ICefContainerRepository containers,
            ICefTransactionQueries transactions,
            AppDbContext context
            
        ): base(context, um)
        { 
            _svc = svc; 
            _read = read; 
            _um = um;
            _catalogs = catalogs;
            _incidents = incidents;
            _containers = containers;
            _transactions = transactions;
            _db = context;
        }

        /// <summary>
        /// Sucursales, estados y permisos por vista.
        /// </summary>
        private async Task SetCommonViewBagsCefAsync(ApplicationUser currentUser, string pageName, params string[] codVistas)
        {
            await base.SetCommonViewBagsBaseAsync(currentUser, pageName);
            bool isAdmin = ViewBag.IsAdmin;

            var (sucursales, estados) = await _transactions.GetDropdownListsAsync(currentUser.Id, isAdmin);
            ViewBag.AvailableBranches = sucursales;
            ViewBag.TransactionStatuses = estados;

            var vistas = (codVistas != null && codVistas.Length > 0)
                ? codVistas
                : new[] { "CEF_SUP", "CEF_REC", "CEF_COL", "CEF_DEL" };

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
        }

        [HttpGet("Process/{txId:int}")]
        [RequiredPermission(PermissionType.Edit, "CEF_SUP")]
        public async Task<IActionResult> Process(int txId)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsCefAsync(user, "CEF - Provisión", "CEF_SUP", "CEF_COL", "CEF_REC", "CEF_DEL");

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
            ViewBag.PointCapsJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                pointCaps.sobres,
                pointCaps.cheques,
                pointCaps.documentos
            }, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });

            var selected = vm.Transaction.Currency?.Trim();
            ViewBag.CurrencyOptions = Enum.GetValues(typeof(CurrencyEnum))
                .Cast<CurrencyEnum>()
                .Select(c => new SelectListItem
                {
                    Value = c.ToString(),
                    Text = c.ToString(),
                    Selected = string.Equals(selected, c.ToString(), StringComparison.OrdinalIgnoreCase)
                })
                .ToList();

            ViewData["Title"] = "Provisión - Procesamiento de Bolsas";
            return View("~/Views/Provision/Process.cshtml", vm);
        }


        [HttpPost("Process/{txId:int}")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "CEF_SUP")]
        public async Task<IActionResult> Process([FromRoute] int txId, [FromForm] SaveProvisionContainersCmd cmd)
        {
            var user = await _um.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var page = await _read.GetProcessPageAsync(txId);
            if (page is null) return NotFound();

            var (capEfectivo, capDocs, capCheques) = await _containers.GetPointCapsAsync(page.Service.ServiceOrderId);

            foreach (var c in cmd.Containers ?? Enumerable.Empty<CefContainerProcessingViewModel>())
            {
                c.ValueDetails = (c.ValueDetails ?? new List<CefValueDetailViewModel>())
                    .Where(v =>
                    {
                        switch (v.ValueType)
                        {
                            case CefValueTypeEnum.Billete:
                            case CefValueTypeEnum.Moneda:
                                return v.DenominationId.HasValue && (v.Quantity ?? 0) > 0;
                            case CefValueTypeEnum.Documento:
                                return (v.UnitValue ?? 0) > 0;
                            case CefValueTypeEnum.Cheque:
                                return !string.IsNullOrWhiteSpace(v.EntitieBankId) && (v.UnitValue ?? 0) > 0;
                            default:
                                return false;
                        }
                    })
                    .ToList();
            }

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(cmd.InformativeIncident))
                errors.Add("La divisa es obligatoria.");

            if (!(cmd.SlipNumber.HasValue && cmd.SlipNumber.Value > 0))
                errors.Add("El número de planilla debe ser mayor que 0.");

            foreach (var c in cmd.Containers ?? Enumerable.Empty<CefContainerProcessingViewModel>())
            {
                foreach (var v in c.ValueDetails ?? Enumerable.Empty<CefValueDetailViewModel>())
                {
                    switch (v.ValueType)
                    {
                        case CefValueTypeEnum.Billete:
                        case CefValueTypeEnum.Moneda:
                            if (c.ContainerType == CefContainerTypeEnum.Sobre && !capEfectivo)
                                errors.Add("El punto no admite sobres de efectivo.");
                            break;

                        case CefValueTypeEnum.Documento:
                            if (!capDocs) errors.Add("El punto no admite sobres de documentos.");
                            break;

                        case CefValueTypeEnum.Cheque:
                            if (!capCheques) errors.Add("El punto no admite sobres de cheques.");
                            break;
                    }
                }
            }

            if (errors.Any())
                return Json(ServiceResult.FailureResult("Validación falló.", new Dictionary<string, string[]>
                {
                    ["Cabecera"] = errors.ToArray()
                }));

            await _svc.SaveHeaderAsync(txId, cmd.SlipNumber!.Value, cmd.InformativeIncident ?? string.Empty, user.Id);
            await _svc.SaveContainersAsync(txId, cmd, user.Id);

            return Json(ServiceResult.SuccessResult("Guardado."));
        }

        [HttpPost("Finalize/{txId:int}")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "CEF_SUP")]
        public async Task<IActionResult> Finalize(int txId)
        {
            var user = await _um.GetUserAsync(User);
            if (user is null) return Unauthorized();

            await _svc.FinalizeAsync(txId, user.Id);
            return RedirectToAction("Supplies", "Cef");
        }

        [HttpGet("Deliver/{txId:int}")]
        [RequiredPermission(PermissionType.Edit, "CEF_SUP")]
        public async Task<IActionResult> Deliver([FromRoute] int txId, [FromQuery] string? returnUrl = null)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser is null) return RedirectToPage("/Account/Login", new { area = "Identity"});

            await SetCommonViewBagsBaseAsync(currentUser, "CEF - Provision - Entrega");

            var vm = await _read.GetDeliveryAsync(txId, returnUrl);
            if (vm is null)
            {
                TempData["ErrorMessage"] = "La transacción no está lista para entrega o no existe.";
                return RedirectToAction("Supplies", "Cef");
            }

            return View("~/Views/Provision/Deliver.cshtml", vm);
        }

        [HttpPost("Deliver/{txId:int}")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "CEF_SUP")]
        public async Task<IActionResult> Deliver([FromRoute] int txId, CefProvisionDeliveryViewModel model)
        {
            var user = await _um.GetUserAsync(User);
            if (user is null) return Unauthorized();

            if (txId != model.TransactionId)
                ModelState.AddModelError("", "Identificador inválido de transacción.");

            if (!ModelState.IsValid)
            {
                var refreshed = await _read.GetDeliveryAsync(txId, model.ReturnUrl);
                if (refreshed != null)
                {
                    model.JtUsers = refreshed.JtUsers;
                    model.ServiceOrderId = refreshed.ServiceOrderId;
                    model.SlipNumber = refreshed.SlipNumber;
                    model.Currency = refreshed.Currency;
                    model.BranchName = refreshed.BranchName;
                    model.TotalCountedValue = refreshed.TotalCountedValue;
                    model.CurrentStatus = refreshed.CurrentStatus;
                }
                return View("~/Views/Provision/Deliver.cshtml", model);
            }

            await _svc.DeliverAsync(txId, user.Id, model.ReceiverUserId!);

            TempData["SuccessMessage"] = "Entrega registrada correctamente.";
            var back = string.IsNullOrWhiteSpace(model.ReturnUrl) ? Url.Action("Delivery", "Cef")! : model.ReturnUrl!;
            return Redirect(back);
        }
    }
}