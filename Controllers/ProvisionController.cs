using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.IdentityModel.Tokens;
using VCashApp.Enums;
using VCashApp.Models;
using VCashApp.Models.ViewModels.CentroEfectivo;
using VCashApp.Services;
using VCashApp.Services.CentroEfectivo.Provision.Application;
using VCashApp.Services.DTOs;

namespace VCashApp.Controllers
{
    [Authorize]
    [Route("Provision")]
    public sealed class ProvisionController : Controller
    {
        private readonly IProvisionService _svc;
        private readonly IProvisionReadService _read;
        private readonly UserManager<ApplicationUser> _um;
        private readonly ICefContainerService _cefContainerService;

        public ProvisionController(IProvisionService svc, 
            IProvisionReadService read,
            UserManager<ApplicationUser> um,
            ICefContainerService cefContainerService,
            ICefIncidentService cefIncidentService)
        { 
            _svc = svc; 
            _read = read; 
            _um = um;
            _cefContainerService = cefContainerService;
        }

        [HttpGet("Process/{txId:int}")]
        public async Task<IActionResult> Process(int txId)
        {
            var user = await _um.GetUserAsync(User);
            if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var vm = await _read.GetProcessPageAsync(txId);
            if (vm is null) return NotFound();

            ViewBag.Mode = "Provision";
            ViewBag.HasEdit = true;
            ViewBag.CanCountBills = true;
            ViewBag.CanCountCoins = true;
            ViewBag.CanIncCreateEdit = false;
            ViewBag.CanIncApprove = false;
            ViewBag.CanFinalize = true;

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
        public async Task<IActionResult> Process([FromRoute] int txId, [FromForm] SaveProvisionContainersCmd cmd)
        {
            var user = await _um.GetUserAsync(User);
            if (user is null) return Unauthorized();

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

            if (string.IsNullOrWhiteSpace(cmd.Currency))
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

            await _svc.SaveHeaderAsync(txId, cmd.SlipNumber!.Value, cmd.Currency!, user.Id);
            await _svc.SaveContainersAsync(txId, cmd, user.Id);
            return Json(ServiceResult.SuccessResult("Guardado."));
        }

        [HttpPost("Finalize/{txId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Finalize(int txId)
        {
            var user = await _um.GetUserAsync(User);
            if (user is null) return Unauthorized();

            await _svc.FinalizeAsync(txId, user.Id);
            return Json(ServiceResult.SuccessResult("Provisión lista para entrega."));
        }

        [HttpPost("Deliver/{txId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deliver(int txId)
        {
            var user = await _um.GetUserAsync(User);
            if (user is null) return Unauthorized();

            await _svc.DeliverAsync(txId, user.Id);
            return Json(ServiceResult.SuccessResult("Entregado."));
        }
    }
}