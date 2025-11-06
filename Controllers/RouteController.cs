using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Filters;
using VCashApp.Infrastructure.Branches;
using VCashApp.Models;
using VCashApp.Models.DTOs;
using VCashApp.Models.Entities;
using VCashApp.Services.DTOs;
using VCashApp.Services.Routes.Application;

namespace VCashApp.Controllers
{
    [Authorize]
    [Route("Route")]
    public sealed class RouteController : BaseController
    {
        private readonly IRouteService _svc;
        private readonly IRouteQueries _queries;
        private readonly IBranchContext _branchCtx;
        private readonly UserManager<ApplicationUser> _um;
        private readonly AppDbContext _db;

        public RouteController(
            IRouteService svc,
            IRouteQueries queries,
            IBranchContext branchCtx,
            AppDbContext context,
            UserManager<ApplicationUser> um
        ) : base(context, um)
        {
            _svc = svc;
            _queries = queries;
            _branchCtx = branchCtx;
            _db = context;
            _um = um;
        }

        /// <summary>
        /// Sucursales (filtradas por permisos), catálogos y banderas de permiso por vista.
        /// </summary>
        private async Task SetCommonViewBagsRoutesAsync(ApplicationUser currentUser, string pageName, params string[] codVistas)
        {
            await base.SetCommonViewBagsBaseAsync(currentUser, pageName);
            bool isAdmin = ViewBag.IsAdmin;

            var (branches, routeTypes, serviceTypes, vehicleTypes) =
                await _queries.GetDropdownsAsync(currentUser.Id, isAdmin);

            ViewBag.AvailableBranches = branches;
            ViewBag.RouteTypeOptions = routeTypes;
            ViewBag.ServiceTypeOptions = serviceTypes;
            ViewBag.VehicleTypeOptions = vehicleTypes;

            var vistas = (codVistas != null && codVistas.Length > 0) ? codVistas : new[] { "ADM_RUTAS" };
            var userRoles = await _userManager.GetRolesAsync(currentUser);

            async Task<bool> HasAsync(PermissionType p)
            {
                foreach (var v in vistas)
                {
                    if (await HasPermisionForView(userRoles, v, p)) return true;
                }
                return false;
            }

            ViewBag.HasCreate = await HasAsync(PermissionType.Create);
            ViewBag.HasEdit = await HasAsync(PermissionType.Edit);
            ViewBag.HasView = await HasAsync(PermissionType.View);
        }

        // =========================================================
        // INDEX / LISTADO
        // =========================================================
        [HttpGet("Index")]
        [RequiredPermission(PermissionType.View, "ADM_RUTAS")]
        public async Task<IActionResult> Index([FromQuery] RouteFilterDto filter)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsRoutesAsync(user, "Administración - Rutas", "ADM_RUTAS");

            if (filter.Page <= 0) filter.Page = 1;
            if (filter.PageSize <= 0) filter.PageSize = 15;

            var (items, total, currentPage, pageSize) = await _queries.GetPagedAsync(filter);

            ViewBag.Filter = filter;
            ViewBag.TotalCount = total;
            ViewBag.CurrentPage = currentPage;
            ViewBag.PageSize = pageSize;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("~/Views/Route/_RoutesTablePartial.cshtml", items);

            return View("~/Views/Route/Index.cshtml", items);
        }

        // =========================================================
        // CREATE
        // =========================================================
        [HttpGet("Create")]
        [RequiredPermission(PermissionType.Create, "ADM_RUTAS")]
        public async Task<IActionResult> Create()
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsRoutesAsync(user, "Administración - Rutas", "ADM_RUTAS");

            var dto = new RouteUpsertDto
            {
                BranchId = _branchCtx.CurrentBranchId
            };

