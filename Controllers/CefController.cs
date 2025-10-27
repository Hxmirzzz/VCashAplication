using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Extensions;
using VCashApp.Filters;
using VCashApp.Models;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.CentroEfectivo.Shared;
using VCashApp.Models.ViewModels.CentroEfectivo.Provision;
using VCashApp.Services;
using VCashApp.Services.Cef;
using VCashApp.Services.DTOs;
using VCashApp.Services.Logging;
using VCashApp.Utils;
using VCashApp.Infrastructure.Branches;

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
        private readonly IAuditLogger _audit;
        private readonly ILogger<CefController> _logger;
        private readonly IBranchContext _branchContext;

        /// <summary>
        /// Constructor del controlador CefController.
        /// </summary>
        /// <param name="cefTransactionService">Servicio para la lógica de transacciones CEF.</param>
        /// <param name="cefContainerService">Servicio para la lógica de contenedores CEF.</param>
        /// <param name="cefIncidentService">Servicio para la lógica de novedades CEF.</param>
        /// <param name="context">Contexto de la base de datos de la aplicación.</param>
        /// <param name="userManager">Administrador de usuarios de Identity.</param>
        /// <param name="logger">Servicio de logging para el controlador.</param>
        /// <param name="branchContext">Contexto de sucursal actual.</param>
        public CefController(
            ICefTransactionService cefTransactionService,
            ICefContainerService cefContainerService,
            ICefIncidentService cefIncidentService,
            ICefServiceCreationService cefServiceCreationService,
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IAuditLogger audit,
            ILogger<CefController> logger,
            IBranchContext branchContext)
            : base(context, userManager)
        {
            _cefTransactionService = cefTransactionService;
            _cefContainerService = cefContainerService;
            _cefIncidentService = cefIncidentService;
            _cefServiceCreationService = cefServiceCreationService; //TEMPORAL
            _audit = audit;
            _logger = logger;
            _branchContext = branchContext;
        }

        /// <summary>
        /// Método auxiliar para configurar ViewBags comunes específicos de CEF.
        /// Hereda y extiende el SetCommonViewBagsBaseAsync del BaseController.
        /// </summary>
        /// <param name="currentUser">El usuario actual autenticado.</param>
        /// <param name="pageName">El nombre de la página para el ViewBag.</param>
        /// <returns>Tarea asíncrona completada.</returns>
        private async Task SetCommonViewBagsCefAsync(ApplicationUser currentUser, string pageName, params string[] codVistas)
        {
            await base.SetCommonViewBagsBaseAsync(currentUser, pageName);
            bool isAdmin = ViewBag.IsAdmin;

            var (sucursales, estados) = await _cefTransactionService.GetDropdownListsAsync(currentUser.Id, isAdmin);
            ViewBag.AvailableBranches = sucursales;
            ViewBag.TransactionStatuses = estados;

            var vistas = (codVistas != null && codVistas.Length > 0)
                ? codVistas
                : new[] { "CEF_REC", "CEF_DEL", "CEF_COL", "CEF_SUP" };

            var userRoles = await _userManager.GetRolesAsync(currentUser);

            async Task<bool> HasAsync(PermissionType p)
            {
                foreach (var v in vistas)
                {
                    if (await HasPermisionForView(userRoles, v, p))
                        return true;
                }
                return false;
            }

            ViewBag.HasCreate = await HasAsync(PermissionType.Create);
            ViewBag.HasEdit = await HasAsync(PermissionType.Edit);
            ViewBag.HasView = await HasAsync(PermissionType.View);
            // ViewBag.HasDeletePermission = await HasPermisionForView(userRoles, "CEF", PermissionType.Delete);
        }

        private static string PageTitleForMode(CefDashboardMode? mode) => mode switch
        {
            CefDashboardMode.TesoreriaRecepcion => "Tesoreria - Recepción",
            CefDashboardMode.TesoreriaEntrega => "Tesoreria - Entrega",
            CefDashboardMode.CefRecoleccion => "CEF - Recolecciones",
            CefDashboardMode.CefProvision => "CEF - Provisiones",
            _ => "Centro de Efectivo"
        };

        private static string ViewForMode(CefDashboardMode? mode) => mode switch
        {
            CefDashboardMode.TesoreriaRecepcion => "Reception",
            CefDashboardMode.TesoreriaEntrega => "Delivery",
            CefDashboardMode.CefRecoleccion => "Collections",
            CefDashboardMode.CefProvision => "Supplies",
            _ => "Index"
        };

        // =========== SECCION: TESORERIA OPERATIVA ===========
        /// <summary>
        /// Dashboard de Tesorería - Recepción (filtra por estado RegistroTesoreria).
        /// Requiere permiso 'View' en TESORERIA.
        /// </summary>
        [HttpGet("Reception")]
        [RequiredPermission(PermissionType.View, "CEF_REC")]
        public Task<IActionResult> Reception(
            int? branchId, DateOnly? startDate, DateOnly? endDate,
            string? search, int page = 1, int pageSize = 15)
        {
            return Index(
                branchId, startDate, endDate,
                CefTransactionStatusEnum.RegistroTesoreria, // estado focal
                search, page, pageSize,
                CefDashboardMode.TesoreriaRecepcion         // MODO
            );
        }

        /// <summary>
        /// Dashboard de Tesorería - Entrega (filtra por estado ListoEntrega).
        /// Requiere permiso 'View' en TESORERIA.
        /// </summary>
        [HttpGet("Delivery")]
        [RequiredPermission(PermissionType.View, "CEF_DEL")]
        public Task<IActionResult> Delivery(
            int? branchId, DateOnly? startDate, DateOnly? endDate,
            string? search, int page = 1, int pageSize = 15)
        {
            return Index(
                branchId, startDate, endDate,
                CefTransactionStatusEnum.ListoParaEntrega,
                search, page, pageSize,
                CefDashboardMode.TesoreriaEntrega
            );
        }

        // =========== SECCION: CENTRO DE EFECTIVO ============
        /// <summary>
        /// Dashboard CEF - Recolección (muestra transacciones de tipo recolección).
        /// Requiere permiso 'View' en CEF.
        /// </summary>
        [HttpGet("Collections")]
        [RequiredPermission(PermissionType.View, "CEF_COL")]
        public Task<IActionResult> Collections(
            int? branchId, DateOnly? startDate, DateOnly? endDate,
            string? search, int page = 1, int pageSize = 15)
        {
            return Index(branchId, startDate, endDate, null, search, page, pageSize, CefDashboardMode.CefRecoleccion);
        }

        /// <summary>
        /// Dashboard CEF - Provisión (muestra transacciones de tipo provisión).
        /// Requiere permiso 'View' en CEF.
        /// </summary>
        [HttpGet("Supplies")]
        [RequiredPermission(PermissionType.View, "CEF_SUP")]
        public Task<IActionResult> Supplies(
            int? branchId, DateOnly? startDate, DateOnly? endDate,
            string? search, int page = 1, int pageSize = 15)
        {
            // Igual que arriba, sin status fijo (hasta ajustar SP)
            return Index(branchId, startDate, endDate, null, search, page, pageSize, CefDashboardMode.CefProvision);
        }

        static string[]? MapCodesByMode(CefDashboardMode? mode) => mode switch
        {
            CefDashboardMode.CefRecoleccion => new[] { "RC", "ET" },
            CefDashboardMode.CefProvision => new[] { "PV", "PR" },
            CefDashboardMode.TesoreriaRecepcion => new[] { "RC" },
            _ => null
        };

        static string[] ExcludedStatusesByMode(CefDashboardMode? mode) => mode switch
        {
            CefDashboardMode.CefRecoleccion => new[] { nameof(CefTransactionStatusEnum.RegistroTesoreria) },
            CefDashboardMode.CefProvision => new[] { nameof(CefTransactionStatusEnum.RegistroTesoreria) },
            _ => Array.Empty<string>()
        };

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
        [RequiredPermission(PermissionType.View, "CEF_REC", "CEF_DEL", "CEF_COL", "CEF_SUP")]
        public async Task<IActionResult> Index(
            int? branchId, DateOnly? startDate, DateOnly? endDate, CefTransactionStatusEnum? status,
            string? search, int page = 1, int pageSize = 15, CefDashboardMode? mode = null)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            int? effectiveBranch = branchId ?? _branchContext.CurrentBranchId;
            var effectiveMode = mode ?? InferModeFromStatus(status);
            var pageTitle = PageTitleForMode(effectiveMode);
            var codVista = effectiveMode switch
            {
                CefDashboardMode.TesoreriaRecepcion => "CEF_REC",
                CefDashboardMode.TesoreriaEntrega => "CEF_DEL",
                CefDashboardMode.CefRecoleccion => "CEF_COL",
                CefDashboardMode.CefProvision => "CEF_SUP",
                _ => "CEF_REC"
            };

            await SetCommonViewBagsCefAsync(currentUser, pageTitle, codVista);

            ViewBag.ShowDeliverActions = (effectiveMode == CefDashboardMode.TesoreriaEntrega);
            ViewData["Title"] = pageTitle;
            ViewData["Mode"] = effectiveMode;

            bool isAdmin = ViewBag.IsAdmin;
            var conceptTypeCodes = MapCodesByMode(effectiveMode);
            var excludedStatuses = ExcludedStatusesByMode(effectiveMode);

            var (transactions, totalRecords) = await _cefTransactionService.GetFilteredCefTransactionsAsync(
                currentUser.Id, branchId, startDate, endDate, status, search, page, pageSize, isAdmin, conceptTypeCodes, excludedStatuses);

            var transactionStatuses = BuildStatusListByMode(mode, status);
            var branches = (ViewBag.AvailableBranches as List<SelectListItem>) ?? new List<SelectListItem>();
            if (effectiveBranch.HasValue)
            {
                foreach (var it in branches)
                    it.Selected = (it.Value == effectiveBranch.Value.ToString());
            }

            var vm = new CefDashboardViewModel
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
                AvailableBranches = branches,
                TransactionStatuses = transactionStatuses,
                Mode = mode ?? InferModeFromStatus(status)
            };

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = vm.TotalPages;
            ViewBag.TotalData = totalRecords;
            ViewBag.TransactionStatuses = transactionStatuses;

            _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Acceso a Dashboard CEF | Conteo: {Conteo} |",
                currentUser.UserName, IpAddressForLogging, transactions.Count());

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                ViewData["Mode"] = mode ?? InferModeFromStatus(status);
                return PartialView("_TransactionTablePartial", transactions);
            }

            _audit.Info(
                action: "CEF.Dashboard",
                detailMessage: "Acceso a dashboard CEF",
                result: "OK",
                entityType: "Dashboard",
                entityId: null,
                extra: new Dictionary<string, object>
                {
                    ["Mode"] = (ViewData["Mode"] ?? "").ToString(),
                    ["BranchId"] = branchId,
                    ["Status"] = status?.ToString(),
                    ["Search"] = search ?? "",
                    ["Page"] = page,
                    ["PageSize"] = pageSize,
                    ["TotalRecords"] = totalRecords
                }
            );

            return View("Index", vm);
        }

        /// <summary>
        /// Devuelve la lista de estados sugeridos según el modo y marca el seleccionado actual.
        /// </summary>
        [NonAction]
        private static List<SelectListItem> BuildStatusListByMode(CefDashboardMode? mode, CefTransactionStatusEnum? selected)
        {
            var recep = new[] { "RegistroTesoreria", "EncoladoParaConteo", "Conteo", "PendienteRevision" };
            var entrg = new[] { "ListoParaEntrega", "Aprobado" };

            IEnumerable<string> set = mode switch
            {
                CefDashboardMode.TesoreriaRecepcion => recep,
                CefDashboardMode.TesoreriaEntrega => entrg,
                _ => Enum.GetNames(typeof(CefTransactionStatusEnum))
            };

            var list = set.Select(s => new SelectListItem
            {
                Value = s,
                Text = s.Replace("_", " "),
                Selected = selected.HasValue && s == selected.Value.ToString()
            }).ToList();

            // placeholder al inicio
            list.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "-- Seleccionar Estado --",
                Selected = !selected.HasValue
            });

            return list;
        }

        /// <summary>
        /// Si alguien entra a /Cef/Index con un status fijo, inferimos un modo para la UI.
        /// </summary>
        [NonAction]
        private static CefDashboardMode InferModeFromStatus(CefTransactionStatusEnum? status)
        {
            if (status == CefTransactionStatusEnum.RegistroTesoreria) return CefDashboardMode.TesoreriaRecepcion;
            if (status == CefTransactionStatusEnum.ListoParaEntrega) return CefDashboardMode.TesoreriaEntrega;
            // por defecto mostramos CEF general (puedes cambiar a Provision si prefieres)
            return CefDashboardMode.CefRecoleccion;
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
        [RequiredPermission(PermissionType.Create, "CEF_REC")]
        public async Task<IActionResult> Checkin(string? serviceOrderId, string? routeId)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsCefAsync(currentUser, "Check-in CEF", "CEF_REC");

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
                    _audit.Warn(
                        action: "CEF.Checkin.Open",
                        detailMessage: ex.Message,
                        result: "InvalidOperation",
                        entityType: "CgsServicio",
                        entityId: serviceOrderId
                    );
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

            _audit.Info(
                action: "CEF.Checkin.Open",
                detailMessage: "Apertura de formulario de Check-in",
                result: "OK",
                entityType: "CefTransaction",
                entityId: null,
                urlId: serviceOrderId
            );

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
        [RequiredPermission(PermissionType.Create, "CEF_REC")]
        public async Task<IActionResult> Checkin(CefTransactionCheckinViewModel viewModel)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            await SetCommonViewBagsCefAsync(currentUser, "Procesando Check-in CEF", "CEF_REC");

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

                _audit.Info(
                    action: "CEF.Checkin",
                    entityType: "CefTransaction",
                    entityId: newTransaction.Id.ToString(),
                    detailMessage: "Check-in registrado",
                    result: "OK",
                    urlId: newTransaction.ServiceOrderId,
                    extra: new Dictionary<string, object>
                    {
                        ["SlipNumber"] = newTransaction.SlipNumber,
                        ["ServiceOrderId"] = newTransaction.ServiceOrderId,
                        ["Currency"] = newTransaction.Currency ?? "N/A",
                        ["DeclaredTotal"] = newTransaction.TotalDeclaredValue
                    }
                );

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

                _audit.Warn(
                    action: "CEF.Checkin",
                    detailMessage: ex.Message,
                    result: "InvalidOperation",
                    entityType: "CefTransaction",
                    entityId: null
                );

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

                _audit.Error(
                    action: "CEF.Checkin",
                    ex: ex,
                    detailMessage: "Error inesperado en Check-in",
                    entityType: "CefTransaction",
                    entityId: null
                );

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
        [RequiredPermission(PermissionType.Create, "CEF_REC", "CEF_DEL", "CEF_COL", "CEF_SUP")]
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
        [RequiredPermission(PermissionType.Create, "CEF_REC", "CEF_DEL", "CEF_COL", "CEF_SUP")]
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
        /// Muestra el formulario para procesar los contenedores de una transacción CEF.
        /// </summary>
        /// <param name="transactionId">Identificador de la transacción.</param>
        /// <param name="containerId">Identificador de la bolsa.</param>
        /// <returns>Vista del formulario de procesamiento de contenedores.</returns>
        [HttpGet("ProcessContainers/{transactionId}")]
        [RequiredPermission(PermissionType.Edit, "CEF_REC", "CEF_COL")]
        public async Task<IActionResult> ProcessContainers(int transactionId, int? containerId = null)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsCefAsync(currentUser, "Procesar Contenedores CEF", "CEF_REC", "CEF_COL");

            var caps = await GetCefCapsAsync(currentUser);
            ViewBag.CanCountBills = caps.CanCountBills;
            ViewBag.CanCountCoins = caps.CanCountCoins;
            ViewBag.CanIncCreateEdit = caps.CanIncCreateEdit;
            ViewBag.CanIncApprove = caps.CanIncApprove;
            ViewBag.CanFinalize = caps.CanFinalize;

            await _cefTransactionService.RecalcTotalsAndNetDiffAsync(transactionId);
            var pageVm = await _cefContainerService.PrepareProcessContainersPageAsync(transactionId);

            var pointCaps = await _cefContainerService.GetPointCapsAsync(pageVm.Service.ServiceOrderId);
            ViewBag.PointCapsJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                sobres = pointCaps.sobres,
                documentos = pointCaps.documentos,
                cheques = pointCaps.cheques
            }, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });

            ViewBag.IncidentTypesForEdit = (await _cefIncidentService.GetAllIncidentTypesAsync())
                .Select(it => new SelectListItem { Value = it.Id.ToString(), Text = it.Description })
                .ToList();

            ViewBag.IncidentTypes = (await _cefIncidentService.GetAllIncidentTypesAsync())
                .Select(it => new SelectListItem { Value = (it.Code ?? "").Trim(), Text = it.Description })
                .ToList();

            ViewBag.DenomsJson = await _cefContainerService.BuildDenomsJsonForTransactionAsync(transactionId);
            ViewBag.QualitiesJson = await _cefContainerService.BuildQualitiesJsonAsync();
            ViewBag.BanksJson = await _cefContainerService.BuildBankEntitiesJsonAsync();

            ViewData["Title"] = "Procesamiento de Bolsas";

            _audit.Info(
                action: "CEF.Containers.Open",
                detailMessage: "Apertura de pantalla de procesamiento de contenedores",
                result: "OK",
                entityType: "CefTransaction",
                entityId: transactionId.ToString(),
                urlId: pageVm.Service?.ServiceOrderId
            );

            return View(pageVm);
        }

        /// <summary>
        /// Elimina un contenedor específico de una transacción CEF.
        /// </summary>
        /// <param name="transactionId">Id de la transaccion</param>
        /// <param name="containerId">Id del contenedor</param>
        /// <returns>Eliminar un contenedor seleccionado</returns>
        [HttpPost("DeleteContainer")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "CEF_REC", "CEF_COL")]
        public async Task<IActionResult> DeleteContainer(int transactionId, int containerId)
        {
            try
            {
                var ok = await _cefContainerService.DeleteContainerAsync(transactionId, containerId);
                if (ok)
                    return Json(new { success = true });

                return Json(new { success = false, message = "No se pudo eliminar el contenedor. Verifique que no tenga datos asociados." });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Ocurrió un error inesperado al eliminar el contenedor." });
            }

        }


        /// <summary>
        /// Procesa el envío del formulario de procesamiento de contenedores (guardar detalles y novedades).
        /// </summary>
        /// <remarks>
        /// Este endpoint es llamado vía HTTP POST.
        /// Requiere permiso 'Edit' para el módulo "CEF".
        /// </remarks>
        /// <param name="transactionId">Identificador de la transacción.</param>
        /// <param name="viewModel">ViewModel con los datos del contenedor, detalles y novedades.</param>
        /// <returns>Un JSON ServiceResult o redirección a la misma vista para continuar o al dashboard.</returns>
        [HttpPost("ProcessContainers/{transactionId:int}")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "CEF_REC", "CEF_COL")]
        public async Task<IActionResult> ProcessContainers(int transactionId, CefProcessContainersPageViewModel viewModel)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            await SetCommonViewBagsCefAsync(currentUser, "Procesando Contenedores CEF", "CEF_REC", "CEF_COL");

            // === Caps centralizados (mismo criterio que el GET) ===
            var caps = await GetCefCapsAsync(currentUser);
            bool canBills = caps.CanCountBills;
            bool canCoins = caps.CanCountCoins;

            viewModel.CefTransactionId = transactionId;

            // Catálogos mínimos para la vista en caso de volver con errores
            ViewBag.IncidentTypes = (await _cefIncidentService.GetAllIncidentTypesAsync())
                .Select(it => new SelectListItem { Value = it.Code, Text = it.Description }).ToList();
            ViewBag.DenomsJson = await _cefContainerService.BuildDenomsJsonForTransactionAsync(transactionId);
            ViewBag.QualitiesJson = await _cefContainerService.BuildQualitiesJsonAsync();

            // ===== Validaciones mínimas del modelo =====
            if (viewModel?.Containers == null || viewModel.Containers.Count == 0)
                ModelState.AddModelError("", "No se recibieron contenedores para guardar.");

            // ===== Validación de permisos SOLO sobre nuevos/cambiados =====
            if (viewModel?.Containers != null)
            {
                // Pre-carga de detalles existentes por contenedor para poder comparar
                var existingByContainer = new Dictionary<int, List<CefValueDetail>>();
                var existingIds = viewModel.Containers.Select(c => c?.Id ?? 0).Where(id => id > 0).Distinct().ToList();
                if (existingIds.Count > 0)
                {
                    var persisted = await _context.CefContainers
                        .Include(c => c.ValueDetails)
                        .Where(c => existingIds.Contains(c.Id))
                        .ToListAsync();

                    foreach (var c in persisted)
                        existingByContainer[c.Id] = c.ValueDetails?.ToList() ?? new List<CefValueDetail>();
                }

                for (int cIdx = 0; cIdx < viewModel.Containers.Count; cIdx++)
                {
                    var cVm = viewModel.Containers[cIdx];
                    if (cVm?.ValueDetails == null) continue;

                    // 1) Duplicados Denom+Quality (solo Billete/Moneda)
                    var dup = cVm.ValueDetails
                        .Where(v => (v.ValueType == CefValueTypeEnum.Billete || v.ValueType == CefValueTypeEnum.Moneda)
                                 && v.DenominationId != null && v.QualityId != null)
                        .GroupBy(v => new { v.ValueType, v.DenominationId, v.QualityId })
                        .FirstOrDefault(g => g.Select(v => v.Id).Distinct().Count() > 1);

                    if (dup != null)
                    {
                        ModelState.AddModelError(
                            $"Containers[{cIdx}]",
                            $"Hay filas duplicadas (Tipo:{dup.Key.ValueType}, Denom:{dup.Key.DenominationId}, Calidad:{dup.Key.QualityId})."
                        );
                    }

                    // 2) Permisos SOLO si es nuevo o cambió
                    var existing = (cVm.Id > 0 && existingByContainer.TryGetValue(cVm.Id, out var list)) ? list : new List<CefValueDetail>();

                    foreach (var vd in cVm.ValueDetails)
                    {
                        // si no es Billete/Moneda, no entra en esta validación de permisos de conteo
                        bool isBill = vd.ValueType == CefValueTypeEnum.Billete;
                        bool isCoin = vd.ValueType == CefValueTypeEnum.Moneda;

                        if (!isBill && !isCoin) continue;

                        var persisted = (vd.Id > 0) ? existing.FirstOrDefault(x => x.Id == vd.Id) : null;
                        bool isNew = vd.Id == 0;
                        bool isChanged = !isNew && persisted != null && DetailChanged(vd, persisted);

                        if (isNew || isChanged)
                        {
                            if (isBill && !canBills)
                                ModelState.AddModelError("", "No tiene permiso para capturar/editar BILLETES.");
                            if (isCoin && !canCoins)
                                ModelState.AddModelError("", "No tiene permiso para capturar/editar MONEDAS.");
                        }
                    }

                    // Nota: si “solo moneda” elimina filas de billetes existentes, NO lo bloqueamos aquí.
                    // Si quisieras bloquear eliminación de billetes por “solo moneda”, agrega una verificación
                    // de “faltantes” vs existing y marca error.
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

                    _audit.Warn(
                        action: "CEF.Containers.Save",
                        detailMessage: "Modelo inválido al guardar contenedores",
                        result: "ValidationFailed",
                        entityType: "CefTransaction",
                        entityId: transactionId.ToString()
                    );

                    return Json(ServiceResult.FailureResult("Hay errores en el formulario.", errors: errorDict));
                }

                var pageVm = await _cefContainerService.PrepareProcessContainersPageAsync(transactionId);
                pageVm.Containers = viewModel.Containers ?? new List<CefContainerProcessingViewModel>();
                return View(pageVm);
            }

            // ======= Persistencia (sin cambios respecto a tu flujo) =======
            try
            {
                using (var tx = await _context.Database.BeginTransactionAsync())
                {
                    var bagIndexToId = new Dictionary<int, int>();

                    // 1) Bolsas primero
                    for (int idx = 0; idx < viewModel.Containers.Count; idx++)
                    {
                        var containerVm = viewModel.Containers[idx];
                        if (containerVm == null) continue;

                        if (containerVm.ContainerType == CefContainerTypeEnum.Bolsa)
                        {
                            containerVm.ParentContainerId = null;
                            var savedBag = await _cefContainerService.SaveContainerAndDetailsAsync(containerVm, currentUser.Id);
                            bagIndexToId[idx] = savedBag.Id;
                        }
                    }

                    // 2) Luego sobres (resolver ParentContainerId por índice/Id)
                    for (int idx = 0; idx < viewModel.Containers.Count; idx++)
                    {
                        var containerVm = viewModel.Containers[idx];
                        if (containerVm == null || containerVm.ContainerType != CefContainerTypeEnum.Sobre) continue;

                        if (containerVm.ParentContainerId == null)
                            throw new InvalidOperationException("Los sobres deben tener asignada una bolsa padre.");

                        var parentIndex = containerVm.ParentContainerId.Value;

                        if (bagIndexToId.ContainsValue(parentIndex))
                            containerVm.ParentContainerId = parentIndex; // ya venía como Id real
                        else if (bagIndexToId.TryGetValue(parentIndex, out var realParentId))
                            containerVm.ParentContainerId = realParentId; // venía como índice
                        else
                            throw new InvalidOperationException($"No se encontró la bolsa padre para el sobre (índice o ID {parentIndex}).");

                        var savedEnv = await _cefContainerService.SaveContainerAndDetailsAsync(containerVm, currentUser.Id);

                        // Incidentes del sobre
                        if (containerVm.Incidents != null)
                        {
                            foreach (var inc in containerVm.Incidents)
                            {
                                inc.CefContainerId = savedEnv.Id;
                                var created = await _cefIncidentService.RegisterIncidentAsync(inc, currentUser.Id);

                                _audit.Info(
                                    action: "CEF.Incident.Reportada",
                                    entityType: "CefIncident",
                                    entityId: created.Id.ToString(),
                                    result: "Reportada",
                                    urlId: created.CefTransactionId?.ToString(),
                                    modelId: created.CefContainerId?.ToString(),
                                    extra: new Dictionary<string, object>
                                    {
                                        ["IncidentType"] = inc.IncidentType.ToString(),
                                        ["AffectedAmount"] = inc.AffectedAmount != 0 ? inc.AffectedAmount : inc.AffectedDenomination * inc.AffectedQuantity,
                                        ["Description"] = inc.Description ?? ""
                                    }
                                );
                            }
                        }
                    }

                    await tx.CommitAsync();

                    _audit.Info(
                        action: "CEF.Containers.Save",
                        detailMessage: "Contenedores guardados correctamente",
                        result: "OK",
                        entityType: "CefTransaction",
                        entityId: transactionId.ToString(),
                        extra: new Dictionary<string, object>
                        {
                            ["ContainersCount"] = viewModel.Containers?.Count ?? 0
                        }
                    );
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
                if (isAjax) return Json(ServiceResult.FailureResult(ex.Message));

                ModelState.AddModelError("", ex.Message);
                _audit.Warn(
                    action: "CEF.Containers.Save",
                    detailMessage: ex.Message,
                    result: "InvalidOperation",
                    entityType: "CefTransaction",
                    entityId: transactionId.ToString()
                );
                var pageVm = await _cefContainerService.PrepareProcessContainersPageAsync(transactionId);
                pageVm.Containers = viewModel.Containers ?? new List<CefContainerProcessingViewModel>();
                return View(pageVm);
            }
            catch (Exception ex)
            {
                if (isAjax) return Json(ServiceResult.FailureResult("Ocurrió un error inesperado al guardar los contenedores."));

                ModelState.AddModelError("", "Ocurrió un error inesperado al guardar los contenedores.");
                _audit.Error(
                    action: "CEF.Containers.Save",
                    ex: ex,
                    detailMessage: "Error inesperado al guardar contenedores",
                    entityType: "CefTransaction",
                    entityId: transactionId.ToString()
                );
                var pageVm = await _cefContainerService.PrepareProcessContainersPageAsync(transactionId);
                pageVm.Containers = viewModel.Containers ?? new List<CefContainerProcessingViewModel>();
                return View(pageVm);
            }
        }

        /// <summary>
        /// Devuelve true si el detalle (POST) difiere del persistido en campos relevantes.
        /// </summary>
        private static bool DetailChanged(CefValueDetailViewModel posted, CefValueDetail? persisted)
        {
            if (persisted == null) return true;

            return
                !string.Equals(posted.ValueType.ToString(), persisted.ValueType, StringComparison.OrdinalIgnoreCase) ||
                (posted.DenominationId ?? 0) != (persisted.DenominationId ?? 0) ||
                (posted.Quantity ?? 0) != (persisted.Quantity ?? 0) ||
                (posted.BundlesCount ?? 0) != (persisted.BundlesCount ?? 0) ||
                (posted.LoosePiecesCount ?? 0) != (persisted.LoosePiecesCount ?? 0) ||
                (decimal.Round(posted.UnitValue ?? 0m, 2) != decimal.Round(persisted.UnitValue ?? 0m, 2)) ||
                (posted.QualityId ?? 0) != (persisted.QualityId ?? 0) ||
                (posted.IsHighDenomination) != (persisted.IsHighDenomination);
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
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "CEF_REC", "CEF_COL")]
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
                if (containers.Any(c =>
                String.Equals(c.ContainerStatus, CefContainerStatusEnum.InProcess.ToString(), StringComparison.OrdinalIgnoreCase) ||
                String.Equals(c.ContainerStatus, CefContainerStatusEnum.Pending.ToString(), StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException("No todos los contenedores han sido procesados. Por favor, complete el conteo de todos los contenedores.");
                }

                // 1) No debe haber novedades pendientes
                if (await _cefIncidentService.HasPendingIncidentsByTransactionAsync(transactionId))
                {
                    throw new InvalidOperationException("Hay novedades pendientes de aprobación. Debe aprobarlas antes de finalizar el conteo.");
                }

                // 2) Recalcula neto y valida que sea 0
                await _cefTransactionService.RecalcTotalsAndNetDiffAsync(transactionId);

                var tx = await _cefTransactionService.GetCefTransactionByIdAsync(transactionId);
                if (tx == null) throw new InvalidOperationException("Transacción no encontrada.");

                if ((tx.ValueDifference) != 0m)
                {
                    var dif = tx.ValueDifference;
                    throw new InvalidOperationException($"La transacción aún presenta diferencia neta {dif:N0}. Debe quedar en 0 para finalizar.");
                }

                var ok = await _cefTransactionService.UpdateTransactionStatusAsync(transactionId, CefTransactionStatusEnum.PendienteRevision, currentUser.Id);

                if (ok)
                {
                    TempData["SuccessMessage"] = $"Transacción {transactionId} finalizada para conteo y enviada a revisión.";
                    _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Transacción CEF finalizada y enviada a revisión | ID Transacción: {TransactionId}.",
                        currentUser.UserName, IpAddressForLogging, transactionId);
                    _audit.Info(
                        action: "CEF.FinalizeCounting",
                        detailMessage: "Transacción enviada a revisión",
                        result: "SentToReview",
                        entityType: "CefTransaction",
                        entityId: transactionId.ToString()
                    );
                    return RedirectToAction(nameof(Collections));
                }
                else
                {
                    TempData["ErrorMessage"] = "No se pudo finalizar el conteo de la transacción. Verifique el estado.";
                    _logger.LogWarning("Usuario: {Usuario} | IP: {IP} | Acción: Fallo al finalizar conteo | ID Transacción: {TransactionId}.", currentUser.UserName, IpAddressForLogging, transactionId);
                    _audit.Warn(
                        action: "CEF.FinalizeCounting",
                        detailMessage: "No se pudo finalizar el conteo (estado no válido)",
                        result: "TransitionDenied",
                        entityType: "CefTransaction",
                        entityId: transactionId.ToString()
                    );
                    return RedirectToAction(nameof(ProcessContainers), new { transactionId });
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                _logger.LogError(ex, "Usuario: {Usuario} | IP: {IP} | Acción: Error al finalizar conteo | Mensaje: {ErrorMessage}.", currentUser.UserName, IpAddressForLogging, ex.Message);
                _audit.Warn(
                    action: "CEF.FinalizeCounting",
                    detailMessage: ex.Message,
                    result: "InvalidOperation",
                    entityType: "CefTransaction",
                    entityId: transactionId.ToString()
                );
                return RedirectToAction(nameof(ProcessContainers), new { transactionId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al finalizar el conteo.";
                _logger.LogError(ex, "Usuario: {Usuario} | IP: {IP} | Acción: Error inesperado al finalizar conteo.", currentUser.UserName, IpAddressForLogging);
                _audit.Error(
                    action: "CEF.FinalizeCounting",
                    ex: ex,
                    detailMessage: "Error inesperado al finalizar conteo",
                    entityType: "CefTransaction",
                    entityId: transactionId.ToString()
                );
                return RedirectToAction(nameof(ProcessContainers), new { transactionId });
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
        [HttpGet("ReviewTransaction/{transactionId}")]
        [RequiredPermission(PermissionType.View, "CEF_REC", "CEF_DEL", "CEF_COL", "CEF_SUP")]
        public async Task<IActionResult> ReviewTransaction(int transactionId)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToAction("Login", "Account");
                
            await SetCommonViewBagsCefAsync(currentUser, "Revisión CEF", "CEF_REC", "CEF_DEL", "CEF_COL", "CEF_SUP");

            var roles = await _userManager.GetRolesAsync(currentUser);
            var vistas = new[] { "CEF_REC", "CEF_DEL", "CEF_COL", "CEF_SUP" };
            bool canReview = false;

            foreach (var v in vistas)
            {
                if (await HasPermisionForView(roles, v, PermissionType.Edit))
                {
                    canReview = true;
                    break;
                }
            }

            var vm = await _cefTransactionService.PrepareReviewViewModelAsync(transactionId);
            if (vm == null)
            {
                TempData["ErrorMessage"] = "Transacción de Centro de Efectivo no encontrada para revisión.";
                return RedirectToAction(nameof(Index));
            }

            if (vm.CurrentStatus != CefTransactionStatusEnum.PendienteRevision)
            {
                TempData["ErrorMessage"] = $"La transacción {vm.SlipNumber} no está en estado 'Pendiente de Revisión'. Estado actual: {vm.CurrentStatus.ToString().Replace("_", " ")}.";
                _audit.Warn(
                    action: "CEF.Review.Open",
                    detailMessage: $"Estado actual: {vm.CurrentStatus}",
                    result: "WrongState",
                    entityType: "CefTransaction",
                    entityId: transactionId.ToString()
                );
                return RedirectToAction(nameof(Index));
            }

            vm.AvailableStatuses = new List<SelectListItem>
            {
                new SelectListItem{ Text = "Aprobada", Value = CefTransactionStatusEnum.Aprobado.ToString() },
                new SelectListItem{ Text = "Rechazada", Value = CefTransactionStatusEnum.Rechazado.ToString() }
            };

            ViewBag.CanReview = canReview;

            bool isProvision = string.Equals(vm.TransactionTypeCode, "PV", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(vm.TransactionTypeCode, "PR", StringComparison.OrdinalIgnoreCase);

            ViewBag.BackController = "Cef";
            ViewBag.BackAction = isProvision ? "Supplies" : "Collections";

            _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Acceso a revisión de Transacción | ID Transacción: {TransactionId}.", currentUser.UserName, IpAddressForLogging, transactionId);
            _audit.Info(
                action: "CEF.Review.Open",
                detailMessage: "Apertura de pantalla de revisión",
                result: "OK",
                entityType: "CefTransaction",
                entityId: transactionId.ToString(),
                urlId: vm.ServiceOrderId
            );
            return View(vm);
        }

        /// <summary>
        /// Procesa la aprobación o rechazo final de una transacción de Centro de Efectivo.
        /// </summary>
        /// <remarks>
        /// Requiere permiso 'Edit' para el módulo "CEF".
        /// </remarks>
        /// <param name="transactionId">Identificador de la transacción.</param>
        /// <param name="viewModel">ViewModel con el ID de la transacción y el nuevo estado (Aprobada/Rechazada).</param>
        /// <returns>Redirección al dashboard de CEF.</returns>
        [HttpPost("ReviewTransaction/{transactionId?}")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "CEF_REC", "CEF_DEL", "CEF_COL", "CEF_SUP")]
        public async Task<IActionResult> ReviewTransaction(int? transactionId, CefTransactionReviewViewModel viewModel)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            if ((viewModel?.Id ?? 0) <= 0 && transactionId.HasValue)
                viewModel!.Id = transactionId.Value;

            await SetCommonViewBagsCefAsync(currentUser, "Procesando Revisión CEF", "CEF_REC", "CEF_DEL", "CEF_COL", "CEF_SUP");

            viewModel.AvailableStatuses = new List<SelectListItem>
            {
                new("Aprobada",  CefTransactionStatusEnum.Aprobado.ToString()),
                new("Rechazada", CefTransactionStatusEnum.Rechazado.ToString())
            };

            ViewBag.CanReview = await HasPermisionForView(await _userManager.GetRolesAsync(currentUser), "CEF", PermissionType.Edit);


            if (!ModelState.IsValid)
            {
                var errors = ModelState.Where(kvp => kvp.Value.Errors.Any()).ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                _logger.LogWarning("Usuario: {Usuario} | IP: {IP} | Acción: Revisión de Transacción - Modelo Inválido | Errores: {@Errores} |",
                    currentUser.UserName, IpAddressForLogging, errors);

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Datos incompletos o inválidos.", errors });
                }

                var currentData = await _cefTransactionService.PrepareReviewViewModelAsync(viewModel.Id);
                if (currentData != null)
                {
                    viewModel.ServiceOrderId = currentData.ServiceOrderId;
                    viewModel.SlipNumber = currentData.SlipNumber;
                    viewModel.TransactionType = currentData.TransactionType;
                    viewModel.Currency = currentData.Currency;
                    viewModel.TotalDeclaredValue = currentData.TotalDeclaredValue;
                    viewModel.TotalCountedValue = currentData.TotalCountedValue;
                    viewModel.ValueDifference = currentData.ValueDifference;
                    viewModel.CurrentStatus = currentData.CurrentStatus;
                    viewModel.ReviewerUserName = currentData.ReviewerUserName;
                    viewModel.ReviewDate = currentData.ReviewDate;
                    viewModel.ContainerSummaries = currentData.ContainerSummaries;
                    viewModel.IncidentSummaries = currentData.IncidentSummaries;
                }
                return View(viewModel);
            }

            try
            {
                bool isProvision = string.Equals(viewModel.TransactionTypeCode, "PV", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(viewModel.TransactionTypeCode, "PR", StringComparison.OrdinalIgnoreCase);
                var redirectAction = isProvision ? "Supplies" : "Collections";
                var redirectUrl = Url.Action(redirectAction, "Cef");

                var success = await _cefTransactionService.ProcessReviewApprovalAsync(viewModel, currentUser.Id);
                if (success)
                {
                    var msg = $"Transacción {viewModel.SlipNumber} {viewModel.NewStatus.ToString().Replace("_", " ")} exitosamente.";
                    _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Transacción CEF {Status} | ID Transacción: {TransactionId}.",
                        currentUser.UserName, IpAddressForLogging, viewModel.NewStatus, viewModel.Id);

                    if (Request.IsAjaxRequest())
                    {
                        return Json(new
                        {
                            success = true,
                            message = msg,
                            data = new { redirectUrl }
                        });
                    }

                    TempData["SuccessMessage"] = msg;
                    _audit.Info(
                        action: "CEF.Review.ControllerAck",
                        detailMessage: $"Revisión aplicada ({viewModel.NewStatus})",
                        result: "OK",
                        entityType: "CefTransaction",
                        entityId: viewModel.Id.ToString()
                    );
                    return Redirect(redirectUrl);
                }
                else
                {
                    _logger.LogWarning("Usuario: {Usuario} | IP: {IP} | Acción: Fallo al procesar revisión de Transacción | ID Transacción: {TransactionId}.",
                        currentUser.UserName, IpAddressForLogging, viewModel.Id);

                    if (Request.IsAjaxRequest())
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "No se pudo procesar la revisión de la transacción. Verifique el estado."
                        });
                    }

                    ModelState.AddModelError("", "No se pudo procesar la revisión de la transacción. Verifique el estado.");
                    _audit.Warn(
                        action: "CEF.Review",
                        detailMessage: "No se pudo procesar la revisión",
                        result: "Failed",
                        entityType: "CefTransaction",
                        entityId: viewModel.Id.ToString()
                    );
                    return View(viewModel);
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Usuario: {Usuario} | IP: {IP} | Acción: Error de operación inválida al revisar Transacción | Mensaje: {ErrorMessage}.", currentUser.UserName, IpAddressForLogging, ex.Message);

                if (Request.IsAjaxRequest())
                {
                    return BadRequest(new { success = false, message = ex.Message });
                }

                ModelState.AddModelError("", ex.Message);

                var currentData = await _cefTransactionService.PrepareReviewViewModelAsync(viewModel.Id);
                if (currentData != null)
                {
                    viewModel.ContainerSummaries = currentData.ContainerSummaries;
                    viewModel.IncidentSummaries = currentData.IncidentSummaries;
                    viewModel.TotalDeclaredValue = currentData.TotalDeclaredValue;
                    viewModel.TotalCountedValue = currentData.TotalCountedValue;
                    viewModel.ValueDifference = currentData.ValueDifference;
                    viewModel.TransactionType = currentData.TransactionType;
                    viewModel.Currency = currentData.Currency;
                    viewModel.SlipNumber = currentData.SlipNumber;
                    viewModel.ServiceOrderId = currentData.ServiceOrderId;
                    viewModel.CurrentStatus = currentData.CurrentStatus;
                    viewModel.ReviewerUserName = currentData.ReviewerUserName;
                    viewModel.ReviewDate = currentData.ReviewDate;
                }
                _audit.Warn(
                    action: "CEF.Review",
                    detailMessage: ex.Message,
                    result: "InvalidOperation",
                    entityType: "CefTransaction",
                    entityId: viewModel.Id.ToString()
                );
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Usuario: {Usuario} | IP: {IP} | Acción: Error inesperado al revisar Transacción.", currentUser.UserName, IpAddressForLogging);

                const string msg = "Ocurrió un error inesperado al revisar la transacción.";
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = msg });
                }

                ModelState.AddModelError("", "Ocurrió un error inesperado al revisar la transacción.");
                var currentData = await _cefTransactionService.PrepareReviewViewModelAsync(viewModel.Id);
                if (currentData != null)
                {
                    viewModel.ContainerSummaries = currentData.ContainerSummaries;
                    viewModel.IncidentSummaries = currentData.IncidentSummaries;
                    viewModel.TotalDeclaredValue = currentData.TotalDeclaredValue;
                    viewModel.TotalCountedValue = currentData.TotalCountedValue;
                    viewModel.ValueDifference = currentData.ValueDifference;
                    viewModel.TransactionType = currentData.TransactionType;
                    viewModel.Currency = currentData.Currency;
                    viewModel.SlipNumber = currentData.SlipNumber;
                    viewModel.ServiceOrderId = currentData.ServiceOrderId;
                    viewModel.CurrentStatus = currentData.CurrentStatus;
                    viewModel.ReviewerUserName = currentData.ReviewerUserName;
                    viewModel.ReviewDate = currentData.ReviewDate;
                }
                _audit.Error(
                    action: "CEF.Review",
                    ex: ex,
                    detailMessage: "Error inesperado en revisión",
                    entityType: "CefTransaction",
                    entityId: viewModel.Id.ToString()
                );
                return View(viewModel);
            }
        }

        /// <summary>
        /// Muestra el detalle de un contenedor específico, incluyendo su desglose por denominaciones y calidades.
        /// </summary>
        /// <param name="transactionId">Identificador de la transacción.</param>
        /// <returns>Vista con el detalle de la transacción.</returns>
        [HttpGet("Detail/{transactionId}")]
        [RequiredPermission(PermissionType.View, "CEF_REC", "CEF_DEL", "CEF_COL", "CEF_SUP")]
        public async Task<IActionResult> Detail(int transactionId)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToAction("Login", "Account");

            await SetCommonViewBagsCefAsync(currentUser, "Detalle de Transacción CEF", "CEF_REC", "CEF_DEL", "CEF_COL", "CEF_SUP");

            var vm = await _cefTransactionService.PrepareDetailViewModelAsync(transactionId);
            if (vm == null)
            {
                TempData["ErrorMessage"] = "Transacción de Centro de Efectivo no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = $"Detalle de Transacción CEF #{vm.SlipNumber}";

            var roles = await _userManager.GetRolesAsync(currentUser);
            ViewBag.CanReview = await HasPermisionForView(roles, "CEF_SUP", PermissionType.Edit);

            _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Acceso a detalle de Contenedor | ID Contenedor: {ContainerId}.", currentUser.UserName, IpAddressForLogging, transactionId);
            _audit.Info(
                action: "CEF.Container.Detail",
                detailMessage: "Apertura de pantalla de detalle de contenedor",
                result: "OK",
                entityType: "CefContainer",
                entityId: transactionId.ToString(),
                urlId: vm.CefTransactionId.ToString()
            );
            return View("Detail", vm);
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

        [HttpGet("ListIncidents")]
        public async Task<IActionResult> ListIncidents(int transactionId, int? containerId)
        {
            var list = await _cefIncidentService.GetIncidentsAsync(transactionId, containerId, null);
            // Devuelve JSON simple para pintar en el front
            var data = list.Select(i => new
            {
                i.Id,
                Type = _context.CefIncidentTypes.FirstOrDefault(t => t.Id == i.IncidentTypeId)!.Code,
                i.Description,
                i.AffectedAmount,
                i.AffectedDenomination,
                i.AffectedQuantity,
                i.IncidentStatus,
                Date = i.IncidentDate.ToString("yyyy-MM-dd HH:mm")
            });
            return Json(new { ok = true, data });
        }

        [HttpPost("CreateIncident")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateIncident([FromForm] CefIncidentViewModel vm)
        {
            try
            {
                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null) return Unauthorized();

                if (vm == null)
                    return BadRequest(new { ok = false, message = "Datos incompletos para registrar novedad." });

                if (!vm.CefTransactionId.HasValue && !vm.CefContainerId.HasValue && !vm.CefValueDetailId.HasValue)
                    return BadRequest(new { ok = false, message = "Debe especificar al menos una transacción, contenedor o detalle." });

                if (Request.Form.TryGetValue("IncidentTypeCode", out var code) && !string.IsNullOrWhiteSpace(code))
                {
                    if (IncidentTypeCodeMap.TryFromCode(code, out var parsedType))
                    {
                        vm.IncidentType = parsedType;
                    }
                    else
                    {
                        return BadRequest(new { ok = false, message = $"Código de tipo de novedad no válido: {code}" });
                    }
                }

                var inc = await _cefIncidentService.RegisterIncidentAsync(vm, currentUser.Id);
                if (inc.CefTransactionId.HasValue)
                    await _cefTransactionService.RecalcTotalsAndNetDiffAsync(inc.CefTransactionId.Value);

                return Json(new { ok = true, message = "Novedad registrada." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { ok = false, message = ex.Message });
            }
        }

        [HttpPost("ResolveIncident")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveIncident(int id, string newStatus = "Ajustada")
        {
            var ok = await _cefIncidentService.ResolveIncidentAsync(id, newStatus);
            if (!ok) return BadRequest(new { ok = false, message = "No se pudo resolver la novedad." });

            // localiza transacción para recalcular
            var inc = await _cefIncidentService.GetIncidentByIdAsync(id);
            if (inc?.CefTransactionId != null)
                await _cefTransactionService.RecalcTotalsAndNetDiffAsync(inc.CefTransactionId.Value);

            return Json(new { ok = true, message = "Novedad actualizada." });
        }

        [HttpGet("CheckPendingIncidents")]
        public async Task<IActionResult> CheckPendingIncidents(int transactionId)
        {
            var hasPending = await _cefIncidentService.HasPendingIncidentsByTransactionAsync(transactionId);
            return Json(new { ok = true, hasPending });
        }

        /// <summary>
        /// Obtiene los totales declarados, contados y la diferencia neta de una transacción.
        /// </summary>
        /// <param name="transactionId">Identificador de la transacción</param>
        /// <returns>JSON con los totales o error si no se encuentra la transacción.</returns>
        [HttpGet("GetTotals")]
        public async Task<IActionResult> GetTotals(int transactionId)
        {
            var tx = await _context.CefTransactions
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (tx == null)
                return NotFound(new { ok = false, message = "Transacción no encontrada." });

            var effect = await _cefIncidentService.SumApprovedEffectByTransactionAsync(transactionId);
            var totalDeclared = tx.TotalDeclaredValue;
            var totalCounted = tx.TotalCountedValue;
            var totalOverall = tx.TotalDeclaredValue + tx.TotalCountedValue;
            var netDiff = (totalCounted - totalDeclared) + effect;

            return Json(new
            {
                ok = true,
                totalDeclared,
                totalCounted,
                totalOverall,
                effect,
                netDiff
            });
        }


        /// <summary>
        /// Obtiene los detalles de una novedad por su ID.
        /// </summary>
        /// <param name="id">Identificador del incidente.</param>
        /// <returns>JSON con los detalles del incidente o error si no se encuentra.</returns>
        [HttpGet("GetIncident")]
        public async Task<IActionResult> GetIncident(int id)
        {
            var inc = await _cefIncidentService.GetIncidentByIdAsync(id);
            if (inc == null) return NotFound(new { ok = false });

            var typeCode = await _context.CefIncidentTypes
                .Where(t => t.Id == inc.IncidentTypeId)
                .Select(t => t.Code)
                .FirstOrDefaultAsync() ?? "";

            return Json(new
            {
                ok = true,
                data = new
                {
                    inc.Id,
                    inc.CefTransactionId,
                    inc.CefContainerId,
                    IncidentTypeId = inc.IncidentTypeId,
                    IncTypeCode = typeCode,
                    inc.Description,
                    inc.AffectedDenomination,
                    inc.AffectedQuantity,
                    inc.AffectedAmount,
                    inc.IncidentStatus
                }
            });
        }

        /// <summary>
        /// Actualiza los detalles de una novedad reportada.
        /// </summary>
        /// <param name="id">Identificador de la novedad.</param>
        /// <returns>JSON indicando éxito o error.</returns>
        [HttpPost("UpdateIncident")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateIncident(int id)
        {
            try
            {
                int? typeId = int.TryParse(Request.Form["IncidentTypeId"], out var ti) ? ti : null;
                string? typeCode = Request.Form["IncidentTypeCode"];
                int? denomId = int.TryParse(Request.Form["AffectedDenomination"], out var dd) ? dd : null;
                int? qty = int.TryParse(Request.Form["AffectedQuantity"], out var qq) ? qq : null;
                decimal? amount = decimal.TryParse(Request.Form["AffectedAmount"], out var aa) ? aa : null;
                string? desc = Request.Form["Description"];

                CefIncidentTypeCategoryEnum? typeEnum = null;
                if (!string.IsNullOrWhiteSpace(typeCode))
                {
                    if (!IncidentTypeCodeMap.TryFromCode(typeCode, out var parsedType))
                        return BadRequest(new { ok = false, message = $"Código de tipo de novedad no válido: {typeCode}" });
                    typeEnum = parsedType;
                }

                var ok = await _cefIncidentService.UpdateReportedIncidentAsync(
                    id,
                    newTypeId: typeId,
                    newType: typeEnum,
                    newDenominationId: denomId,
                    newQuantity: qty,
                    newAmount: amount,
                    newDescription: desc
                );

                // 4) Recalcula totales
                var inc = await _cefIncidentService.GetIncidentByIdAsync(id);
                if (inc?.CefTransactionId != null)
                    await _cefTransactionService.RecalcTotalsAndNetDiffAsync(inc.CefTransactionId.Value);

                return Json(new { ok });
            }
            catch (Exception ex)
            {
                return BadRequest(new { ok = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Elimina una novedad reportada por su ID.
        /// </summary>
        /// <param name="id">Identificador de la novedad.</param>
        /// <returns>JSON indicando éxito o error.</returns>
        [HttpPost("DeleteIncident")]
        public async Task<IActionResult> DeleteIncident(int id)
        {
            try
            {
                var inc = await _cefIncidentService.GetIncidentByIdAsync(id);
                if (inc == null) return NotFound(new { ok = false, message = "Novedad no encontrada." });

                var ok = await _cefIncidentService.DeleteReportedIncidentAsync(id);
                if (inc.CefTransactionId.HasValue)
                    await _cefTransactionService.RecalcTotalsAndNetDiffAsync(inc.CefTransactionId.Value);
                return Json(new { ok });
            }
            catch (Exception ex)
            {
                return BadRequest(new { ok = false, message = ex.Message });
            }
        }
    }
}