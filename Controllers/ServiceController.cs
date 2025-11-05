using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Filters;
using VCashApp.Models;
using VCashApp.Models.ViewModels.Servicio;
using VCashApp.Services.GestionServicio.Application;
using VCashApp.Services.DTOs;
using VCashApp.Infrastructure.Branches;

namespace VCashApp.Controllers
{
    /// <summary>
    /// Controlador para la gestión de solicitudes del Centro de Gestión de Servicios (CGS).
    /// </summary>
    [Authorize]
    [Route("/Service")]
    public class ServiceController : BaseController
    {
        private readonly ICgsServiceApp _service;
        private readonly ILogger<ServiceController> _logger;
        private readonly IBranchContext _branchContext;

        public ServiceController(
            ICgsServiceApp service,
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ServiceController> logger,
            IBranchContext branchContext)
            : base(context, userManager)
        {
            _service = service;
            _logger = logger;
            _branchContext = branchContext;
        }

        private async Task SetCommonViewBagsCgsAsync(ApplicationUser currentUser, string pageName)
        {
            await base.SetCommonViewBagsBaseAsync(currentUser, pageName);

            ViewBag.AvailableBranches = (await _service.GetBranchesForDropdownAsync());
            ViewBag.AvailableClients = (await _service.GetClientsForDropdownAsync());
            ViewBag.AvailableConcepts = (await _service.GetServiceConceptsForDropdownAsync());
            ViewBag.AvailableStatuses = (await _service.GetServiceStatusesForDropdownAsync());

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            ViewBag.HasCreatePermission = await HasPermisionForView(userRoles, "CGS", PermissionType.Create);
            ViewBag.HasEditPermission = await HasPermisionForView(userRoles, "CGS", PermissionType.Edit);
            ViewBag.HasViewPermission = await HasPermisionForView(userRoles, "CGS", PermissionType.View);
        }

        private static int GenerateGenericKey()
        {
            return Random.Shared.Next(1000, 10000);
        }

        /// <summary>
        /// Muestra el dashboard principal del Centro de Gestión de Servicios, permitiendo filtrar y ver las solicitudes.
        /// </summary>
        [HttpGet("Index")]
        [RequiredPermission(PermissionType.View, "CGS")]
        public async Task<IActionResult> Index(
            string? search, int? clientCode, int? branchCode, int? conceptCode, DateOnly? startDate, DateOnly? endDate, int? status,
            int page = 1, int pageSize = 15)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsCgsAsync(currentUser, "Gestión de Servicios");
            bool isAdmin = ViewBag.IsAdmin;

            (List<CgsServiceSummaryViewModel> serviceRequests, int totalRecords) = await _service.GetFilteredServiceRequestsAsync(
                search, clientCode, branchCode, conceptCode, startDate, endDate, status, page, pageSize, currentUser.Id, isAdmin);

            var dashboardViewModel = new CgsDashboardViewModel
            {
                ServiceRequests = serviceRequests,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                TotalData = totalRecords,
                SearchTerm = search,
                CurrentClientCode = clientCode,
                CurrentBranchCode = branchCode,
                CurrentConceptCode = conceptCode,
                CurrentStartDate = startDate,
                CurrentEndDate = endDate,
                CurrentStatus = status,
                AvailableClients = ViewBag.AvailableClients as List<SelectListItem>,
                AvailableBranches = ViewBag.AvailableBranches as List<SelectListItem>,
                AvailableConcepts = ViewBag.AvailableConcepts as List<SelectListItem>,
                AvailableStatuses = ViewBag.AvailableStatuses as List<SelectListItem>
            };

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = dashboardViewModel.TotalPages;
            ViewBag.TotalData = totalRecords;
            ViewBag.SearchTerm = search;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_ServiceRequestTablePartial", serviceRequests);
            }

