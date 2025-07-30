using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Filters;
using VCashApp.Models;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.CentroEfectivo;
using VCashApp.Services;
using VCashApp.Services.Cef;
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
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<CefController> logger)
            : base(context, userManager)
        {
            _cefTransactionService = cefTransactionService;
            _cefContainerService = cefContainerService;
            _cefIncidentService = cefIncidentService;
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
            ViewBag.AvailableBranches = (await _context.AdmSucursales
                .Select(s => new SelectListItem { Value = s.CodSucursal.ToString(), Text = s.NombreSucursal })
                .ToListAsync());

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            ViewBag.HasCreatePermission = await HasPermisionForView(userRoles, "CEF", PermissionType.Create);
            ViewBag.HasEditPermission = await HasPermisionForView(userRoles, "CEF", PermissionType.Edit);
            ViewBag.HasViewPermission = await HasPermisionForView(userRoles, "CEF", PermissionType.View);
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
        /// <param name="searchTerm">Término de búsqueda.</param>
        /// <param name="pageNumber">Número de página para paginación.</param>
        /// <param name="pageSize">Cantidad de registros por página.</param>
        /// <returns>Una vista con la lista de transacciones de CEF o un parcial si es una petición AJAX.</returns>
        [HttpGet("Index")]
        [RequiredPermission(PermissionType.View, "CEF")]
        public async Task<IActionResult> Index(
            int? branchId, DateOnly? startDate, DateOnly? endDate, CefTransactionStatusEnum? status,
            string? searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsCefAsync(currentUser, "Centro Efectivo");

            var (transactions, totalRecords) = await _cefTransactionService.GetFilteredCefTransactionsAsync(
                branchId, startDate, endDate, status, searchTerm, pageNumber, pageSize);

            var transactionStatuses = Enum.GetValues(typeof(CefTransactionStatusEnum))
                .Cast<CefTransactionStatusEnum>()
                .Select(e => new SelectListItem { Value = e.ToString(), Text = e.ToString().Replace("_", " ") })
                .ToList();
            transactionStatuses.Insert(0, new SelectListItem { Value = "", Text = "-- Seleccionar Estado --" });

            var dashboardViewModel = new CefDashboardViewModel
            {
                Transactions = transactions,
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                TotalData = totalRecords,
                SearchTerm = searchTerm,
                CurrentBranchId = branchId,
                CurrentStartDate = startDate,
                CurrentEndDate = endDate,
                CurrentStatus = status,
                TransactionStatuses = transactionStatuses,
                AvailableBranches = new List<SelectListItem>()
            };

            ViewBag.CurrentPage = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = dashboardViewModel.TotalPages;
            ViewBag.TotalData = totalRecords;
            ViewBag.SearchTerm = searchTerm;
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
                    viewModel = await _cefTransactionService.PrepareCheckinViewModelAsync(serviceOrderId, routeId, currentUser.Id, IpAddressForLogging);
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
                return View(viewModel); // Para submits de formulario HTML tradicional
            }

            try
            {
                var newTransaction = await _cefTransactionService.ProcessCheckinViewModelAsync(viewModel, currentUser.Id, IpAddressForLogging);
                TempData["SuccessMessage"] = $"Check-in para planilla {newTransaction.SlipNumber} registrado exitosamente. La transacción está lista para el conteo.";
                _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Check-in Exitoso | Transacción ID: {TransactionId} | Planilla: {PlanillaNumber}.",
                    currentUser.UserName, IpAddressForLogging, newTransaction.Id, newTransaction.SlipNumber);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(ServiceResult.SuccessResult("Check-in exitoso.", newTransaction.Id));
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

        /// <summary>
        /// Muestra la vista para procesar (contar) los contenedores de una transacción de CEF.
        /// </summary>
        /// <remarks>
        /// Requiere permiso 'Edit' para el módulo "CEF".
        /// </remarks>
        /// <param name="transactionId">ID de la transacción CEF a procesar.</param>
        /// <param name="containerId">ID opcional de un contenedor específico a editar/continuar.</param>
        /// <returns>Vista de procesamiento de contenedores.</returns>
        [HttpGet("ProcessContainers/{transactionId}")]
        [RequiredPermission(PermissionType.Edit, "CEF")] // Cajeros o Supervisores que editen
        public async Task<IActionResult> ProcessContainers(int transactionId, int? containerId = null)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            await SetCommonViewBagsCefAsync(currentUser, "Procesar Contenedores CEF");

            var transaction = await _cefTransactionService.GetCefTransactionByIdAsync(transactionId);
            if (transaction == null)
            {
                TempData["ErrorMessage"] = "Transacción de Centro de Efectivo no encontrada.";
                return RedirectToAction(nameof(Index));
            }
            if (transaction.TransactionStatus == CefTransactionStatusEnum.Approved.ToString() ||
                transaction.TransactionStatus == CefTransactionStatusEnum.Rejected.ToString() ||
                transaction.TransactionStatus == CefTransactionStatusEnum.Cancelled.ToString() ||
                transaction.TransactionStatus == CefTransactionStatusEnum.PendingReview.ToString())
            {
                TempData["ErrorMessage"] = $"La transacción {transaction.SlipNumber} ya está en estado '{transaction.TransactionStatus.Replace("_", " ")}' y no puede ser procesada.";
                return RedirectToAction(nameof(Index));
            }
            if (transaction.TransactionStatus == CefTransactionStatusEnum.Checkin.ToString())
            {
                // No hay cambio de estado explícito aquí, el servicio lo manejará al guardar el contenedor.
            }

            CefContainerProcessingViewModel viewModel = await _cefContainerService.PrepareContainerProcessingViewModelAsync(containerId, transactionId);

            // Cargar SelectLists para el frontend
            ViewBag.ValueTypes = Enum.GetValues(typeof(CefValueTypeEnum))
                                     .Cast<CefValueTypeEnum>()
                                     .Select(e => new SelectListItem { Value = e.ToString(), Text = e.ToString().Replace("_", " ") })
                                     .ToList();
            ViewBag.IncidentTypes = (await _cefIncidentService.GetAllIncidentTypesAsync())
                                    .Select(it => new SelectListItem { Value = it.Code, Text = it.Description })
                                    .ToList();

            ViewBag.TransactionId = transactionId;
            ViewBag.TransactionNumber = transaction.SlipNumber;
            ViewBag.TransactionStatus = transaction.TransactionStatus.Replace("_", " ");
            ViewBag.AvailableContainers = (await _cefContainerService.GetContainersByTransactionIdAsync(transactionId))
                .Where(c => c.ParentContainerId == null)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = $"{c.ContainerCode} ({c.ContainerType.Replace("_", " ")})" })
                .ToList();
            ViewBag.AvailableContainers.Insert(0, new SelectListItem { Value = "0", Text = "-- Nuevo Contenedor --" });

            _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Acceso a procesamiento de contenedores | Transacción: {TransactionId} | Contenedor: {ContainerId}.",
                currentUser.UserName, IpAddressForLogging, transactionId, containerId ?? 0);
            return View(viewModel);
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
        [HttpPost("ProcessContainers")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "CEF")]
        public async Task<IActionResult> ProcessContainers(CefContainerProcessingViewModel viewModel)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            if (currentUser == null) return Unauthorized();

            await SetCommonViewBagsCefAsync(currentUser, "Procesando Contenedores CEF");

            ViewBag.ValueTypes = Enum.GetValues(typeof(CefValueTypeEnum)).Cast<CefValueTypeEnum>().Select(e => new SelectListItem { Value = e.ToString(), Text = e.ToString().Replace("_", " ") }).ToList();
            ViewBag.IncidentTypes = (await _cefIncidentService.GetAllIncidentTypesAsync()).Select(it => new SelectListItem { Value = it.Code, Text = it.Description }).ToList();
            ViewBag.TransactionId = viewModel.CefTransactionId; 

            if (!ModelState.IsValid)
            {
                var fieldErrors = ModelState
                    .Where(kvp => kvp.Value.Errors.Any())
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                _logger.LogWarning("Usuario: {Usuario} | IP: {IP} | Acción: Procesamiento de Contenedores - Modelo Inválido | Errores: {@Errores} |",
                    currentUser.UserName, IpAddressForLogging, fieldErrors);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(ServiceResult.FailureResult("Hay errores en el formulario de contenedor.", errors: fieldErrors));
                }
                return View(viewModel);
            }

            try
            {
                var savedContainer = await _cefContainerService.SaveContainerAndDetailsAsync(viewModel, currentUser.Id);

                foreach (var incidentVm in viewModel.Incidents)
                {
                    incidentVm.CefContainerId = savedContainer.Id;
                    await _cefIncidentService.RegisterIncidentAsync(incidentVm, currentUser.Id);
                }

                TempData["SuccessMessage"] = $"Contenedor '{savedContainer.ContainerCode}' procesado exitosamente.";
                _logger.LogInformation("Usuario: {Usuario} | IP: {IP} | Acción: Contenedor procesado | ID Contenedor: {ContainerId} | ID Transacción: {TransactionId}.",
                    currentUser.UserName, IpAddressForLogging, savedContainer.Id, savedContainer.CefTransactionId);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(ServiceResult.SuccessResult("Contenedor procesado exitosamente.", new { containerId = savedContainer.Id, transactionId = savedContainer.CefTransactionId }));
                }
                return RedirectToAction(nameof(ProcessContainers), new { transactionId = savedContainer.CefTransactionId, containerId = savedContainer.Id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                _logger.LogError(ex, "Usuario: {Usuario} | IP: {IP} | Acción: Error al procesar contenedor | Mensaje: {ErrorMessage}.", currentUser.UserName, IpAddressForLogging, ex.Message);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(ServiceResult.FailureResult(ex.Message));
                }
                return View(viewModel);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ocurrió un error inesperado al procesar el contenedor.");
                _logger.LogError(ex, "Usuario: {Usuario} | IP: {IP} | Acción: Error inesperado al procesar contenedor.", currentUser.UserName, IpAddressForLogging);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(ServiceResult.FailureResult("Ocurrió un error inesperado."));
                }
                return View(viewModel);
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
    }
}