            return View("~/Views/Route/Create.cshtml", dto);
        }

        // =========================================================
        // EDIT
        // =========================================================
        [HttpGet("Edit/{id}")]
        [RequiredPermission(PermissionType.Edit, "ADM_RUTAS")]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsRoutesAsync(user, "Administración - Rutas", "ADM_RUTAS");

            var dto = await _queries.GetForEditAsync(id);
            if (dto is null) return NotFound();

            return View("~/Views/Route/Edit.cshtml", dto);
        }

        private static string NormalizeRouteCode(string? v)
            => (v ?? "").Trim().ToUpper().Replace(" ", "");

        private static string? BuildBranchRouteCode(int? branchId, string? routeCode)
        {
            var rc = NormalizeRouteCode(routeCode);
            if (branchId.HasValue && !string.IsNullOrWhiteSpace(rc))
                return $"{branchId.Value}_{rc}";
            return null;
        }

        // =========================================================
        // SAVE (UPSERT)
        // =========================================================
        [HttpPost("Save")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "ADM_RUTAS")]
        public async Task<IActionResult> Save([FromForm] RouteUpsertDto dto, [FromForm] bool IsEdit)
        {
            var user = await _um.GetUserAsync(User);
            if (user is null) return Unauthorized();

            dto.RouteCode = NormalizeRouteCode(dto.RouteCode);
            var builtPk = BuildBranchRouteCode(dto.BranchId, dto.RouteCode);
            if (string.IsNullOrWhiteSpace(builtPk))
            {
                return Json(ServiceResult.FailureResult("Debe seleccionar Sucursal y diligenciar Código de Ruta."));
            }
            dto.BranchRouteCode = builtPk;

            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(dto.BranchRouteCode))
                errors.Add("El código de ruta de sucursal es obligatorio.");

            if (!string.IsNullOrWhiteSpace(dto.RouteType) && !RouteCatalogs.RouteTypeCode.IsValid(dto.RouteType))
                errors.Add("Tipo de ruta inválido.");

            if (!string.IsNullOrWhiteSpace(dto.ServiceType) && !RouteCatalogs.ServiceTypeCode.IsValid(dto.ServiceType))
                errors.Add("Tipo de atención inválido.");

            if (!string.IsNullOrWhiteSpace(dto.VehicleType) && !RouteCatalogs.VehicleTypeCode.IsValid(dto.VehicleType))
                errors.Add("Tipo de vehículo inválido.");

            if (errors.Any())
                return Json(ServiceResult.FailureResult("Validación falló.", new Dictionary<string, string[]>
                {
                    ["Cabecera"] = errors.ToArray()
                }));

            var dayErrors = new List<string>();

            void Check(string day, bool enabled, TimeSpan? start, TimeSpan? end)
            {
                if (!enabled) return;

                if (start is null && end is null)
                {
                    dayErrors.Add($"{day}: debe diligenciar al menos un rango (hora inicial y final).");
                    return;
                }

                if (start is null ^ end is null)
                {
                    dayErrors.Add($"{day}: si diligencia una hora debe diligenciar la otra.");
                    return;
                }

                if (start >= end)
                {
                    dayErrors.Add($"{day}: la hora inicial debe ser menor que la final.");
                }
            }

            Check("Lunes", dto.Monday, dto.MondayStartTime, dto.MondayEndTime);
            Check("Martes", dto.Tuesday, dto.TuesdayStartTime, dto.TuesdayEndTime);
            Check("Miércoles", dto.Wednesday, dto.WednesdayStartTime, dto.WednesdayEndTime);
            Check("Jueves", dto.Thursday, dto.ThursdayStartTime, dto.ThursdayEndTime);
            Check("Viernes", dto.Friday, dto.FridayStartTime, dto.FridayEndTime);
            Check("Sábado", dto.Saturday, dto.SaturdayStartTime, dto.SaturdayEndTime);
            Check("Domingo", dto.Sunday, dto.SundayStartTime, dto.SundayEndTime);
            Check("Festivo", dto.Holiday, dto.HolidayStartTime, dto.HolidayEndTime);

            bool hasAnyDay = dto.Monday || dto.Tuesday || dto.Wednesday || dto.Thursday ||
                             dto.Friday || dto.Saturday || dto.Sunday || dto.Holiday;

            if (!hasAnyDay)
                dayErrors.Add("Debe seleccionar al menos un día habilitado.");

            if (dayErrors.Any())
            {
                return Json(ServiceResult.FailureResult(
                    "Validación de horarios inválida.",
                    new Dictionary<string, string[]> { ["Horarios"] = dayErrors.ToArray() }
                ));
            }

            try
            {
                var exists = await _db.Set<AdmRoute>()
                    .AsNoTracking()
                    .AnyAsync(r => r.BranchRouteCode == dto.BranchRouteCode);

                if (!IsEdit && exists)
                    return Json(ServiceResult.FailureResult("Ya existe una ruta con ese código. No se puede crear duplicado."));

                if (IsEdit && !exists)
                    return Json(ServiceResult.FailureResult("Ruta no encontrada para actualizar."));

                if (IsEdit)
                {
                    if (!exists)
                        return Json(ServiceResult.FailureResult("No se encontró la ruta para editar."));
                    var result = await _svc.UpdateAsync(dto, user.Id);
                    return result.ok
                        ? Json(ServiceResult.SuccessResult("Ruta actualizada correctamente."))
                        : Json(ServiceResult.FailureResult(result.message));
                }
                else
                {
                    if (exists)
                        return Json(ServiceResult.FailureResult("Ya existe una ruta con ese código."));
                    var result = await _svc.CreateAsync(dto, user.Id);
                    return result.ok
                        ? Json(ServiceResult.SuccessResult("Ruta registrada correctamente."))
                        : Json(ServiceResult.FailureResult(result.message));
                }
            }
            catch (InvalidOperationException ex)
            {
                return Json(ServiceResult.FailureResult(ex.Message));
            }
            catch
            {
                return Json(ServiceResult.FailureResult("Ocurrió un error inesperado al guardar la ruta."));
            }
        }

        // =========================================================
        // TOGGLE STATUS (ACTIVAR/INACTIVAR)
        // =========================================================
        [HttpPost("ToggleStatus/{id}")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "ADM_RUTAS")]
        public async Task<IActionResult> ToggleStatus(string id, [FromForm] bool isActive)
        {
            var user = await _um.GetUserAsync(User);
            if (user is null) return Unauthorized();

            try
            {
                var (ok, message) = await _svc.SetStatusAsync(id, isActive, user.Id);
                if (!ok) return Json(ServiceResult.FailureResult(message));

                return Json(ServiceResult.SuccessResult("Estado actualizado."));
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
        // EXPORT (básico)
        // =========================================================
        [HttpGet("Export")]
        [RequiredPermission(PermissionType.View, "ADM_RUTAS")]
        public async Task<IActionResult> Export([FromQuery] RouteFilterDto filter)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return Unauthorized();

            var rows = await _queries.ExportAsync(filter, "csv");

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("CodigoRuta,Ruta,Sucursal,TipoRuta,TipoAtencion,Vehiculo,Monto,Estado");
            foreach (var r in rows)
            {
                var line = string.Join(",",
                    EscapeCsv(r.RouteCode),
                    EscapeCsv(r.RouteName),
                    EscapeCsv(r.Branch),
                    EscapeCsv(r.RouteType),
                    EscapeCsv(r.ServiceType),
                    EscapeCsv(r.VehicleType),
                    r.Amount?.ToString() ?? "",
                    EscapeCsv(r.Status)
                );
                sb.AppendLine(line);
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"rutas_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(bytes, "text/csv", fileName);
        }

        private static string EscapeCsv(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
                return $"\"{s.Replace("\"", "\"\"")}\"";
            return s;
        }
    }
}