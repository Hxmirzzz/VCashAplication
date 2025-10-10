using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VCashApp.Models;
using VCashApp.Models.ViewModels;
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

        public ProvisionController(IProvisionService svc, IProvisionReadService read, UserManager<ApplicationUser> um)
        { _svc = svc; _read = read; _um = um; }

        [HttpGet("Process/{txId:int}")]
        public async Task<IActionResult> Process(int txId)
        {
            var user = await _um.GetUserAsync(User);
            if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var vm = await _read.GetProcessPageAsync(txId);
            if (vm is null) return NotFound();

            ViewBag.Mode = "Provision";
            ViewBag.CanCountBills = true;
            ViewBag.CanCountCoins = true;
            ViewBag.CanIncCreateEdit = false;
            ViewBag.CanIncApprove = false;
            ViewBag.CanFinalize = true;

            return View("~/Views/Provision/Process.cshtml", vm);
        }


        [HttpPost("Process/{txId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(int txId, SaveProvisionContainersCmd cmd)
        {
            var user = await _um.GetUserAsync(User);
            if (user is null) return Unauthorized();

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