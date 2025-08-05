using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCashApp.Data;
using VCashApp.Filters;
using VCashApp.Models;
using VCashApp.Models.ViewModels.Servicio;
using VCashApp.Services;
using VCashApp.Services.Service;
using VCashApp.Services.DTOs;

namespace VCashApp.Controllers
{
    /// <summary>
    /// Controlador para la gestión de solicitudes del Centro de Gestión de Servicios (CGS).
    /// </summary>
    [Authorize]
    [Route("/Service")]
    public class ServiceController : BaseController
    {
        private readonly ICgsServiceService _cgsService;
        private readonly ILogger<ServiceController> _logger;

        public ServiceController(
            ICgsServiceService cgsService,
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ServiceController> logger)
            : base(context, userManager)
        {
            _cgsService = cgsService;
            _logger = logger;
        }

        private async Task SetCommonViewBagsCgsAsync(ApplicationUser currentUser, string pageName)
        {
            await base.SetCommonViewBagsBaseAsync(currentUser, pageName);

            // La variable isAdmin ya se ha establecido en el ViewBag por SetCommonViewBagsBaseAsync
            ViewBag.AvailableBranches = (await _cgsService.GetBranchesForDropdownAsync());
            ViewBag.AvailableClients = (await _cgsService.GetClientsForDropdownAsync());
            ViewBag.AvailableConcepts = (await _cgsService.GetServiceConceptsForDropdownAsync());
            ViewBag.AvailableStatuses = (await _cgsService.GetServiceStatusesForDropdownAsync());

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            ViewBag.HasCreatePermission = await HasPermisionForView(userRoles, "CGS", PermissionType.Create);
            ViewBag.HasEditPermission = await HasPermisionForView(userRoles, "CGS", PermissionType.Edit);
            ViewBag.HasViewPermission = await HasPermisionForView(userRoles, "CGS", PermissionType.View);
        }

        /// <summary>
        /// Muestra el dashboard principal del Centro de Gestión de Servicios, permitiendo filtrar y ver las solicitudes.
        /// </summary>
        [HttpGet("Index")]
        [RequiredPermission(PermissionType.View, "CGS")]
        public async Task<IActionResult> Index(
            string? search, int? clientCode, int? branchCode, int? conceptCode, DateOnly? startDate, DateOnly? endDate, int? status,
            int pageNumber = 1, int pageSize = 15)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsCgsAsync(currentUser, "Gestión de Servicios");
            bool isAdmin = ViewBag.IsAdmin;

            (List<CgsServiceSummaryViewModel> serviceRequests, int totalRecords) = await _cgsService.GetFilteredServiceRequestsAsync(
                search, clientCode, branchCode, conceptCode, startDate, endDate, status, pageNumber, pageSize, currentUser.Id, isAdmin);

            var dashboardViewModel = new CgsDashboardViewModel
            {
                ServiceRequests = serviceRequests,
                CurrentPage = pageNumber,
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

            ViewBag.CurrentPage = pageNumber;
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
        [RequiredPermission(PermissionType.Create, "CGS")] // Requiere permiso de Creación
        public async Task<IActionResult> Create()
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsCgsAsync(currentUser, "Nueva Solicitud CGS");

            var viewModel = await _cgsService.PrepareServiceRequestViewModelAsync(currentUser.Id, IpAddressForLogging);

            return View(viewModel);
        }

        /// <summary>
        /// Procesa el envío del formulario para crear una solicitud de servicio.
        /// </summary>
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Create, "CGS")]
        public async Task<IActionResult> Create(CgsServiceRequestViewModel viewModel)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            await SetCommonViewBagsCgsAsync(currentUser, "Procesando Solicitud CGS");

            if (!ModelState.IsValid)
            {
                // Re-poblar las listas para el retorno de la vista
                await PopulateViewModelDropdownsAsync(viewModel);
                return View(viewModel);
            }

            var serviceResult = await _cgsService.CreateServiceRequestAsync(viewModel, currentUser.Id, IpAddressForLogging);

            if (serviceResult.Success)
            {
                TempData["SuccessMessage"] = serviceResult.Message;
                _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Solicitud CGS creada exitosamente | Orden de Servicio: {ServiceOrderId}",
                    currentUser.UserName, IpAddressForLogging, serviceResult.Data);
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ModelState.AddModelError("", serviceResult.Message);
                // Re-poblar las listas para el retorno de la vista
                await PopulateViewModelDropdownsAsync(viewModel);
                _logger.LogError("Usuario: {Usuario} | IP: {IP} | Acción: Error al crear solicitud CGS | Mensaje: {ErrorMessage}",
                    currentUser.UserName, IpAddressForLogging, serviceResult.Message);
                return View(viewModel);
            }
        }

        // --- Acciones de API para la carga dinámica de dropdowns ---

        /// <summary>
        /// Obtiene una lista de puntos o fondos para un cliente y sucursal específicos.
        /// </summary>
        [HttpGet("api/GetLocations")]
        [RequiredPermission(PermissionType.View, "CGS")]
        public async Task<IActionResult> GetLocations(int clientId, int branchId, string locationType, string conceptCode)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            List<SelectListItem> locations = new List<SelectListItem>();

            // Lógica para determinar el tipo de punto a buscar (0=Oficina/Punto, 1=ATM)
            // Esto es una suposición basada en tu flujo.
            var pointType = (conceptCode == "3" || conceptCode == "4") ? 1 : 0;

            if (locationType == "P") // Si el tipo de ubicación es Punto
            {
                locations = await _cgsService.GetPointsByClientAndBranchAsync(clientId, branchId, pointType);
            }
            else if (locationType == "F") // Si el tipo de ubicación es Fondo
            {
                // Lógica para determinar el tipo de fondo a buscar (0=punto, 1=ATM)
                var fundType = (conceptCode == "3" || conceptCode == "4") ? 1 : 0;
                locations = await _cgsService.GetFundsByClientAndBranchAsync(clientId, branchId, fundType);
            }

            return Json(locations);
        }

        private async Task PopulateViewModelDropdownsAsync(CgsServiceRequestViewModel viewModel)
        {
            viewModel.AvailableClients = await _cgsService.GetClientsForDropdownAsync();
            viewModel.AvailableBranches = await _cgsService.GetBranchesForDropdownAsync();
            viewModel.AvailableConcepts = await _cgsService.GetServiceConceptsForDropdownAsync();
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
            viewModel.AvailableServiceModalities = await _cgsService.GetServiceModalitiesForDropdownAsync();
        }
    }
}