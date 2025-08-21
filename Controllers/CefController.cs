using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VCashApp.Utils;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Filters;
using VCashApp.Models;
using VCashApp.Models.ViewModels.CentroEfectivo;
using VCashApp.Services;
using VCashApp.Services.DTOs;

namespace VCashApp.Controllers
{
    /// <summary>
    /// Controlador para la gestión de operaciones de Centro de Efectivo (CEF).
    /// Maneja el flujo de Check-in, conteo de contenedores y revisión de transacciones.
    /// </summary>
    [Authorize]
    [Route("/Cef")]
    public class CefController : BaseController
    {
        private readonly ICefTransactionService _cefTransactionService;
        private readonly ICefContainerService _cefContainerService;
        private readonly ICefIncidentService _cefIncidentService;
        private readonly ICefServiceCreationService _cefServiceCreationService; //TEMPORAL
        private readonly IExportService _exportService;
        private readonly ILogger<CefController> _logger;

        /// <summary>
        /// Constructor del controlador CefController.
        /// </summary>
        /// <param name="cefTransactionService">Servicio para la lógica de transacciones CEF.</param>
        /// <param name="cefContainerService">Servicio para la lógica de contenedores CEF.</param>
        /// <param name="cefIncidentService">Servicio para la lógica de novedades CEF.</param>
        /// <param name="context">Contexto de la base de datos de la aplicación.</param>
        /// <param name="userManager">Administrador de usuarios de Identity.</param>
        /// <param name="logger">Servicio de logging para el controlador.</param>
        public CefController(
            ICefTransactionService cefTransactionService,
            ICefContainerService cefContainerService,
            ICefIncidentService cefIncidentService,
            ICefServiceCreationService cefServiceCreationService,
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<CefController> logger)
            : base(context, userManager)
        {
            _cefTransactionService = cefTransactionService;
            _cefContainerService = cefContainerService;
            _cefIncidentService = cefIncidentService;
            _cefServiceCreationService = cefServiceCreationService; //TEMPORAL
            _logger = logger;
        }

        /// <summary>
        /// Método auxiliar para configurar ViewBags comunes específicos de CEF.
        /// Hereda y extiende el SetCommonViewBagsBaseAsync del BaseController.
        /// </summary>
        /// <param name="currentUser">El usuario actual autenticado.</param>
        /// <param name="pageName">El nombre de la página para el ViewBag.</param>
        /// <returns>Tarea asíncrona completada.</returns>
        private async Task SetCommonViewBagsCefAsync(ApplicationUser currentUser, string pageName)
        {
            await base.SetCommonViewBagsBaseAsync(currentUser, pageName);
            bool isAdmin = ViewBag.IsAdmin;

            var (sucursales, estados) = await _cefTransactionService.GetDropdownListsAsync(currentUser.Id, isAdmin);

            ViewBag.AvailableBranches = sucursales;
            ViewBag.TransactionStatuses = estados;

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            ViewBag.HasCreate = await HasPermisionForView(userRoles, "CEF", PermissionType.Create);
            ViewBag.HasEdit = await HasPermisionForView(userRoles, "CEF", PermissionType.Edit);
            ViewBag.HasView = await HasPermisionForView(userRoles, "CEF", PermissionType.View);
            // ViewBag.HasDeletePermission = await HasPermisionForView(userRoles, "CEF", PermissionType.Delete);
        }

        /// <summary>
        /// Muestra el dashboard principal del Centro de Efectivo, permitiendo filtrar y ver las transacciones.
        /// </summary>
        /// <remarks>
        /// Requiere permiso 'View' para el módulo "CEF".
        /// </remarks>
        /// <param name="branchId">ID de la sucursal para filtrar.</param>
        /// <param name="startDate">Fecha de inicio para el filtro de registro.</param>
        /// <param name="endDate">Fecha de fin para el filtro de registro.</param>
        /// <param name="status">Estado de la transacción para filtrar.</param>
        /// <param name="search">Término de búsqueda.</param>
        /// <param name="page">Número de página para paginación.</param>
        /// <param name="pageSize">Cantidad de registros por página.</param>
        /// <returns>Una vista con la lista de transacciones de CEF o un parcial si es una petición AJAX.</returns>
        [HttpGet("Index")]
        [RequiredPermission(PermissionType.View, "CEF")]
        public async Task<IActionResult> Index(
            int? branchId, DateOnly? startDate, DateOnly? endDate, CefTransactionStatusEnum? status,
            string? search, int page = 1, int pageSize = 15)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsCefAsync(currentUser, "Tesoreria");
            bool isAdmin = ViewBag.IsAdmin;

            var (transactions, totalRecords) = await _cefTransactionService.GetFilteredCefTransactionsAsync(
                currentUser.Id, branchId, startDate, endDate, status, search, page, pageSize, isAdmin);

            var transactionStatuses = Enum.GetValues(typeof(CefTransactionStatusEnum))
                .Cast<CefTransactionStatusEnum>()
                .Select(e => new SelectListItem { Value = e.ToString(), Text = e.ToString().Replace("_", " ") })
                .ToList();
            transactionStatuses.Insert(0, new SelectListItem { Value = "", Text = "-- Seleccionar Estado --" });

