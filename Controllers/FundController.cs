using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using VCashApp.Data;
using VCashApp.Filters;
using VCashApp.Models;
using VCashApp.Models.Dtos.Fund;
using VCashApp.Services.DTOs;
using VCashApp.Services.Fund.Application;

namespace VCashApp.Controllers
{
    [Authorize]
    [Route("/Fund")]
    public sealed class FundController : BaseController
    {
        private readonly IFundService _svc;
        private readonly IFundQueries _queries;
        private readonly UserManager<ApplicationUser> _um;
        private const string CodVista = "FUNDS";

        public FundController(
            IFundService svc,
            IFundQueries queries,
            AppDbContext context,
            UserManager<ApplicationUser> um
        ) : base(context, um)
        {
            _svc = svc;
            _queries = queries;
            _um = um;
        }

        /// <summary>
        /// Carga ViewBags comunes (cabecera, combos, y permisos).
        /// </summary>
        private async Task SetCommonViewBagsAsync(ApplicationUser currentUser, string pageName)
        {
            await base.SetCommonViewBagsBaseAsync(currentUser, pageName);

            var lookups = await _queries.GetLookupsAsync();
            ViewBag.Clients = lookups.Clients;
            ViewBag.Branches = lookups.Branches;
            ViewBag.Cities = lookups.Cities;
            ViewBag.Currencies = lookups.Currencies;
            ViewBag.FundTypes = lookups.FundTypes;

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            ViewBag.HasCreate = await HasPermisionForView(userRoles, CodVista, PermissionType.Create);
            ViewBag.HasEdit = await HasPermisionForView(userRoles, CodVista, PermissionType.Edit);
            ViewBag.HasView = await HasPermisionForView(userRoles, CodVista, PermissionType.View);
        }

        // =========================================================
        // INDEX (LISTADO + FILTROS + PAGINACIÓN)
        // =========================================================
        [HttpGet("Index")]
        [RequiredPermission(PermissionType.View, CodVista)]
        public async Task<IActionResult> Index([FromQuery] FundFilterDto filter)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            if (filter.Page <= 0) filter.Page = 1;
            if (filter.PageSize <= 0) filter.PageSize = 15;

            await SetCommonViewBagsAsync(user, "Fondos");

            var (items, total, currentPage, pageSize) = await _queries.GetPagedAsync(filter);

            ViewBag.Filter = filter;
            ViewBag.TotalData = total;
            ViewBag.CurrentPage = currentPage;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("~/Views/Fund/_FundTablePartial.cshtml", items);

            return View("~/Views/Fund/Index.cshtml", items);
        }

        // =========================================================
        // CREATE (GET)
        // =========================================================
        [HttpGet("Create")]
        [RequiredPermission(PermissionType.Create, CodVista)]
        public async Task<IActionResult> Create()
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsAsync(user, "Crear Fondo");

            var dto = new FundUpsertDto { FundStatus = true };
            return View("~/Views/Fund/Create.cshtml", dto);
        }

        // =========================================================
        // EDIT (GET)
        // =========================================================
        [HttpGet("Edit/{code}")]
        [RequiredPermission(PermissionType.Edit, CodVista)]
        public async Task<IActionResult> Edit(string code)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsAsync(user, "Editar Fondo");

            var dto = await _queries.GetForEditAsync(code);
            if (dto is null) return NotFound();

            return View("~/Views/Fund/Edit.cshtml", dto);
        }

        // =========================================================
        // SAVE (CREATE/UPDATE) — mismo patrón que RangeController
        // =========================================================
        [HttpPost("Save")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, CodVista)]
        public async Task<IActionResult> Save([FromForm] FundUpsertDto dto, [FromForm] bool IsEdit)
        {
            var user = await _um.GetUserAsync(User);
            if (user is null) return Unauthorized();

            try
            {
                ServiceResult res = IsEdit
                    ? await _svc.UpdateAsync(dto, user.Id)
                    : await _svc.CreateAsync(dto, user.Id);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(res);

                if (res.Success)
                {
                    TempData["Success"] = res.Message;
                    return RedirectToAction(nameof(Index));
                }

                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser is not null)
                    await SetCommonViewBagsAsync(currentUser, IsEdit ? "Editar Fondo" : "Crear Fondo");

                ViewBag.Errors = res.Errors;
                return View("~/Views/Funds/Upsert.cshtml", dto);
            }
            catch (InvalidOperationException ex)
            {
                var err = ServiceResult.FailureResult(ex.Message);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(err);

                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                var err = ServiceResult.FailureResult("Ocurrió un error inesperado al guardar el fondo.");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(err);

                TempData["Error"] = err.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // =========================================================
        // PREVIEW (GET)
        // =========================================================
        [HttpGet("Preview/{code}")]
        [RequiredPermission(PermissionType.View, CodVista)]
        public async Task<IActionResult> Preview(string code)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsAsync(user, "Detalle de Fondo");

            var dto = await _queries.GetForPreviewAsync(code);
            if (dto is null) return NotFound();

            return View("~/Views/Funds/Preview.cshtml", dto);
        }

        // =========================================================
        // TOGGLE STATUS (ACTIVAR/INACTIVAR)
        // =========================================================
        [HttpPost("ToggleStatus/{code}")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, CodVista)]
        public async Task<IActionResult> ToggleStatus(string code, [FromForm] bool isActive)
        {
            var user = await _um.GetUserAsync(User);
            if (user is null) return Unauthorized();

            try
            {
                var result = await _svc.ChangeStatusAsync(code, isActive, user.Id);
                return Json(result);
            }
            catch (InvalidOperationException ex)
            {
                return Json(ServiceResult.FailureResult(ex.Message));
            }
            catch
            {
                return Json(ServiceResult.FailureResult("No fue posible actualizar el estado."));
            }
        }

        // =========================================================
        // EXPORT (CSV básico desde Queries)
        // =========================================================
        [HttpGet("Export")]
        [RequiredPermission(PermissionType.View, CodVista)]
        public async Task<IActionResult> Export([FromQuery] FundFilterDto filter, string format = "csv")
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var rows = await _queries.ExportAsync(filter);

            format = (format ?? "csv").Trim().ToLowerInvariant();
            if (format != "csv")
                return BadRequest("Formato no válido. Solo se permite CSV en esta versión.");

            var sb = new StringBuilder();
            sb.AppendLine("FundCode,VatcoFundCode,FundName,Client,Branch,City,Furrency,FundType,CreationDate,WithdrawalDate,Status");
            foreach (var r in rows)
            {
                var creation = r.CreationDate?.ToString("yyyy-MM-dd") ?? "";
                var withdrawal = r.WithdrawalDate?.ToString("yyyy-MM-dd") ?? "";
                var status = r.FundStatus ? "Activo" : "Inactivo";

                sb.AppendLine($"{r.FundCode},{r.VatcoFundCode},{EscapeCsv(r.FundName)},{EscapeCsv(r.ClientName)},{EscapeCsv(r.BranchName)},{EscapeCsv(r.CityName)},{EscapeCsv(r.FundCurrency)},{r.FundType},{creation},{withdrawal},{status}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", "funds_export.csv");
        }

        // =========================================================
        // Helpers
        // =========================================================
        private static string EscapeCsv(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var needsQuotes = s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
            if (!needsQuotes) return s;
            return $"\"{s.Replace("\"", "\"\"")}\"";
        }
    }
}