            return View(dashboardViewModel);
        }

        /// <summary>
        /// Muestra el formulario para crear una nueva solicitud de servicio.
        /// </summary>
        [HttpGet("Create")]
        [RequiredPermission(PermissionType.Create, "CGS")]
        public async Task<IActionResult> Create()
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsCgsAsync(currentUser, "Nueva Solicitud CGS");

            var viewModel = await _service.ServiceRequestAsync(currentUser.Id, IpAddressForLogging);
            viewModel.KeyValue = GenerateGenericKey();

            viewModel.CgsOperatorUserName ??= currentUser.UserName ?? currentUser.Email ?? "N/D";
            viewModel.OperatorIpAddress ??= IpAddressForLogging ?? HttpContext.Connection.RemoteIpAddress?.ToString();

            return View(viewModel);
        }

        /// <summary>
        /// Procesa el envío del formulario para crear una solicitud de servicio CGS.
        /// </summary>
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Create, "CGS")]
        public async Task<IActionResult> Create(CgsServiceRequestViewModel viewModel)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null)
                return Unauthorized();

            await SetCommonViewBagsCgsAsync(currentUser, "Procesando Solicitud CGS");

            if (!ModelState.IsValid)
            {
                var fieldErrors = ModelState
                    .Where(kvp => kvp.Value.Errors.Any())
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                _logger.LogWarning("Usuario: {Usuario} | IP: {IP} | Acción: CGS - Modelo Inválido | Errores: {@Errores}",
                    currentUser.UserName, IpAddressForLogging, fieldErrors);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(ServiceResult.FailureResult("Hay errores en el formulario.", errors: fieldErrors));
                }

                await PopulateViewModelDropdownsAsync(viewModel);
                return View(viewModel);
            }

            try
            {
                viewModel.KeyValue ??= GenerateGenericKey();
                var result = await _service.CreateServiceRequestAsync(viewModel, currentUser.Id, IpAddressForLogging);

                if (result.Success)
                {
                    var successMessage = $"Solicitud creada exitosamente. Clave: {viewModel.KeyValue:0000}";
                    TempData["SuccessMessage"] = successMessage;
                    _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Solicitud CGS creada | Orden de Servicio: {ServiceOrderId} | Clave: {Clave}",
                        currentUser.UserName, IpAddressForLogging, result.Data, viewModel.KeyValue);

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(ServiceResult.SuccessResult(successMessage, result.Data));
                    }

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", result.Message);

                    _logger.LogError("Usuario: {Usuario} | IP: {IP} | Acción: Error al crear solicitud CGS | Mensaje: {Mensaje}",
                        currentUser.UserName, IpAddressForLogging, result.Message);

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(ServiceResult.FailureResult(result.Message));
                    }

                    await PopulateViewModelDropdownsAsync(viewModel);
                    return View(viewModel);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ocurrió un error inesperado al crear la solicitud.");
                _logger.LogError(ex, "Usuario: {Usuario} | IP: {IP} | Acción: Error inesperado al crear solicitud CGS.",
                    currentUser.UserName, IpAddressForLogging);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(ServiceResult.FailureResult("Ocurrió un error inesperado."));
                }

                await PopulateViewModelDropdownsAsync(viewModel);
                return View(viewModel);
            }
        }


        // --- Acciones de API para la carga dinámica de dropdowns ---

        /// <summary>
        /// Obtiene detalles de un punto, ATM o fondo para autocompletar.
        /// </summary>
        /// <param name="code">Código del punto/ATM o fondo.</param>
        /// <param name="clientId">ID del cliente.</param>
        /// <param name="isPoint">True si es un punto/ATM, false si es un fondo.</param>
        /// <returns>JSON con los detalles.</returns>
        [HttpGet("GetLocationDetails")]
        public async Task<IActionResult> GetLocationDetails(string code, int clientId, bool isPoint)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            var details = await _service.GetLocationDetailsByCodeAsync(code, clientId, isPoint);

            if (details == null)
            {
                return Json(ServiceResult.FailureResult("Detalles de ubicación no encontrados."));
            }

            return Json(ServiceResult.SuccessResult("Detalles obtenidos.", details));
        }

        /// <summary>
        /// Obtiene una lista de puntos o fondos para un cliente y sucursal específicos.
        /// </summary>
        [HttpGet("GetLocations")]
        [RequiredPermission(PermissionType.View, "CGS")]
        public async Task<IActionResult> GetLocations(int clientId, int branchId, string locationType, string conceptCode)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            List<SelectListItem> locations = new List<SelectListItem>();

            var pointType = (locationType == "A") ? 1 : ((conceptCode == "3" || conceptCode == "4") ? 1 : 0);

            if (locationType == "P" || locationType == "A")
            {
                locations = await _service.GetPointsByClientAndBranchAsync(clientId, branchId, pointType);
            }
            else if (locationType == "F")
            {
                var fundType = (conceptCode == "3" || conceptCode == "4") ? 1 : 0;
                locations = await _service.GetFundsByClientAndBranchAsync(clientId, branchId, fundType);
            }

            return Json(locations);
        }

        private async Task PopulateViewModelDropdownsAsync(CgsServiceRequestViewModel viewModel)
        {
            viewModel.AvailableClients = await _service.GetClientsForDropdownAsync();
            viewModel.AvailableBranches = await _service.GetBranchesForDropdownAsync();
            viewModel.AvailableConcepts = await _service.GetServiceConceptsForDropdownAsync();
            viewModel.AvailableStatuses = await _context.AdmEstados
                                                     .Select(e => new SelectListItem { Value = e.StateCode.ToString(), Text = e.StateName })
                                                     .OrderBy(e => e.Text)
                                                     .ToListAsync();
            viewModel.AvailableTransferTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "N", Text = "Normal (Predeterminado)" },
                new SelectListItem { Value = "I", Text = "Interno" },
                new SelectListItem { Value = "T", Text = "Transportadora" }
            };
            viewModel.AvailableServiceModalities = await _service.GetServiceModalitiesForDropdownAsync();
            viewModel.AvailableCurrencies = await _service.GetCurrenciesForDropdownAsync();
        }
    }
}