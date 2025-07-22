using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCashApp.Data;
using VCashApp.Models;
using VCashApp.Models.ViewModels;
using VCashApp.Services;
using VCashApp.Enums;
using VCashApp.Filters;
using VCashApp.Services.DTOs;

namespace VCashApp.Controllers
{
    /// <summary>
    /// Controlador para la gestión de empleados. Sigue un patrón de servicio para la lógica de negocio.
    /// Proporciona funcionalidades para listar, crear, editar y visualizar información de empleados.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("/Employee")]
    public class EmployeeController : BaseController
    {
        private readonly IEmployeeService _employeeService;
        private readonly IExportService _exportService;

        /// <summary>
        /// Constructor del controlador EmployeeController.
        /// </summary>
        public EmployeeController(
            IEmployeeService employeeService,
            IExportService exportService,
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
            : base(context, userManager)
        {
            _employeeService = employeeService;
            _exportService = exportService;
        }

        // Método auxiliar para configurar ViewBags comunes
        private async Task SetCommonViewBagsEmployeeAsync(ApplicationUser currentUser, string pageName)
        {
            await base.SetCommonViewBagsBaseAsync(currentUser, pageName);

            bool isAdmin = (bool)ViewBag.IsAdmin;
            var (cargos, sucursales, ciudades) = await _employeeService.GetDropdownListsAsync(currentUser.Id, isAdmin);

            ViewBag.Cargos = new SelectList(cargos, "Value", "Text");
            ViewBag.Sucursales = new SelectList(sucursales, "Value", "Text");
            ViewBag.Ciudades = new SelectList(ciudades, "Value", "Text");

            // Obtener permisos
            var userRoles = await _userManager.GetRolesAsync(currentUser);
            ViewBag.HasCreatePermission = await HasPermisionForView(userRoles, "EMP", PermissionType.Create);
            ViewBag.HasEditPermission = await HasPermisionForView(userRoles, "EMP", PermissionType.Edit);
            ViewBag.HasViewPermission = await HasPermisionForView(userRoles, "EMP", PermissionType.View);
        }

        /// <summary>
        /// Muestra una lista paginada y filtrada de empleados.
        /// </summary>
        [HttpGet("Index")]
        [RequiredPermission(PermissionType.View, "EMP")]
        public async Task<IActionResult> Index(int? cargoId, int? branchId, int? employeeStatus, string? search, string? gender, int page = 1, int pageSize = 15)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsEmployeeAsync(currentUser, "Empleados");
            bool isAdmin = (bool)ViewBag.IsAdmin;

            var (data, totalData) = await _employeeService.GetFilteredEmployeesAsync(
                currentUser.Id,
                cargoId,
                branchId,
                employeeStatus,
                search,
                gender,
                page,
                pageSize,
                isAdmin
            );

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalData / pageSize);
            ViewBag.TotalData = totalData;
            ViewBag.SearchTerm = search;
            ViewBag.CurrentCargoId = cargoId;
            ViewBag.CurrentBranchId = branchId;
            ViewBag.CurrentEmployeeStatus = employeeStatus;
            ViewBag.CurrentGender = gender;

            Log.Information("| User: {User} | IP: {Ip} | Action: Accessing Employee List | Count: {Count} | Result: Access Granted |", currentUser.UserName, ViewBag.Ip, data.Count());

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_EmployeeTablePartial", data);
            }

