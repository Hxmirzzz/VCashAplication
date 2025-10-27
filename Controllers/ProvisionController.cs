using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Filters;
using VCashApp.Models;
using VCashApp.Models.ViewModels.CentroEfectivo.Shared;
using VCashApp.Models.ViewModels.CentroEfectivo.Provision;
using VCashApp.Services;
using VCashApp.Services.CentroEfectivo.Provision.Application;
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
        private readonly ICefContainerService _cefContainerService;

        public ProvisionController(
            AppDbContext context,
            UserManager<ApplicationUser> um,
            IProvisionService svc, 
            IProvisionReadService read,
            ICefContainerService cefContainerService,
            ICefIncidentService cefIncidentService
        ): base(context, um)
        { 
            _svc = svc; 
            _read = read; 
            _um = um;
            _cefContainerService = cefContainerService;
        }

        [HttpGet("Process/{txId:int}")]
        [RequiredPermission(PermissionType.Edit, "CEF_SUP")]
        public async Task<IActionResult> Process(int txId)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsBaseAsync(currentUser, "CEF - Provisión");

            var roleNames = await _userManager.GetRolesAsync(currentUser);
            ViewBag.HasEdit = await HasPermisionForView(roleNames, "CEF_SUP", PermissionType.Edit);

            var caps = await GetCefCapsAsync(currentUser);
            ViewBag.CanCountBills = caps.CanCountBills;
            ViewBag.CanCountCoins = caps.CanCountCoins;
            ViewBag.CanIncCreateEdit = caps.CanIncCreateEdit;
            ViewBag.CanIncApprove = caps.CanIncApprove;
            ViewBag.CanFinalize = caps.CanFinalize;

            var vm = await _read.GetProcessPageAsync(txId);
            if (vm is null) return NotFound();

            ViewBag.DenomsJson = await _cefContainerService.BuildDenomsJsonForTransactionAsync(txId);
            ViewBag.QualitiesJson = await _cefContainerService.BuildQualitiesJsonAsync();
            ViewBag.BanksJson = await _cefContainerService.BuildBankEntitiesJsonAsync();

            var pointCaps = await _cefContainerService.GetPointCapsAsync(vm.Service.ServiceOrderId);
            ViewBag.PointCapsJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                sobres = pointCaps.sobres,
                cheques = pointCaps.cheques,
                documentos = pointCaps.documentos
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

            return View("~/Views/Provision/Process.cshtml", vm);
        }


        [HttpPost("Process/{txId:int}")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "CEF_SUP")]
        public async Task<IActionResult> Process([FromRoute] int txId, [FromForm] SaveProvisionContainersCmd cmd)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser is null) return Unauthorized();

            var page = await _read.GetProcessPageAsync(txId);
            if (page is null) return NotFound();
            var (capEfectivo, capDocs, capCheques) = await _cefContainerService.GetPointCapsAsync(page.Service.ServiceOrderId);

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

            await _svc.SaveHeaderAsync(txId, cmd.SlipNumber!.Value, cmd.InformativeIncident!, currentUser.Id);
            await _svc.SaveContainersAsync(txId, cmd, currentUser.Id);
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