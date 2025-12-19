using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using VCashApp.Data;
using VCashApp.Filters;
using VCashApp.Models;
using VCashApp.Models.Dtos.Point;
using VCashApp.Services.DTOs;
using VCashApp.Services.Point.Application;
using VCashApp.Services.Point.Infrastructure;

namespace VCashApp.Controllers
{
    [Authorize]
    [Route("/Point")]
    public sealed class PointController : BaseController
    {
        private readonly IPointService _svc;
        private readonly IPointQueries _queries;
        private readonly UserManager<ApplicationUser> _um;

        private const string CodVista = "POINTS";

        public PointController(
            IPointService svc,
            IPointQueries queries,
            AppDbContext context,
            UserManager<ApplicationUser> um
        ) : base(context, um)
        {
            _svc = svc;
            _queries = queries;
            _um = um;
        }

        // =========================================================
        // ViewBags comunes
        // =========================================================
        private async Task SetCommonViewBagsAsync(ApplicationUser user, string pageName)
        {
            await base.SetCommonViewBagsBaseAsync(user, pageName);

            var lookups = await _queries.GetLookupsAsync();

            ViewBag.Clientes = lookups.Clientes;
            ViewBag.Sucursales = lookups.Sucursales;
            ViewBag.Ciudades = lookups.Ciudades;
            ViewBag.Fondos = lookups.Fondos;
            ViewBag.Rutas = lookups.Rutas;
            ViewBag.Rangos = lookups.Rangos;
            ViewBag.TiposNeg = lookups.TiposNegocio;

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.HasCreate = await HasPermisionForView(roles, CodVista, PermissionType.Create);
            ViewBag.HasEdit = await HasPermisionForView(roles, CodVista, PermissionType.Edit);
            ViewBag.HasView = await HasPermisionForView(roles, CodVista, PermissionType.View);
        }

        // =========================================================
        // INDEX
        // =========================================================
        [HttpGet("Index")]
        [RequiredPermission(PermissionType.View, CodVista)]
        public async Task<IActionResult> Index([FromQuery] PointFilterDto filter)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            if (filter.Page <= 0) filter.Page = 1;
            if (filter.PageSize <= 0) filter.PageSize = 15;

            await SetCommonViewBagsAsync(user, "Puntos");

            var (items, total) = await _queries.GetPagedAsync(filter);