            return View(data);
        }

        /// <summary>
        /// Muestra el formulario para crear un nuevo empleado.
        /// </summary>
        [HttpGet("Create")]
        [RequiredPermission(PermissionType.Create, "EMP")]
        public async Task<IActionResult> Create()
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsBaseAsync(currentUser, "Crear Empleado");
            bool isAdmin = (bool)ViewBag.IsAdmin;

            var (cargos, sucursales, ciudades) = await _employeeService.GetDropdownListsAsync(currentUser.Id, isAdmin);

            var model = new EmpleadoViewModel
            {
                Cargos = cargos,
                Sucursales = sucursales,
                Ciudades = ciudades,
                IndicadorCatalogo = false,
                IngresoRepublica = false,
                IngresoAeropuerto = false
            };

            Log.Information("| User: {User} | IP: {Ip} | Action: Accessing Create Employee Form | Result: Access Granted |", currentUser.UserName, ViewBag.Ip);

            return View(model); // Retorna el ViewModel con las listas
        }

        /// <summary>
        /// Procesa la creación de un nuevo empleado.
        /// </summary>
        // En EmployeeController.cs

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Create, "EMP")]
        public async Task<IActionResult> Create([FromForm] EmpleadoViewModel model)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            if (!ModelState.IsValid)
            {
                var fieldErrors = ModelState
                    .Where(kvp => kvp.Value.Errors.Any())
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return Json(ServiceResult.FailureResult("Hay errores en el formulario.", errors: fieldErrors));
            }

            var result = await _employeeService.CreateEmployeeAsync(model, currentUser.Id);
            return Json(result);
        }

        /// <summary>
        /// Muestra el formulario para editar un empleado existente.
        /// </summary>
        [HttpGet("Edit/{id}")]
        [RequiredPermission(PermissionType.Edit, "EMP")]
        public async Task<IActionResult> Edit(int id)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsBaseAsync(currentUser, "Editar Empleado");
            bool isAdmin = (bool)ViewBag.IsAdmin;

            // Obtener los datos del empleado desde el servicio
            var employeeModel = await _employeeService.GetEmployeeForEditAsync(id, currentUser.Id, isAdmin);

            if (employeeModel == null)
            {
                Log.Warning("| User: {User} | IP: {Ip} | Action: Accessing Edit Employee Form | EmployeeId: {EmployeeId} | Result: Not Found or Forbidden |", currentUser.UserName, ViewBag.Ip, id);
                TempData["ErrorMessage"] = "Empleado no encontrado o no tiene permiso para verlo.";
                return RedirectToAction(nameof(Index));
            }

            var (cargos, sucursales, ciudades) = await _employeeService.GetDropdownListsAsync(currentUser.Id, isAdmin);
            employeeModel.Cargos = cargos;
            employeeModel.Sucursales = sucursales;
            employeeModel.Ciudades = ciudades;

            Log.Information("| User: {User} | IP: {Ip} | Action: Accessing Edit Employee Form | EmployeeId: {EmployeeId} | Result: Access Granted |", currentUser.UserName, ViewBag.Ip, id);
            return View(employeeModel);
        }

        /// <summary>
        /// Procesa la actualización de un empleado existente.
        /// </summary>
        [HttpPost("Edit")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "EMP")]
        public async Task<IActionResult> Edit(EmpleadoViewModel model)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            if (!ModelState.IsValid)
            {
                var fieldErrors = ModelState
                    .Where(kvp => kvp.Value.Errors.Any())
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return Json(ServiceResult.FailureResult("Hay errores en el formulario.", errors: fieldErrors));
            }

            // Llamada al servicio para actualizar el empleado
            var result = await _employeeService.UpdateEmployeeAsync(model, currentUser.Id);

            if (result.Success)
            {
                Log.Information("| User: {User} | IP: {Ip} | Action: Employee Updated | EmployeeId: {EmployeeId} | Result: Success |", currentUser.UserName, ViewBag.Ip, model.CodCedula);
            }
            else
            {
                Log.Warning("| User: {User} | IP: {Ip} | Action: Employee Update Failed | EmployeeId: {EmployeeId} | Result: {Message} |", currentUser.UserName, ViewBag.Ip, model.CodCedula, result.Message);
            }

            return Json(result);
        }

        /// <summary>
        /// Sirve archivos de imagen (fotos o firmas) de empleados desde el repositorio.
        /// </summary>
        [HttpGet("images/{*filePath}")]
        public async Task<IActionResult> GetImage(string filePath)
        {
            // La lógica para obtener el archivo ahora puede estar en el servicio
            var fileStream = await _employeeService.GetEmployeeImageStreamAsync(filePath);
            if (fileStream == null)
            {
                return NotFound();
            }

            string mimeType = "image/jpeg"; // O determinar el tipo de contenido dinámicamente
            return File(fileStream, mimeType);
        }
    }
}