            var dashboardViewModel = new CefDashboardViewModel
            {
                Transactions = transactions,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                TotalData = totalRecords,
                SearchTerm = search,
                CurrentBranchId = branchId,
                CurrentStartDate = startDate,
                CurrentEndDate = endDate,
                CurrentStatus = status,
                AvailableBranches = ViewBag.AvailableBranches as List<SelectListItem>,
                TransactionStatuses = ViewBag.TransactionStatuses as List<SelectListItem>
            };

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = dashboardViewModel.TotalPages;
            ViewBag.TotalData = totalRecords;
            ViewBag.SearchTerm = search;
            ViewBag.CurrentBranchId = branchId;
            ViewBag.CurrentStartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentEndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentStatus = status?.ToString();
            ViewBag.TransactionStatuses = transactionStatuses;


            _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Acceso a Dashboard CEF | Conteo: {Conteo} |",
                currentUser.UserName, IpAddressForLogging, transactions.Count());

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_TransactionTablePartial", transactions);
            }

            return View(dashboardViewModel);
        }

        /// <summary>
        /// Muestra el formulario para iniciar una nueva transacción de Centro de Efectivo (Check-in).
        /// </summary>
        /// <remarks>
        /// Requiere permiso 'Create' para el módulo "CEF".
        /// </remarks>
        /// <param name="serviceOrderId">Orden de Servicio opcional para precargar datos.</param>
        /// <param name="routeId">ID de Ruta Diaria opcional para precargar datos.</param>
        /// <returns>Vista del formulario de Check-in.</returns>
        [HttpGet("Checkin")]
        [RequiredPermission(PermissionType.Create, "CEF")] // Solo si el usuario puede "Crear" en "CEF"
        public async Task<IActionResult> Checkin(string? serviceOrderId, string? routeId)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsCefAsync(currentUser, "Check-in CEF"); // Configura ViewBags comunes

            CefTransactionCheckinViewModel viewModel;
            if (!string.IsNullOrEmpty(serviceOrderId))
            {
                try
                {
                    viewModel = await _cefTransactionService.PrepareCheckinViewModelAsync(serviceOrderId, currentUser.Id, IpAddressForLogging);
                }
                catch (InvalidOperationException ex)
                {
                    TempData["ErrorMessage"] = ex.Message;
                    _logger.LogError(ex, "Usuario: {Usuario} | IP: {IP} | Acción: Error al preparar Check-in para OS {ServiceOrderId}.", currentUser.UserName, IpAddressForLogging, serviceOrderId);
                    return RedirectToAction(nameof(Index));
                }
            }
            else
            {
                viewModel = new CefTransactionCheckinViewModel
                {
                    RegistrationDate = DateTime.Now,
                    RegistrationUserName = currentUser.NombreUsuario ?? "N/A",
                    IPAddress = IpAddressForLogging,
                    Currencies = new List<SelectListItem>
                    {
                        new ("COP", "COP"),
                        new ("USD", "USD")
                    },
                    TransactionTypes = Enum.GetValues(typeof(CefTransactionTypeEnum))
                                           .Cast<CefTransactionTypeEnum>()
                                           .Select(e => new SelectListItem
                                           {
                                               Value = e.ToString(),
                                               Text = e.ToString().Replace("_", " ")
                                           }).ToList()
                };
            }

            _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Acceso a formulario Check-in | OS: {ServiceOrderId}.", currentUser.UserName, IpAddressForLogging, serviceOrderId ?? "N/A");
            return View(viewModel);
        }

        /// <summary>
        /// Procesa el envío del formulario de Check-in y crea la transacción de CEF.
        /// </summary>
        /// <remarks>
        /// Este endpoint es llamado vía HTTP POST.
        /// Requiere permiso 'Create' para el módulo "CEF".
        /// </remarks>
        /// <param name="viewModel">ViewModel con los datos del formulario.</param>
        /// <returns>Redirección al dashboard o a la vista de procesamiento de contenedores.</returns>
        [HttpPost("Checkin")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Create, "CEF")]
        public async Task<IActionResult> Checkin(CefTransactionCheckinViewModel viewModel)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            await SetCommonViewBagsCefAsync(currentUser, "Procesando Check-in CEF");

            viewModel.Currencies = new List<SelectListItem> { new("COP", "COP"), new("USD", "USD") };
            viewModel.TransactionTypes = Enum.GetValues(typeof(CefTransactionTypeEnum)).Cast<CefTransactionTypeEnum>().Select(e => new SelectListItem { Value = e.ToString(), Text = e.ToString().Replace("_", " ") }).ToList();
            viewModel.RegistrationUserName = currentUser.NombreUsuario ?? "N/A";
            viewModel.IPAddress = IpAddressForLogging;

            if (!ModelState.IsValid)
            {
                var fieldErrors = ModelState
                    .Where(kvp => kvp.Value.Errors.Any())
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                _logger.LogWarning("Usuario: {Usuario} | IP: {IP} | Acción: Check-in - Modelo Inválido | Errores: {@Errores} |",
                    currentUser.UserName, IpAddressForLogging, fieldErrors);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(ServiceResult.FailureResult("Hay errores en el formulario.", errors: fieldErrors));
                }
                return View(viewModel);
            }
            try
            {
                var newTransaction = await _cefTransactionService.ProcessCheckinViewModelAsync(viewModel, currentUser.Id, IpAddressForLogging);
                TempData["SuccessMessage"] = $"Check-in para planilla {newTransaction.SlipNumber} registrado exitosamente. La transacción está lista para el conteo.";
                _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Check-in Exitoso | Transacción ID: {TransactionId} | Planilla: {PlanillaNumber}.",
                    currentUser.UserName, IpAddressForLogging, newTransaction.Id, newTransaction.SlipNumber);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var url = Url.Action(nameof(ProcessContainers), "Cef", new { transactionId = newTransaction.Id });
                    return Json(ServiceResult.SuccessResult("Check-in exitoso.", new { transactionId = newTransaction.Id, url }));
                }
                return RedirectToAction(nameof(ProcessContainers), new { transactionId = newTransaction.Id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                _logger.LogError(ex, "Usuario: {Usuario} | IP: {IP} | Acción: Error al procesar Check-in | Mensaje: {ErrorMessage}.", currentUser.UserName, IpAddressForLogging, ex.Message);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(ServiceResult.FailureResult(ex.Message));
                }
                return View(viewModel);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ocurrió un error inesperado al registrar el Check-in.");
                _logger.LogError(ex, "Usuario: {Usuario} | IP: {IP} | Acción: Error inesperado en Check-in.", currentUser.UserName, IpAddressForLogging);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(ServiceResult.FailureResult("Ocurrió un error inesperado."));
                }
                return View(viewModel);
            }
        }

        //////////// TEMPORAL ///////////////////

        /// <summary>
        /// Muestra el formulario unificado para crear un nuevo Servicio y Transacción CEF.
        /// </summary>
        /// <remarks>
        /// Este es un formulario temporal para suplir la falta del módulo de Servicios.
        /// Recibe un código de concepto para predefinir el tipo de servicio (ej: "RC", "PV").
        /// Requiere permiso 'Create' para el módulo "CEF".
        /// </remarks>
        /// <param name="serviceConceptCode">Código del concepto de servicio (ej: "RC" para Recolección Oficinas).</param>
        /// <returns>Vista del formulario de creación unificada.</returns>
        [HttpGet("CreateServiceAndCefTransaction/{serviceConceptCode?}")]
        [RequiredPermission(PermissionType.Create, "CEF")]
        public async Task<IActionResult> CreateServiceAndCefTransaction(string? serviceConceptCode)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            bool isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            var (availableBranches, availableServiceModalities, availableFailedReponsibles) = await _cefServiceCreationService.GetDropdownListsAsync(currentUser.Id, isAdmin);

            await SetCommonViewBagsCefAsync(currentUser, "Crear Servicio CEF");

            CefServiceCreationViewModel viewModel = await _cefServiceCreationService.PrepareCefServiceCreationViewModelAsync(
                currentUser.Id, IpAddressForLogging, serviceConceptCode);

            viewModel.AvailableBranches = availableBranches;
            viewModel.AvailableServiceModalities = availableServiceModalities;
            viewModel.AvailableFailedResponsibles = availableFailedReponsibles;

            // Si el serviceConceptCode es nulo o inválido, podrías redirigir o mostrar un error.
            if (string.IsNullOrEmpty(serviceConceptCode) || (viewModel.AvailableServiceConcepts != null && !viewModel.AvailableServiceConcepts.Any(s => s.Value == serviceConceptCode)))
            {
                // Si la URL no tiene un código válido, podemos dejar que el usuario lo seleccione.
                // O forzar una redirección: TempData["ErrorMessage"] = "Tipo de servicio no especificado o inválido."; return RedirectToAction(nameof(Index));
            }

            return View(viewModel);
        }

        /// <summary>
        /// Procesa el envío del formulario unificado para crear un nuevo Servicio y Transacción CEF.
        /// </summary>
        /// <remarks>
        /// Este es un endpoint temporal.
        /// Requiere permiso 'Create' para el módulo "CEF".
        /// </remarks>
        /// <param name="viewModel">ViewModel con todos los datos del formulario.</param>
        /// <returns>JSON ServiceResult o redirección.</returns>
        [HttpPost("CreateServiceAndCefTransaction")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Create, "CEF")]
        public async Task<IActionResult> CreateServiceAndCefTransaction(CefServiceCreationViewModel viewModel)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            await SetCommonViewBagsCefAsync(currentUser, "Procesando Creación de Servicio CEF");

            viewModel.AvailableServiceConcepts = await _cefServiceCreationService.GetServiceConceptsForDropdownAsync();
            viewModel.AvailableBranches = await _context.AdmSucursales.Where(s => s.Estado && s.CodSucursal != 32).Select(s => new SelectListItem { Value = s.CodSucursal.ToString(), Text = s.NombreSucursal }).ToListAsync();
            viewModel.AvailableClients = await _cefServiceCreationService.GetClientsForDropdownAsync();
            viewModel.AvailableCities = await _context.AdmCiudades.Where(c => c.Estado).Select(c => new SelectListItem { Value = c.CodCiudad.ToString(), Text = c.NombreCiudad }).ToListAsync();
            //viewModel.AvailableRanks = await _context.AdmRangos.Where(r => r.RangoEstado == 1).Select(r => new SelectListItem { Value = r.CodRango, Text = r.InfoRangoAtencion }).ToListAsync();
            viewModel.AvailableEmployees = new List<SelectListItem>();
            viewModel.AvailableVehicles = new List<SelectListItem>();
            viewModel.AvailableServiceModalities = new List<SelectListItem>();

            if (!ModelState.IsValid)
            {
                var fieldErrors = ModelState.Where(kvp => kvp.Value.Errors.Any()).ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                _logger.LogWarning("Usuario: {Usuario} | IP: {IP} | Acción: Creación de Servicio CEF - Modelo Inválido | Errores: {@Errores} |",
                    currentUser.UserName, IpAddressForLogging, fieldErrors);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(ServiceResult.FailureResult("Hay errores en el formulario.", errors: fieldErrors));
                }
                return View(viewModel);
            }

            try
            {
                // Llama al servicio unificado para crear AdmServicio y CefTransaction
                string newServiceOrderId = await _cefServiceCreationService.ProcessCefServiceCreationAsync(viewModel, currentUser.Id, IpAddressForLogging);

                TempData["SuccessMessage"] = $"Servicio '{newServiceOrderId}' y Transacción CEF inicial creados exitosamente.";
                _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Servicio CEF temporal creado | OrdenServicio: {ServiceId} |",
                    currentUser.UserName, IpAddressForLogging, newServiceOrderId);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(ServiceResult.SuccessResult("Servicio y Transacción CEF creados.", newServiceOrderId));
                }
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                _logger.LogError(ex, "Usuario: {Usuario} | IP: {IP} | Acción: Error en creación de Servicio CEF | Mensaje: {ErrorMessage}.", currentUser.UserName, IpAddressForLogging, ex.Message);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(ServiceResult.FailureResult(ex.Message));
                }
                return View(viewModel);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ocurrió un error inesperado al crear el servicio y transacción CEF.");
                _logger.LogError(ex, "Usuario: {Usuario} | IP: {IP} | Acción: Error inesperado en creación de Servicio CEF.", currentUser.UserName, IpAddressForLogging);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(ServiceResult.FailureResult("Ocurrió un error inesperado."));
                }
                return View(viewModel);
            }
        }

        //////////// TEMPORAL ///////////////////

        /// <summary>
        /// Muestra la vista para procesar (contar) los contenedores de una transacción de CEF.
        /// </summary>
        /// <remarks>
        /// Requiere permiso 'Edit' para el módulo "CEF".
        /// </remarks>
        /// <param name="transactionId">ID de la transacción CEF a procesar.</param>
        /// 
        /// <returns>Vista de procesamiento de contenedores.</returns>
        [HttpGet("ProcessContainers/{transactionId}")]
        [RequiredPermission(PermissionType.Edit, "CEF")]
        public async Task<IActionResult> ProcessContainers(int transactionId, int? containerId = null)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsCefAsync(currentUser, "Procesar Contenedores CEF");

            var pageVm = await _cefContainerService.PrepareProcessContainersPageAsync(transactionId);

            ViewBag.IncidentTypes = (await _cefIncidentService.GetAllIncidentTypesAsync()).Select(it => new SelectListItem { Value = it.Code, Text = it.Description }).ToList();
            ViewBag.DenomsJson = await _cefContainerService.BuildDenomsJsonForTransactionAsync(transactionId);
            ViewBag.QualitiesJson = await _cefContainerService.BuildQualitiesJsonAsync();

            return View(pageVm);
        }

        [HttpGet("ProcessTotals")]
        [RequiredPermission(PermissionType.Edit, "CEF")]
        public async Task<IActionResult> ProcessTotals(int transactionId)
        {
            var (declared, counted, diff) = await _cefContainerService.GetTransactionTotalsAsync(transactionId);
            return Json(new { declared, counted, diff });
        }


        /// <summary>
        /// Procesa el envío del formulario de procesamiento de contenedores (guardar detalles y novedades).
        /// </summary>
        /// <remarks>
        /// Este endpoint es llamado vía HTTP POST.
        /// Requiere permiso 'Edit' para el módulo "CEF".
        /// </remarks>
        /// <param name="viewModel">ViewModel con los datos del contenedor, detalles y novedades.</param>
        /// <returns>Un JSON ServiceResult o redirección a la misma vista para continuar o al dashboard.</returns>
        [HttpPost("ProcessContainers/{transactionId:int}")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "CEF")]
        public async Task<IActionResult> ProcessContainers(int transactionId, CefProcessContainersPageViewModel viewModel)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            await SetCommonViewBagsCefAsync(currentUser, "Procesando Contenedores CEF");

            // Asegura el id de la transacción en el VM
            viewModel.CefTransactionId = transactionId;

            // Catálogos por si hay que re-renderizar
            ViewBag.IncidentTypes = (await _cefIncidentService.GetAllIncidentTypesAsync())
                .Select(it => new SelectListItem { Value = it.Code, Text = it.Description })
                .ToList();
            ViewBag.DenomsJson = await _cefContainerService.BuildDenomsJsonForTransactionAsync(transactionId);
            ViewBag.QualitiesJson = await _cefContainerService.BuildQualitiesJsonAsync();

            // Validaciones mínimas
            if (viewModel?.Containers == null || viewModel.Containers.Count == 0)
                ModelState.AddModelError("", "No se recibieron contenedores para guardar.");

            // Duplicados por contenedor (Tipo + Denom + Calidad)
            if (viewModel?.Containers != null)
            {
                for (int cIdx = 0; cIdx < viewModel.Containers.Count; cIdx++)
                {
                    var c = viewModel.Containers[cIdx];
                    if (c?.ValueDetails == null) continue;

                    var dup = c.ValueDetails
                        .GroupBy(v => new { v.ValueType, v.DenominationId, v.QualityId })
                        .FirstOrDefault(g => g.Count() > 1);

                    if (dup != null)
                    {
                        ModelState.AddModelError(
                            $"Containers[{cIdx}]",
                            $"Hay filas duplicadas (Tipo:{dup.Key.ValueType}, Denom:{dup.Key.DenominationId}, Calidad:{dup.Key.QualityId})."
                        );
                    }
                }
            }

            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (!ModelState.IsValid)
            {
                if (isAjax)
                {
                    var errorDict = ModelState
                        .Where(kvp => kvp.Value.Errors.Any())
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        );

                    return Json(ServiceResult.FailureResult("Hay errores en el formulario.", errors: errorDict));
                }

                var pageVm = await _cefContainerService.PrepareProcessContainersPageAsync(transactionId);
                pageVm.Containers = viewModel.Containers ?? new List<CefContainerProcessingViewModel>();
                return View(pageVm);
            }

            try
            {
                // isAjax ya lo calculas arriba
                using (var tx = await _context.Database.BeginTransactionAsync())
                {
                    // 1) Mapa de índice de la vista -> ID real en BD de bolsas
                    var bagIndexToId = new Dictionary<int, int>();

                    // 2) Guardar primero BOLSAS (ParentContainerId == null)
                    for (int idx = 0; idx < viewModel.Containers.Count; idx++)
                    {
                        var containerVm = viewModel.Containers[idx];
                        if (containerVm == null) continue;

                        var esBolsa = containerVm.ContainerType == CefContainerTypeEnum.Bolsa;
                        var esSobre = containerVm.ContainerType == CefContainerTypeEnum.Sobre;

                        if (esBolsa)
                        {
                            // Asegura que las bolsas no tengan padre
                            containerVm.ParentContainerId = null;
                            var savedBag = await _cefContainerService.SaveContainerAndDetailsAsync(containerVm, currentUser.Id);
                            bagIndexToId[idx] = savedBag.Id;
                        }
                    }

                    // 3) Guardar luego SOBRES (ParentContainerId es un ÍNDICE de bolsa en la vista)
                    for (int idx = 0; idx < viewModel.Containers.Count; idx++)
                    {
                        var containerVm = viewModel.Containers[idx];
                        if (containerVm == null) continue;

                        var esSobre = containerVm.ContainerType == CefContainerTypeEnum.Sobre;
                        if (!esSobre) continue;

                        if (containerVm.ParentContainerId == null)
                            throw new InvalidOperationException("Los sobres deben tener asignada una bolsa padre.");

                        var parentIndex = containerVm.ParentContainerId.Value;

                        // Mapea el índice de la vista al ID real de la bolsa recién guardada
                        if (!bagIndexToId.TryGetValue(parentIndex, out var realParentId))
                            throw new InvalidOperationException($"No se encontró la bolsa padre para el sobre (índice {parentIndex}).");

                        containerVm.ParentContainerId = realParentId;

                        var savedEnv = await _cefContainerService.SaveContainerAndDetailsAsync(containerVm, currentUser.Id);

                        // Novedades (incidents) de sobre
                        if (containerVm.Incidents != null)
                        {
                            foreach (var inc in containerVm.Incidents)
                            {
                                inc.CefContainerId = savedEnv.Id;
                                await _cefIncidentService.RegisterIncidentAsync(inc, currentUser.Id);
                            }
                        }
                    }

                    await tx.CommitAsync();
                }

                if (isAjax)
                {
                    var redirectUrl = Url.Action(nameof(ProcessContainers), new { transactionId });
                    return Json(ServiceResult.SuccessResult("Contenedores guardados correctamente.", new { redirectUrl }));
                }

                TempData["SuccessMessage"] = "Contenedores guardados correctamente.";
                return RedirectToAction(nameof(ProcessContainers), new { transactionId });
            }
            catch (InvalidOperationException ex)
            {
                if (isAjax)
                    return Json(ServiceResult.FailureResult(ex.Message));

                ModelState.AddModelError("", ex.Message);
                var pageVm = await _cefContainerService.PrepareProcessContainersPageAsync(transactionId);
                pageVm.Containers = viewModel.Containers ?? new List<CefContainerProcessingViewModel>();
                return View(pageVm);
            }
            catch (Exception)
            {
                if (isAjax)
                    return Json(ServiceResult.FailureResult("Ocurrió un error inesperado al guardar los contenedores."));

                ModelState.AddModelError("", "Ocurrió un error inesperado al guardar los contenedores.");
                var pageVm = await _cefContainerService.PrepareProcessContainersPageAsync(transactionId);
                pageVm.Containers = viewModel.Containers ?? new List<CefContainerProcessingViewModel>();
                return View(pageVm);
            }
        }

        /// <summary>
        /// Finaliza el conteo de todos los contenedores de una transacción y la prepara para revisión.
        /// </summary>
        /// <remarks>
        /// Requiere permiso 'Edit' para el módulo "CEF".
        /// </remarks>
        /// <param name="transactionId">ID de la transacción a finalizar.</param>
        /// <returns>Redirección al dashboard de revisión.</returns>
        [HttpPost("FinalizeCounting/{transactionId}")]
        [RequiredPermission(PermissionType.Edit, "CEF")] // Solo supervisores pueden finalizar y enviar a revisión
        public async Task<IActionResult> FinalizeCounting(int transactionId)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            try
            {
                var containers = await _cefContainerService.GetContainersByTransactionIdAsync(transactionId);
                if (!containers.Any())
                {
                    throw new InvalidOperationException("No se han registrado contenedores para esta transacción. No se puede finalizar el conteo.");
                }
                if (containers.Any(c => c.ContainerStatus == CefContainerStatusEnum.InProcess.ToString() || c.ContainerStatus == CefContainerStatusEnum.Pending.ToString()))
                {
                    throw new InvalidOperationException("No todos los contenedores han sido procesados. Por favor, complete el conteo de todos los contenedores.");
                }

                var success = await _cefTransactionService.UpdateTransactionStatusAsync(transactionId, CefTransactionStatusEnum.PendingReview, currentUser.Id); // <-- CORREGIDO AQUÍ

                if (success)
                {
                    TempData["SuccessMessage"] = $"Transacción {transactionId} finalizada para conteo y enviada a revisión.";
                    _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Transacción CEF finalizada y enviada a revisión | ID Transacción: {TransactionId}.",
                        currentUser.UserName, IpAddressForLogging, transactionId);

                    return RedirectToAction(nameof(ReviewTransaction), new { transactionId = transactionId });
                }
                else
                {
                    TempData["ErrorMessage"] = "No se pudo finalizar el conteo de la transacción. Verifique el estado.";
                    _logger.LogWarning("Usuario: {Usuario} | IP: {IP} | Acción: Fallo al finalizar conteo | ID Transacción: {TransactionId}.", currentUser.UserName, IpAddressForLogging, transactionId);
                    return RedirectToAction(nameof(ProcessContainers), new { transactionId = transactionId });
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                _logger.LogError(ex, "Usuario: {Usuario} | IP: {IP} | Acción: Error al finalizar conteo | Mensaje: {ErrorMessage}.", currentUser.UserName, IpAddressForLogging, ex.Message);
                return RedirectToAction(nameof(ProcessContainers), new { transactionId = transactionId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al finalizar el conteo.";
                _logger.LogError(ex, "Usuario: {Usuario} | IP: {IP} | Acción: Error inesperado al finalizar conteo.", currentUser.UserName, IpAddressForLogging);
                return RedirectToAction(nameof(ProcessContainers), new { transactionId = transactionId });
            }
        }

        /// <summary>
        /// Muestra la vista de revisión final de una transacción de Centro de Efectivo.
        /// Permite al supervisor aprobar o rechazar la planilla.
        /// </summary>
        /// <remarks>
        /// Requiere permiso 'View' para el módulo "CEF" y 'Edit' para aprobar/rechazar.
        /// </remarks>
        /// <param name="transactionId">ID de la transacción a revisar.</param>
        /// <returns>Vista de revisión de transacción.</returns>
        [HttpGet("Review/{transactionId}")]
        [RequiredPermission(PermissionType.View, "CEF")] // Los supervisores pueden ver la revisión
        public async Task<IActionResult> ReviewTransaction(int transactionId)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToAction("Login", "Account");

            await SetCommonViewBagsCefAsync(currentUser, "Revisión CEF");

            var viewModel = await _cefTransactionService.PrepareReviewViewModelAsync(transactionId);
            if (viewModel == null)
            {
                TempData["ErrorMessage"] = "Transacción de Centro de Efectivo no encontrada para revisión.";
                return RedirectToAction(nameof(Index));
            }

            if (viewModel.CurrentStatus != CefTransactionStatusEnum.PendingReview)
            {
                TempData["ErrorMessage"] = $"La transacción {viewModel.SlipNumber} no está en estado 'Pendiente de Revisión'. Estado actual: {viewModel.CurrentStatus.ToString().Replace("_", " ")}.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.CanReview = await HasPermisionForView(await _userManager.GetRolesAsync(currentUser), "CEF", PermissionType.Edit);

            _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Acceso a revisión de Transacción | ID Transacción: {TransactionId}.", currentUser.UserName, IpAddressForLogging, transactionId);
            return View(viewModel);
        }

        /// <summary>
        /// Procesa la aprobación o rechazo final de una transacción de Centro de Efectivo.
        /// </summary>
        /// <remarks>
        /// Requiere permiso 'Edit' para el módulo "CEF".
        /// </remarks>
        /// <param name="viewModel">ViewModel con el ID de la transacción y el nuevo estado (Aprobada/Rechazada).</param>
        /// <returns>Redirección al dashboard de CEF.</returns>
        [HttpPost("Review")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "CEF")]
        public async Task<IActionResult> ReviewTransaction(CefTransactionReviewViewModel viewModel)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            await SetCommonViewBagsCefAsync(currentUser, "Procesando Revisión CEF");

            viewModel.AvailableStatuses = new List<SelectListItem>
            {
                new (CefTransactionStatusEnum.Approved.ToString(), "Aprobada"),
                new (CefTransactionStatusEnum.Rejected.ToString(), "Rechazada")
            };
            ViewBag.CanReview = await HasPermisionForView(await _userManager.GetRolesAsync(currentUser), "CEF", PermissionType.Edit);


            if (!ModelState.IsValid)
            {
                var fieldErrors = ModelState.Where(kvp => kvp.Value.Errors.Any()).ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                _logger.LogWarning("Usuario: {Usuario} | IP: {IP} | Acción: Revisión de Transacción - Modelo Inválido | Errores: {@Errores} |",
                    currentUser.UserName, IpAddressForLogging, fieldErrors);

                var currentViewModelData = await _cefTransactionService.PrepareReviewViewModelAsync(viewModel.Id);
                if (currentViewModelData != null)
                {
                    viewModel.ServiceOrderId = currentViewModelData.ServiceOrderId;
                    viewModel.SlipNumber = currentViewModelData.SlipNumber;
                    viewModel.TransactionType = currentViewModelData.TransactionType;
                    viewModel.Currency = currentViewModelData.Currency;
                    viewModel.TotalDeclaredValue = currentViewModelData.TotalDeclaredValue;
                    viewModel.TotalCountedValue = currentViewModelData.TotalCountedValue;
                    viewModel.ValueDifference = currentViewModelData.ValueDifference;
                    viewModel.CurrentStatus = currentViewModelData.CurrentStatus;
                    viewModel.ReviewerUserName = currentViewModelData.ReviewerUserName;
                    viewModel.ReviewDate = currentViewModelData.ReviewDate;
                    viewModel.ContainerSummaries = currentViewModelData.ContainerSummaries;
                    viewModel.IncidentSummaries = currentViewModelData.IncidentSummaries;
                }
                return View(viewModel);
            }

            try
            {
                var success = await _cefTransactionService.ProcessReviewApprovalAsync(viewModel, currentUser.Id);
                if (success)
                {
                    TempData["SuccessMessage"] = $"Transacción {viewModel.SlipNumber} {viewModel.NewStatus.ToString().Replace("_", " ")} exitosamente.";
                    _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Transacción CEF {Status} | ID Transacción: {TransactionId}.",
                        currentUser.UserName, IpAddressForLogging, viewModel.NewStatus, viewModel.Id);
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "No se pudo procesar la revisión de la transacción. Verifique el estado.");
                    _logger.LogWarning("Usuario: {Usuario} | IP: {IP} | Acción: Fallo al procesar revisión de Transacción | ID Transacción: {TransactionId}.", currentUser.UserName, IpAddressForLogging, viewModel.Id);

                    var currentViewModelData = await _cefTransactionService.PrepareReviewViewModelAsync(viewModel.Id);
                    if (currentViewModelData != null)
                    {
                        viewModel.ContainerSummaries = currentViewModelData.ContainerSummaries;
                        viewModel.IncidentSummaries = currentViewModelData.IncidentSummaries;
                        viewModel.TotalDeclaredValue = currentViewModelData.TotalDeclaredValue;
                        viewModel.TotalCountedValue = currentViewModelData.TotalCountedValue;
                        viewModel.ValueDifference = currentViewModelData.ValueDifference;
                        viewModel.TransactionType = currentViewModelData.TransactionType;
                        viewModel.Currency = currentViewModelData.Currency;
                        viewModel.SlipNumber = currentViewModelData.SlipNumber;
                        viewModel.ServiceOrderId = currentViewModelData.ServiceOrderId;
                        viewModel.CurrentStatus = currentViewModelData.CurrentStatus;
                        viewModel.ReviewerUserName = currentViewModelData.ReviewerUserName;
                        viewModel.ReviewDate = currentViewModelData.ReviewDate;
                    }

                    return View(viewModel);
                }
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                _logger.LogError(ex, "Usuario: {Usuario} | IP: {IP} | Acción: Error de operación inválida al revisar Transacción | Mensaje: {ErrorMessage}.", currentUser.UserName, IpAddressForLogging, ex.Message);

                // --- INICIO DE LA CORRECCIÓN EN EL CATCH (para InvalidOperationException) ---
                var currentViewModelData = await _cefTransactionService.PrepareReviewViewModelAsync(viewModel.Id);
                if (currentViewModelData != null)
                {
                    viewModel.ContainerSummaries = currentViewModelData.ContainerSummaries;
                    viewModel.IncidentSummaries = currentViewModelData.IncidentSummaries;
                    viewModel.TotalDeclaredValue = currentViewModelData.TotalDeclaredValue;
                    viewModel.TotalCountedValue = currentViewModelData.TotalCountedValue;
                    viewModel.ValueDifference = currentViewModelData.ValueDifference;
                    viewModel.TransactionType = currentViewModelData.TransactionType;
                    viewModel.Currency = currentViewModelData.Currency;
                    viewModel.SlipNumber = currentViewModelData.SlipNumber;
                    viewModel.ServiceOrderId = currentViewModelData.ServiceOrderId;
                    viewModel.CurrentStatus = currentViewModelData.CurrentStatus;
                    viewModel.ReviewerUserName = currentViewModelData.ReviewerUserName;
                    viewModel.ReviewDate = currentViewModelData.ReviewDate;
                }
                // --- FIN DE LA CORRECCIÓN ---
                return View(viewModel);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ocurrió un error inesperado al revisar la transacción.");
                _logger.LogError(ex, "Usuario: {Usuario} | IP: {IP} | Acción: Error inesperado al revisar Transacción.", currentUser.UserName, IpAddressForLogging);

                // --- INICIO DE LA CORRECCIÓN EN EL CATCH (para Excepción general) ---
                var currentViewModelData = await _cefTransactionService.PrepareReviewViewModelAsync(viewModel.Id);
                if (currentViewModelData != null)
                {
                    viewModel.ContainerSummaries = currentViewModelData.ContainerSummaries;
                    viewModel.IncidentSummaries = currentViewModelData.IncidentSummaries;
                    viewModel.TotalDeclaredValue = currentViewModelData.TotalDeclaredValue;
                    viewModel.TotalCountedValue = currentViewModelData.TotalCountedValue;
                    viewModel.ValueDifference = currentViewModelData.ValueDifference;
                    viewModel.TransactionType = currentViewModelData.TransactionType;
                    viewModel.Currency = currentViewModelData.Currency;
                    viewModel.SlipNumber = currentViewModelData.SlipNumber;
                    viewModel.ServiceOrderId = currentViewModelData.ServiceOrderId;
                    viewModel.CurrentStatus = currentViewModelData.CurrentStatus;
                    viewModel.ReviewerUserName = currentViewModelData.ReviewerUserName;
                    viewModel.ReviewDate = currentViewModelData.ReviewDate;
                }
                // --- FIN DE LA CORRECCIÓN ---
                return View(viewModel);
            }
        }

        // --- NUEVAS ACCIONES AJAX PARA DROPDOWNS DINÁMICOS (DEL TEMPORAL) ---
        /// <summary>
        /// Obtiene una lista de ubicaciones (puntos, ATMs o fondos) para dropdowns dinámicos.
        /// </summary>
        /// <param name="clientId">Código del cliente.</param>
        /// <param name="branchId">Código de la sucursal (opcional).</param>
        /// <param name="locationType">Tipo de ubicación como cadena ("Point", "ATM" o "Fund").</param>
        /// <returns>JSON con lista de SelectListItem.</returns>
        [HttpGet("GetPointsOrFundsForDropdown")]
        public async Task<IActionResult> GetPointsOrFundsForDropdown(int clientId, int? branchId, string locationType, string? serviceConceptCode)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            if (!Enum.TryParse(locationType, out LocationTypeEnum typeEnum))
            {
                return BadRequest("Tipo de ubicación no válido. Use: Point, ATM o Fund");
            }

            var items = await _cefServiceCreationService.GetLocationsForDropdownAsync(clientId, branchId, typeEnum, serviceConceptCode);
            return Json(items);
        }

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

            var details = await _cefServiceCreationService.GetLocationDetailsByCodeAsync(code, clientId, isPoint);

            if (details == null)
            {
                return Json(ServiceResult.FailureResult("Detalles de ubicación no encontrados."));
            }

            return Json(ServiceResult.SuccessResult("Detalles obtenidos.", details));
        }

        /// <summary>
        /// Obtiene usuarios responsables de entrega o recepción según concepto y sucursal.
        /// </summary>
        /// <param name="branchId">ID de la sucursal seleccionada.</param>
        /// <param name="serviceConceptCode">Código del concepto del servicio.</param>
        /// <param name="isDelivery">True para lista de entrega, false para recepción.</param>
        /// <returns>JSON con lista de SelectListItem.</returns>
        [HttpGet("GetResponsibleUsers")]
        public async Task<IActionResult> GetResponsibleUsers(int branchId, string serviceConceptCode, bool isDelivery)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            var items = await _cefServiceCreationService.GetResponsibleUsersForDropdownAsync(branchId, serviceConceptCode, isDelivery, currentUser.Id);
            return Json(items);
        }

        /// <summary>
        /// Obtiene empleados (JT, Conductor, Tripulante) filtrados por sucursal y cargo.
        /// </summary>
        /// <param name="branchId">ID de la sucursal.</param>
        /// <param name="positionId">ID del cargo a filtrar (ej. 64 para JT).</param>
        /// <returns>JSON con lista de SelectListItem.</returns>
        [HttpGet("GetEmployeesForDropdown")]
        public async Task<IActionResult> GetEmployeesForDropdown(int branchId, int positionId)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            var employees = await _cefServiceCreationService.GetEmployeesForDropdownAsync(branchId, positionId);
            return Json(employees);
        }

        [HttpGet]
        public IActionResult GetClientName(int clientId)
        {
            var client = _context.AdmClientes.FirstOrDefault(c => c.ClientCode == clientId);
            if (client != null)
            {
                return Json(new { clientName = client.ClientName });
            }

            return NotFound();
        }

        [HttpGet("AmountInWords")]
        public IActionResult AmountInWords(decimal value, string currency = "COP")
        {
            var words = AmountInWordsHelper.ToSpanishCurrency(value, currency);
            return Json(new { words });
        }
    }
}