            ViewBag.Filter = filter;
            ViewBag.TotalData = total;
            ViewBag.CurrentPage = filter.Page;
            ViewBag.PageSize = filter.PageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)filter.PageSize);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("~/Views/Point/_PointTablePartial.cshtml", items);

            return View("~/Views/Point/Index.cshtml", items);
        }

        // =========================================================
        // CREATE
        // =========================================================
        [HttpGet("Create")]
        [RequiredPermission(PermissionType.Create, CodVista)]
        public async Task<IActionResult> Create()
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsAsync(user, "Crear Punto");

            return View("~/Views/Point/Create.cshtml",
                new PointUpsertDto { FecIngreso = DateOnly.FromDateTime(DateTime.Today) });
        }

        // =========================================================
        // EDIT
        // =========================================================
        [HttpGet("Edit/{code}")]
        [RequiredPermission(PermissionType.Edit, CodVista)]
        public async Task<IActionResult> Edit(string code)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var dto = await _queries.GetForEditAsync(code);
            if (dto is null) return NotFound();

            await SetCommonViewBagsAsync(user, "Editar Punto");

            return View("~/Views/Point/Edit.cshtml", dto);
        }

        // =========================================================
        // SAVE (Create/Update)
        // =========================================================
        [HttpPost("Save")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, CodVista)]
        public async Task<IActionResult> Save(
            [FromForm] PointUpsertDto dto,
            [FromForm] bool IsEdit,
            IFormFile? cartaFile,
            bool removeCartaActual = false)
        {
            var user = await _um.GetUserAsync(User);
            if (user is null) return Unauthorized();

            try
            {
                ServiceResult res = IsEdit
                    ? await _svc.UpdateAsync(dto, cartaFile, removeCartaActual)
                    : await _svc.CreateAsync(dto, cartaFile);

                // AJAX
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(res);

                if (res.Success)
                {
                    TempData["Success"] = res.Message;
                    return RedirectToAction(nameof(Index));
                }

                var current = await GetCurrentApplicationUserAsync();
                if (current != null)
                    await SetCommonViewBagsAsync(current, IsEdit ? "Editar Punto" : "Crear Punto");

                ViewBag.Errors = res.Errors;
                return View("~/Views/Point/Upsert.cshtml", dto);
            }
            catch (InvalidOperationException ex)
            {
                var err = ServiceResult.FailureResult(ex.Message);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(err);

                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                var err = ServiceResult.FailureResult("Ocurrió un error inesperado al guardar el punto.");

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(err);

                TempData["Error"] = err.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // =========================================================
        // PREVIEW
        // =========================================================
        [HttpGet("Preview/{code}")]
        [RequiredPermission(PermissionType.View, CodVista)]
        public async Task<IActionResult> Preview(string code)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsAsync(user, "Detalle Punto");

            var dto = await _queries.GetPreviewAsync(code);
            if (dto is null) return NotFound();

            return View("~/Views/Point/Preview.cshtml", dto);
        }

        // =========================================================
        // TOGGLE STATUS
        // =========================================================
        [HttpPost("ToggleStatus/{code}")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, CodVista)]
        public async Task<IActionResult> ToggleStatus(string code)
        {
            var user = await _um.GetUserAsync(User);
            if (user is null) return Unauthorized();

            try
            {
                var result = await _svc.ToggleStatusAsync(code);
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
        // EXPORT
        // =========================================================
        [HttpGet("Export")]
        [RequiredPermission(PermissionType.View, CodVista)]
        public async Task<IActionResult> Export([FromQuery] PointFilterDto filter)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var rows = await _queries.ExportAsync(filter);

            var sb = new StringBuilder();
            sb.AppendLine("CodPunto,CodPCliente,Cliente,NombrePunto,Sucursal,Ciudad,Estado");

            foreach (var r in rows)
            {
                var est = r.EstadoPunto ? "Activo" : "Inactivo";
                sb.AppendLine($"{r.CodPunto},{r.CodPCliente},{EscapeCsv(r.ClienteNombre)},{EscapeCsv(r.NombrePunto)},{EscapeCsv(r.NombreSucursal)},{EscapeCsv(r.NombreCiudad)},{est}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", "points_export.csv");
        }

        // =========================================================
        // GENERATE VATCO CODE (AJAX)
        // =========================================================
        [HttpPost("GetNewVatcoCode")]
        public async Task<IActionResult> GetNewVatcoCode(
            [FromForm] int codCliente,
            [FromForm] int tipoPunto)
        {
            if (codCliente <= 0)
                return BadRequest("Cliente inválido.");

            if (tipoPunto is not (0 or 1))
                return BadRequest("Tipo de punto inválido.");

            var code = await _svc.GenerateVatcoCodeAsync(codCliente, tipoPunto);
            return Content(code);
        }

        [HttpGet("GetMainClientOptions")]
        public async Task<IActionResult> GetMainClientOptions(int codCliente)
        {
            var options = await _svc.GetMainClientOptionsAsync(codCliente);
            return Ok(options);
        }

        /// <summary>
        /// Obtiene el HTML de las opciones de fondos para un punto según sucursal y cliente.
        /// </summary>
        /// <param name="branchId">Sucursal</param>
        /// <param name="clientId">Cliente</param>
        /// <param name="mainClientId">Cliente Principal</param>
        /// <returns>Lista de opciones en formato HTML</returns>
        [HttpGet("GetFunds")]
        public async Task<IActionResult> GetFunds(int branchId, int clientId, int mainClientId = 0)
        {
            var options = await _svc.GetFundsOptionsHtmlAsync(branchId, clientId, mainClientId);
            return Ok(options);
        }

        /// <summary>
        /// Obtiene el HTML de las opciones de rutas para un punto según ciudad.
        /// </summary>
        /// <param name="branchId"></param>
        /// <returns>Lista de opciones en formato HTML</returns>
        [HttpGet("GetRoutes")]
        public async Task<IActionResult> GetRoutes(int branchId)
        {
            var options = await _svc.GetRoutesOptionsHtmlAsync(branchId);
            return Ok(options);
        }

        /// <summary>
        /// Obtiene el HTML de las opciones de rangos para un punto según cliente.
        /// </summary>
        /// <param name="clientId">Cliente</param>
        /// <returns>De lista de opciones en formato HTML</returns>
        [HttpGet("GetRangesByClient")]
        public async Task<IActionResult> GetRangesByClient(int clientId)
        {
            var options = await _svc.GetRangeOptionsHtmlAsync(clientId);
            return Ok(options);
        }

        private static string EscapeCsv(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var needsQuotes = s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
            if (!needsQuotes) return s;
            return $"\"{s.Replace("\"", "\"\"")}\"";
        }
    }
}
