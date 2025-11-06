using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VCashApp.Data;
using VCashApp.Filters;
using VCashApp.Models;
using VCashApp.Models.DTOs.Range;
using VCashApp.Models.ViewModels.Range;
using VCashApp.Services.DTOs;
using VCashApp.Services.Range.Application;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        private readonly IRangeService _svc;
        private readonly IRangeQueries _queries;
        private readonly UserManager<ApplicationUser> _um;
        private const string CodVista = "RANGE";

        public RangeController(
            IRangeService svc,
            IRangeQueries queries,
            AppDbContext context,
            UserManager<ApplicationUser> um
        ) : base(context, um)
        {
            _svc = svc;
            _queries = queries;
            _um = um;
        }

        /// <summary>
        /// Setea los ViewBags comunes para la UI de rangos (drop-downs, permisos, datos de cabecera).
        /// </summary>
        private async Task SetCommonViewBagsAsync(ApplicationUser currentUser, string pageName)
        {
            await base.SetCommonViewBagsBaseAsync(currentUser, pageName);

            ViewBag.AvailableClients = await _queries.GetClientsForDropdownAsync();

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            ViewBag.HasCreate = await HasPermisionForView(userRoles, "RANGE", PermissionType.Create);
            ViewBag.HasEdit = await HasPermisionForView(userRoles, "RANGE", PermissionType.Edit);
            ViewBag.HasView = await HasPermisionForView(userRoles, "RANGE", PermissionType.View);
        }

        /// <summary>
        /// Muestra el dashboard principal de Rangos, permitiendo filtrar y ver los registros.
        /// </summary>
        [HttpGet("Index")]
        [RequiredPermission(PermissionType.View, "RANGE")]
        public async Task<IActionResult> Index([FromQuery] RangeFilterDto filter)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            if (filter.Page <= 0) filter.Page = 1;
            if (filter.PageSize <= 0) filter.PageSize = 15;

            await SetCommonViewBagsAsync(user, "Rangos de Atención");

            var (items, total, currentPage, pageSize) = await _queries.GetPagedAsync(filter);

            ViewBag.Filter = filter;
            ViewBag.TotalData = total;
            ViewBag.CurrentPage = currentPage;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("~/Views/Range/_RangeTablePartial.cshtml", items);

            return View("~/Views/Range/Index.cshtml", items);
        }

        /// <summary>
        /// Muestra el formulario de creación.
        /// </summary>
        [HttpGet("Create")]
        [RequiredPermission(PermissionType.Create, "RANGE")]
        public async Task<IActionResult> Create()
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsAsync(user, "Crear Rango");

            var dto = new RangeUpsertDto { RangeStatus = true };
            return View("~/Views/Range/Create.cshtml", dto);
        }

        /// <summary>
        /// Vista de edición.
        /// </summary>
        [HttpGet("Edit/{id:int}")]
        [RequiredPermission(PermissionType.Edit, "RANGE")]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsAsync(user, "Editar Rango");

            var dto = await _queries.GetForEditAsync(id);
            if (dto is null) return NotFound();

            return View("~/Views/Range/Edit.cshtml", dto);
        }

        /// <summary>
        /// Procesa la edición (validación de unicidad en DB).
        /// </summary>
        [HttpPost("Save")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "RANGE")]
        public async Task<IActionResult> Save([FromForm] RangeUpsertDto dto, [FromForm] bool IsEdit)
        {
            var user = await _um.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var fieldErrors = new Dictionary<string, string[]>();
            var errors = new List<string>();

            // 1) Al menos un día habilitado
            if (!(dto.Monday || dto.Tuesday || dto.Wednesday || dto.Thursday ||
                  dto.Friday || dto.Saturday || dto.Sunday || dto.Holiday))
            {
                errors.Add("Debe seleccionar al menos un día habilitado.");
            }

            // 2) Pares incompletos + orden en cada día habilitado
            errors.AddRange(ValidateAllDayRows(dto));

            // 3) Al menos un rango completo por día habilitado
            errors.AddRange(ValidateAtLeastOneRangePerEnabledDay(dto));

            if (errors.Any())
            {
                fieldErrors["Horarios"] = errors.ToArray();
                return Json(ServiceResult.FailureResult("Validación de horarios inválida.", fieldErrors));
            }

            // 4) Unicidad en DB
            var unique = await _queries.ValidateUniqueAsync(dto);
            if (!unique.Success)
                return Json(ServiceResult.FailureResult(unique.Message, unique.Code));

            try
            {
                ServiceResult res = IsEdit
                    ? await _svc.UpdateAsync(dto)
                    : await _svc.CreateAsync(dto);

                return res.Success
                    ? Json(ServiceResult.SuccessResult(res.Message, dto.Id))
                    : Json(ServiceResult.FailureResult(res.Message, res.Code));
            }
            catch (InvalidOperationException ex)
            {
                return Json(ServiceResult.FailureResult(ex.Message));
            }
            catch
            {
                return Json(ServiceResult.FailureResult("Ocurrió un error inesperado al guardar el rango."));
            }
        }

        /// <summary>
        /// Detalle del rango.
        /// </summary>
        [HttpGet("Details/{id:int}")]
        [RequiredPermission(PermissionType.View, "RANGE")]
        public async Task<IActionResult> Details(int id)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsAsync(user, "Detalle de Rango");

            var entityOrDto = await _queries.GetByIdAsync(id);
            if (entityOrDto is null) return NotFound();

            return View("~/Views/Range/Details.cshtml", entityOrDto);
        }

        // =========================================================
        // TOGGLE STATUS (ACTIVAR/INACTIVAR)
        // =========================================================
        [HttpPost("ToggleStatus/{id:int}")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "RANGE")]
        public async Task<IActionResult> ToggleStatus(int id, [FromForm] bool isActive)
        {
            var user = await _um.GetUserAsync(User);
            if (user is null) return Unauthorized();

            try
            {
                var result = await _svc.SetStatusAsync(id, isActive);
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

        // =======================
        //  Endpoints utilitarios
        // =======================

        /// <summary>
        /// Valida en caliente (AJAX) si la combinación de cliente + horarios ya existe (usa la misma lógica del servicio).
        /// Útil en la UI para avisar antes de postear el form.
        /// </summary>
        [HttpPost("ValidateUnique")]
        [RequiredPermission(PermissionType.View, "RANGE")]
        public async Task<IActionResult> ValidateUnique([FromBody] RangeUpsertDto dto)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user is null) return Json(new { success = false, message = "No autorizado.", code = "unauthorized" });

            if (dto is null || dto.ClientId <= 0)
                return Json(new { success = false, message = "Solicitud inválida.", code = "bad_request" });

            var res = await _queries.ValidateUniqueAsync(dto);
            return Json(new { success = res.Success, message = res.Message, code = res.Code });
        }

        // ===== Helpers de validación =====
        private static bool IsPairIncomplete(TimeSpan? hi, TimeSpan? hf)
            => hi.HasValue ^ hf.HasValue;

        private static bool IsPairCompleteValid(TimeSpan? hi, TimeSpan? hf)
            => hi.HasValue && hf.HasValue && hi.Value < hf.Value;

        private static IEnumerable<string> ValidateIncompletePairs(string day,
            (TimeSpan? hi, TimeSpan? hf) r1,
            (TimeSpan? hi, TimeSpan? hf) r2,
            (TimeSpan? hi, TimeSpan? hf) r3)
        {
            var errs = new List<string>();
            if (IsPairIncomplete(r1.hi, r1.hf)) errs.Add($"{day}, Rango 1: si diligencia una hora debe diligenciar la otra.");
            if (IsPairIncomplete(r2.hi, r2.hf)) errs.Add($"{day}, Rango 2: si diligencia una hora debe diligenciar la otra.");
            if (IsPairIncomplete(r3.hi, r3.hf)) errs.Add($"{day}, Rango 3: si diligencia una hora debe diligenciar la otra.");
            if (r1.hi.HasValue && r1.hf.HasValue && r1.hi.Value >= r1.hf.Value) errs.Add($"{day}, Rango 1: la hora inicial debe ser menor que la final.");
            if (r2.hi.HasValue && r2.hf.HasValue && r2.hi.Value >= r2.hf.Value) errs.Add($"{day}, Rango 2: la hora inicial debe ser menor que la final.");
            if (r3.hi.HasValue && r3.hf.HasValue && r3.hi.Value >= r3.hf.Value) errs.Add($"{day}, Rango 3: la hora inicial debe ser menor que la final.");
            return errs;
        }

        private static bool HasAtLeastOneCompleteRange(bool enabled,
            TimeSpan? a1, TimeSpan? b1, TimeSpan? a2, TimeSpan? b2, TimeSpan? a3, TimeSpan? b3)
        {
            if (!enabled) return true;
            return IsPairCompleteValid(a1, b1) || IsPairCompleteValid(a2, b2) || IsPairCompleteValid(a3, b3);
        }

        private static IEnumerable<string> ValidateAtLeastOneRangePerEnabledDay(RangeUpsertDto d)
        {
            var errs = new List<string>();
            if (!HasAtLeastOneCompleteRange(d.Monday, d.Lr1Hi, d.Lr1Hf, d.Lr2Hi, d.Lr2Hf, d.Lr3Hi, d.Lr3Hf)) errs.Add("Lunes: debe diligenciar al menos un rango completo.");
            if (!HasAtLeastOneCompleteRange(d.Tuesday, d.Mr1Hi, d.Mr1Hf, d.Mr2Hi, d.Mr2Hf, d.Mr3Hi, d.Mr3Hf)) errs.Add("Martes: debe diligenciar al menos un rango completo.");
            if (!HasAtLeastOneCompleteRange(d.Wednesday, d.Wr1Hi, d.Wr1Hf, d.Wr2Hi, d.Wr2Hf, d.Wr3Hi, d.Wr3Hf)) errs.Add("Miércoles: debe diligenciar al menos un rango completo.");
            if (!HasAtLeastOneCompleteRange(d.Thursday, d.Jr1Hi, d.Jr1Hf, d.Jr2Hi, d.Jr2Hf, d.Jr3Hi, d.Jr3Hf)) errs.Add("Jueves: debe diligenciar al menos un rango completo.");
            if (!HasAtLeastOneCompleteRange(d.Friday, d.Vr1Hi, d.Vr1Hf, d.Vr2Hi, d.Vr2Hf, d.Vr3Hi, d.Vr3Hf)) errs.Add("Viernes: debe diligenciar al menos un rango completo.");
            if (!HasAtLeastOneCompleteRange(d.Saturday, d.Sr1Hi, d.Sr1Hf, d.Sr2Hi, d.Sr2Hf, d.Sr3Hi, d.Sr3Hf)) errs.Add("Sábado: debe diligenciar al menos un rango completo.");
            if (!HasAtLeastOneCompleteRange(d.Sunday, d.Dr1Hi, d.Dr1Hf, d.Dr2Hi, d.Dr2Hf, d.Dr3Hi, d.Dr3Hf)) errs.Add("Domingo: debe diligenciar al menos un rango completo.");
            if (!HasAtLeastOneCompleteRange(d.Holiday, d.Fr1Hi, d.Fr1Hf, d.Fr2Hi, d.Fr2Hf, d.Fr3Hi, d.Fr3Hf)) errs.Add("Festivo: debe diligenciar al menos un rango completo.");
            return errs;
        }

        private static IEnumerable<string> ValidateAllDayRows(RangeUpsertDto d)
        {
            var errs = new List<string>();

            void Acc(string day, bool enabled, (TimeSpan?, TimeSpan?) r1, (TimeSpan?, TimeSpan?) r2, (TimeSpan?, TimeSpan?) r3)
            {
                if (!enabled) return;
                errs.AddRange(ValidateIncompletePairs(day, r1, r2, r3));
            }

            Acc("Lunes", d.Monday, (d.Lr1Hi, d.Lr1Hf), (d.Lr2Hi, d.Lr2Hf), (d.Lr3Hi, d.Lr3Hf));
            Acc("Martes", d.Tuesday, (d.Mr1Hi, d.Mr1Hf), (d.Mr2Hi, d.Mr2Hf), (d.Mr3Hi, d.Mr3Hf));
            Acc("Miércoles", d.Wednesday, (d.Wr1Hi, d.Wr1Hf), (d.Wr2Hi, d.Wr2Hf), (d.Wr3Hi, d.Wr3Hf));
            Acc("Jueves", d.Thursday, (d.Jr1Hi, d.Jr1Hf), (d.Jr2Hi, d.Jr2Hf), (d.Jr3Hi, d.Jr3Hf));
            Acc("Viernes", d.Friday, (d.Vr1Hi, d.Vr1Hf), (d.Vr2Hi, d.Vr2Hf), (d.Vr3Hi, d.Vr3Hf));
            Acc("Sábado", d.Saturday, (d.Sr1Hi, d.Sr1Hf), (d.Sr2Hi, d.Sr2Hf), (d.Sr3Hi, d.Sr3Hf));
            Acc("Domingo", d.Sunday, (d.Dr1Hi, d.Dr1Hf), (d.Dr2Hi, d.Dr2Hf), (d.Dr3Hi, d.Dr3Hf));
            Acc("Festivo", d.Holiday, (d.Fr1Hi, d.Fr1Hf), (d.Fr2Hi, d.Fr2Hf), (d.Fr3Hi, d.Fr3Hf));

            return errs;
        }
    }
}