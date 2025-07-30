using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Filters;
using VCashApp.Models;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels;
using VCashApp.Services;
using VCashApp.Services.DTOs;

namespace VCashApp.Controllers
{
    /// <summary>
    /// Controlador para la gestión de rutas diarias.
    /// Incluye funcionalidades de generacion de rutas, creacion de ruta indivual, gestion de cada ruta por medio de flujos e historial.
    /// </summary>
    [ApiController]
    [Route("/RutasDiarias")]
    public class RutasDiariasController : BaseController
    {
        private readonly IRutaDiariaService _rutaDiariaService;
        private readonly IExportService _exportService;

        public RutasDiariasController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IRutaDiariaService rutaDiariaService,
            IExportService exportService
        ) : base(context, userManager)
        {
            _rutaDiariaService = rutaDiariaService;
            _exportService = exportService;
        }

        // Método auxiliar para configurar ViewBags comunes
        private async Task SetCommonViewBagsAsync(ApplicationUser currentUser, string pageName)
        {
            await base.SetCommonViewBagsBaseAsync(currentUser, pageName);

            bool isAdminFromBase = (bool)ViewBag.IsAdmin;
            string? currentCodPerfilFromBase = ViewBag.CurrentCodPerfil as string;

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            bool hasRUDPLView = await HasPermisionForView(userRoles, "RUDPL", PermissionType.View);
            bool hasRUDOPView = await HasPermisionForView(userRoles, "RUDOP", PermissionType.View);
            bool hasRUDCEView = await HasPermisionForView(userRoles, "RUDCE", PermissionType.View);
            bool hasRUDHISView = await HasPermisionForView(userRoles, "RUDHIS", PermissionType.View);
            bool hasRUDCreate = await HasPermisionForView(userRoles, "RUDPL", PermissionType.Create);
            bool hasRUDEdit = await HasPermisionForView(userRoles, "RUDPL", PermissionType.Edit);
            bool hasRUDOPEdit = await HasPermisionForView(userRoles, "RUDOP", PermissionType.Edit);
            bool hasRUDCEEdit = await HasPermisionForView(userRoles, "RUDCE", PermissionType.Edit);

            ViewBag.canCreate = (bool)ViewBag.IsAdmin || (hasRUDCreate);
            ViewBag.canEditPlaneador = (bool)ViewBag.IsAdmin || (hasRUDEdit);
            ViewBag.canCargarEfectivoCef = (bool)ViewBag.IsAdmin || (hasRUDCEEdit); 
            ViewBag.canDescargarEfectivoCef = (bool)ViewBag.IsAdmin || (hasRUDCEEdit);
            ViewBag.canRegistrarSalidaSupervisor = (bool)ViewBag.IsAdmin || (hasRUDOPEdit);
            ViewBag.canRegistrarEntradaSupervisor = (bool)ViewBag.IsAdmin || (hasRUDOPEdit);
        }

        // Método para limpiar ModelState (reutilizado del proyecto anterior)
        private void ClearModelStateForLaterStages(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState)
        {
            modelState.Remove(nameof(TdvRutaDiaria.UsuarioPlaneacionObj));
            modelState.Remove(nameof(TdvRutaDiaria.UsuarioCEFCargueObj));
            modelState.Remove(nameof(TdvRutaDiaria.UsuarioCEFDescargueObj));
            modelState.Remove(nameof(TdvRutaDiaria.UsuarioSupervisorAperturaObj));
            modelState.Remove(nameof(TdvRutaDiaria.UsuarioSupervisorCierreObj));

            modelState.Remove(nameof(TdvRutaDiaria.RutaMaster));
            modelState.Remove(nameof(TdvRutaDiaria.Sucursal));
            modelState.Remove(nameof(TdvRutaDiaria.Vehiculo));
            modelState.Remove(nameof(TdvRutaDiaria.JT));
            modelState.Remove(nameof(TdvRutaDiaria.Conductor));
            modelState.Remove(nameof(TdvRutaDiaria.Tripulante));
            modelState.Remove(nameof(TdvRutaDiaria.CargoJTObj));
            modelState.Remove(nameof(TdvRutaDiaria.CargoConductorObj));
            modelState.Remove(nameof(TdvRutaDiaria.CargoTripulanteObj));

            // Si TdvRutaDetallePunto se excluye de esta migración, asegúrate de que no haya referencia a DetallePuntos
            // modelState.Remove(nameof(TdvRutaDiaria.DetallePuntos)); // Descomentar si TdvRutaDiaria ya no tiene esta propiedad
            // NOTA: TdvRutaDetallePunto no está en esta migración, así que esta línea no debería causar error.
            // Si la añades en el futuro, y TdvRutaDiaria ya no tiene la ICollection, esta línea no iría.
        }

        /// <summary>
        /// Metodo de redireccion segun el rol del usuario Planeador/CEF/SupervisorDeRutas.
        /// </summary>
        /// <remarks>
        /// Requiere el permiso de 'View' para la vista 'RUD'.
        /// </remarks>
        /// <returns>Redireccion hacia Planner/Operations.</returns>
        /// 
        [HttpGet("ListGenerated")]
        [RequiredPermission(PermissionType.View, "RUD")]
        public async Task<IActionResult> ListGenerated(int? page, int pageSize = 15, string search = "", DateOnly? fechaEjecucion = null, int? codSuc = null, int? estado = null)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null) return RedirectToAction("Login", "Account", new { Area = "Identity" });

                await SetCommonViewBagsAsync(currentUser, "Rutas Diarias");
                var currentCodPerfil = ViewBag.CurrentCodPerfil as string;

                bool isAdmin = (bool)ViewBag.IsAdmin;

                if (currentCodPerfil == "Planeador")
                {
                    return RedirectToAction(nameof(PlannerDashboard), new { page, pageSize, search, fechaEjecucion, codSuc, estado });
                }
                else if (currentCodPerfil == "CEF" || currentCodPerfil == "Supervisor")
                {
                    return RedirectToAction(nameof(OperationsDashboard), new { page, pageSize, search, fechaEjecucion, codSuc, estado });
                }
                else
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a Listar Rutas Diarias | Resultado: Perfil no reconocido o sin dashboard específico: {Perfil} |", currentUser.UserName, ipAddress, currentCodPerfil);
                    TempData["ErrorMessage"] = "Su perfil no tiene un panel de rutas definido.";
                    return RedirectToAction("Index", "Home");
                }
            }
        }

        /// <summary>
        /// Muestra el registros de rutas del dia actual para el Planeador, con opciones de filtrado y paginación. 
        /// </summary>
        /// <param name="fechaEjecucion">Filtro por una fecha seleccionada (valor por defecto dia actual).</param>
        /// <param name="codSuc">Filtro por ID de sucursal.</param>
        /// <param name="estado">Estado de la ruta generada.</param>
        /// <param name="search">Término de búsqueda por nombre de la ruta.</param>
        /// <param name="page">Número de página actual.</param>
        /// <param name="pageSize">Número de elementos por página.</param>
        /// <returns>La vista de las rutas, con datos paginados.</returns>
        [HttpGet("PlannerDashboard")]
        [RequiredPermission(PermissionType.View, "RUDPL")]
        public async Task<IActionResult> PlannerDashboard(int? page, int pageSize = 15, string search = "", DateOnly? fechaEjecucion = null, int? codSuc = null, int? estado = null)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null) return RedirectToAction("Login", "Account", new { Area = "Identity" });

                await SetCommonViewBagsAsync(currentUser, "Planner");
                var isAdmin = (bool)ViewBag.IsAdmin;

                DateOnly defaultFechaEjecucionForPlanner = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
                DateOnly filterFechaEjecucion = fechaEjecucion ?? defaultFechaEjecucionForPlanner;

                List<int> permittedSucursalIds = new List<int>();
                if (!isAdmin)
                {
                    permittedSucursalIds = await GetUserPermittedSucursalesAsync(currentUser.Id);
                    if (!permittedSucursalIds.Any())
                    {
                        TempData["ErrorMessage"] = "No tiene sucursales asignadas para ver rutas.";
                        return RedirectToAction("Index", "Home");
                    }
                }

                IQueryable<AdmSucursal> sucursalesQuery = _context.AdmSucursales.Where(s => s.Estado == true);

                if (!isAdmin && permittedSucursalIds.Any())
                {
                    Expression<Func<AdmSucursal, bool>> combinedPredicateSucursal = null;

                    foreach (var sucursalId in permittedSucursalIds)
                    {
                        Expression<Func<AdmSucursal, bool>> currentPredicate = s => s.CodSucursal == sucursalId;

                        if (combinedPredicateSucursal == null)
                        {
                            combinedPredicateSucursal = currentPredicate;
                        }
                        else
                        {
                            var parameter = Expression.Parameter(typeof(AdmSucursal), "s");
                            var body = Expression.OrElse(
                                Expression.Invoke(combinedPredicateSucursal, parameter),
                                Expression.Invoke(currentPredicate, parameter)
                            );
                            combinedPredicateSucursal = Expression.Lambda<Func<AdmSucursal, bool>>(body, parameter);
                        }
                    }
                    if (combinedPredicateSucursal != null)
                    {
                        sucursalesQuery = sucursalesQuery.Where(combinedPredicateSucursal);
                    }
                }
                else if (!isAdmin && !permittedSucursalIds.Any())
                {
                    sucursalesQuery = sucursalesQuery.Where(s => false); 
                }

                var sucursalesForFilter = await sucursalesQuery.ToListAsync();

                ViewBag.SucursalesFilter = new SelectList(sucursalesForFilter, "CodSucursal", "NombreSucursal", codSuc);
                ViewBag.FechaEjecucionFilter = filterFechaEjecucion;
                ViewBag.EstadoFilter = estado;

                var estadosSelectItems = Enum.GetValues(typeof(EstadoRuta))
                    .Cast<EstadoRuta>()
                    .Select(e => new SelectListItem
                    {
                        Value = ((int)e).ToString(),
                        Text = e.ToString().Replace("_", " ")
                    }).ToList();
                estadosSelectItems.Insert(0, new SelectListItem { Value = "", Text = "-- Selecciona --" });
                ViewBag.EstadosFilterList = new SelectList(estadosSelectItems, "Value", "Text", estado);

                ViewBag.DefaultFechaEjecucionBasedOnRole = defaultFechaEjecucionForPlanner.ToString("yyyy-MM-dd");

                IQueryable<TdvRutaDiaria> query = _context.TdvRutasDiarias.Include(rd => rd.RutaMaster).AsQueryable();

                if (!isAdmin && permittedSucursalIds.Any())
                {
                    Expression<Func<TdvRutaDiaria, bool>> combinedPredicateRutas = null;

                    foreach (var sucursalId in permittedSucursalIds)
                    {
                        Expression<Func<TdvRutaDiaria, bool>> currentPredicate = r => r.CodSucursal == sucursalId;

                        if (combinedPredicateRutas == null)
                        {
                            combinedPredicateRutas = currentPredicate;
                        }
                        else
                        {
                            var parameter = Expression.Parameter(typeof(TdvRutaDiaria), "r");
                            var body = Expression.OrElse(
                                Expression.Invoke(combinedPredicateRutas, parameter),
                                Expression.Invoke(currentPredicate, parameter)
                            );
                            combinedPredicateRutas = Expression.Lambda<Func<TdvRutaDiaria, bool>>(body, parameter);
                        }
                    }
                    if (combinedPredicateRutas != null)
                    {
                        query = query.Where(combinedPredicateRutas);
                    }
                }
                else if (!isAdmin && !permittedSucursalIds.Any())
                {
                    query = query.Where(r => false);
                }

                if (codSuc.HasValue && codSuc.Value != 0) { query = query.Where(r => r.CodSucursal == codSuc.Value); }

                query = query.Where(r => r.FechaEjecucion == filterFechaEjecucion);

                if (!estado.HasValue && string.IsNullOrEmpty(search))
                {
                    query = query.Where(r => r.Estado == (int)EstadoRuta.GENERADO || r.Estado == (int)EstadoRuta.PLANEADO);
                }
                else if (estado.HasValue && estado.Value != 0)
                {
                    query = query.Where(r => r.Estado == estado.Value);
                }

                if (!string.IsNullOrEmpty(search))
                {
                    string trimmedSearch = search.Trim().ToLower();
                    query = query.Where(r => r.NombreRuta.ToLower().Contains(trimmedSearch) ||
                                             r.Id.ToLower().Contains(trimmedSearch) ||
                                             r.NombreSucursal.ToLower().Contains(trimmedSearch) ||
                                             (r.NombreJT != null && r.NombreJT.ToLower().Contains(trimmedSearch)) ||
                                             (r.NombreConductor != null && r.NombreConductor.ToLower().Contains(trimmedSearch)));
                }
                query = query.OrderByDescending(r => r.FechaEjecucion).ThenBy(r => r.Id);

                var totalData = await query.CountAsync();
                page = Math.Max(page ?? 1, 1); pageSize = 15;
                int totalPages = (int)Math.Ceiling((double)totalData / pageSize);
                page = Math.Min(page.Value, Math.Max(1, totalPages));
                var data = await query.Skip((page.Value - 1) * pageSize).Take(pageSize).ToListAsync();

                ViewBag.CurrentPage = page.Value; ViewBag.TotalPages = totalPages; ViewBag.TotalData = totalData; 
                ViewBag.SearchTerm = search; ViewBag.PageSize = pageSize; ViewBag.CurrentFechaEjecucion = filterFechaEjecucion.ToString("yyyy-MM-dd"); 
                ViewBag.CurrentCodSuc = codSuc; ViewBag.CurrentEstado = estado;

                ViewBag.canCreate = (bool)ViewBag.IsAdmin || ((bool)ViewBag.canCreate);
                ViewBag.canEditPlaneador = (bool)ViewBag.IsAdmin || ((bool)ViewBag.canEditPlaneador);

                Log.Information("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a Planner Dashboard | Cantidad de rutas: {Count} | Respuesta: Acceso permitido | ", currentUser.UserName, ipAddress, data.Count);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return PartialView("~/Views/RutasDiarias/_RoutesTablePartial.cshtml", data);
                return View(data);
            }
        }

        /// <summary>
        /// Muestra el registros de rutas del dia actual para el CEF, con opciones de filtrado y paginación. 
        /// </summary>
        /// <param name="fechaEjecucion">Filtro por una fecha seleccionada (valor por defecto dia actual).</param>
        /// <param name="codSuc">Filtro por ID de sucursal.</param>
        /// <param name="estado">Estado de la ruta generada.</param>
        /// <param name="search">Término de búsqueda por nombre de la ruta.</param>
        /// <param name="page">Número de página actual.</param>
        /// <param name="pageSize">Número de elementos por página.</param>
        /// <returns>La vista de las rutas, con datos paginados.</returns>
        [HttpGet("CefDashboard")]
        [RequiredPermission(PermissionType.View, "RUDCE")]
        public async Task<IActionResult> CefDashboard(int? page, int pageSize = 15, string search = "", DateOnly? fechaEjecucion = null, int? codSuc = null, int? estado = null)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null) return RedirectToAction("Login", "Account", new { Area = "Identity" });

                await SetCommonViewBagsAsync(currentUser, "Operations");
                var isAdmin = (bool)ViewBag.IsAdmin;

                DateOnly defaultFechaEjecucionForOperations = DateOnly.FromDateTime(DateTime.Today);
                DateOnly filterFechaEjecucion = fechaEjecucion ?? defaultFechaEjecucionForOperations;

                List<int> permittedSucursalIds = new List<int>();
                if (!isAdmin)
                {
                    permittedSucursalIds = await GetUserPermittedSucursalesAsync(currentUser.Id);
                    if (!permittedSucursalIds.Any())
                    {
                        TempData["ErrorMessage"] = "No tiene sucursales asignadas para ver rutas.";
                        return RedirectToAction("Index", "Home");
                    }
                }

                IQueryable<AdmSucursal> sucursalesQuery = _context.AdmSucursales.Where(s => s.Estado == true);

                if (!isAdmin && permittedSucursalIds.Any())
                {
                    Expression<Func<AdmSucursal, bool>> combinedPredicateSucursal = null;

                    foreach (var sucursalId in permittedSucursalIds)
                    {
                        Expression<Func<AdmSucursal, bool>> currentPredicate = s => s.CodSucursal == sucursalId;

                        if (combinedPredicateSucursal == null)
                        {
                            combinedPredicateSucursal = currentPredicate;
                        }
                        else
                        {
                            var parameter = Expression.Parameter(typeof(AdmSucursal), "s");
                            var body = Expression.OrElse(
                                Expression.Invoke(combinedPredicateSucursal, parameter),
                                Expression.Invoke(currentPredicate, parameter)
                            );
                            combinedPredicateSucursal = Expression.Lambda<Func<AdmSucursal, bool>>(body, parameter);
                        }
                    }
                    if (combinedPredicateSucursal != null)
                    {
                        sucursalesQuery = sucursalesQuery.Where(combinedPredicateSucursal);
                    }
                }
                else if (!isAdmin && !permittedSucursalIds.Any())
                {
                    sucursalesQuery = sucursalesQuery.Where(s => false);
                }

                var sucursalesForFilter = await sucursalesQuery.ToListAsync();

                ViewBag.SucursalesFilter = new SelectList(sucursalesForFilter, "CodSucursal", "NombreSucursal", codSuc);
                ViewBag.FechaEjecucionFilter = filterFechaEjecucion;
                ViewBag.EstadoFilter = estado;

                var estadosSelectItems = Enum.GetValues(typeof(EstadoRuta))
                    .Cast<EstadoRuta>()
                    .Select(e => new SelectListItem
                    {
                        Value = ((int)e).ToString(),
                        Text = e.ToString().Replace("_", " ")
                    }).ToList();
                estadosSelectItems.Insert(0, new SelectListItem { Value = "", Text = "-- Selecciona --" });
                ViewBag.EstadosFilterList = new SelectList(estadosSelectItems, "Value", "Text", estado);
                ViewBag.DefaultFechaEjecucionBasedOnRole = defaultFechaEjecucionForOperations.ToString("yyyy-MM-dd");

                IQueryable<TdvRutaDiaria> query = _context.TdvRutasDiarias
                                        .Include(r => r.UsuarioPlaneacionObj)
                                        .Include(r => r.RutaMaster)
                                        .AsQueryable();

                if (!isAdmin && permittedSucursalIds.Any())
                {
                    Expression<Func<TdvRutaDiaria, bool>> combinedPredicateRutas = null;

                    foreach (var sucursalId in permittedSucursalIds)
                    {
                        Expression<Func<TdvRutaDiaria, bool>> currentPredicate = r => r.CodSucursal == sucursalId;

                        if (combinedPredicateRutas == null)
                        {
                            combinedPredicateRutas = currentPredicate;
                        }
                        else
                        {
                            var parameter = Expression.Parameter(typeof(TdvRutaDiaria), "r");
                            var body = Expression.OrElse(
                                Expression.Invoke(combinedPredicateRutas, parameter),
                                Expression.Invoke(currentPredicate, parameter)
                            );
                            combinedPredicateRutas = Expression.Lambda<Func<TdvRutaDiaria, bool>>(body, parameter);
                        }
                    }
                    if (combinedPredicateRutas != null)
                    {
                        query = query.Where(combinedPredicateRutas);
                    }
                }
                else if (!isAdmin && !permittedSucursalIds.Any())
                {
                    query = query.Where(r => false);
                }

                if (codSuc.HasValue && codSuc.Value != 0) { query = query.Where(r => r.CodSucursal == codSuc.Value); }

                query = query.Where(r => r.FechaEjecucion == filterFechaEjecucion);

                if (!estado.HasValue && string.IsNullOrEmpty(search))
                {
                    query = query.Where(r => r.Estado == (int)EstadoRuta.PLANEADO || r.Estado == (int)EstadoRuta.CARGUE_REGISTRADO ||
                                            r.Estado == (int)EstadoRuta.SALIDA_REGISTRADA || r.Estado == (int)EstadoRuta.DESCARGUE_REGISTRADO ||
                                            r.Estado == (int)EstadoRuta.CERRADO);
                }
                else if (estado.HasValue && estado.Value != 0)
                {
                    query = query.Where(r => r.Estado == estado.Value);
                }

                if (!string.IsNullOrEmpty(search))
                {
                    string trimmedSearch = search.Trim().ToLower();
                    query = query.Where(r => r.NombreRuta.ToLower().Contains(trimmedSearch) ||
                                             r.Id.ToLower().Contains(trimmedSearch) ||
                                             r.NombreSucursal.ToLower().Contains(trimmedSearch) ||
                                             (r.NombreJT != null && r.NombreJT.ToLower().Contains(trimmedSearch)) ||
                                             (r.NombreConductor != null && r.NombreConductor.ToLower().Contains(trimmedSearch)));
                }
                query = query.OrderByDescending(r => r.FechaEjecucion).ThenBy(r => r.Id);

                var totalData = await query.CountAsync();
                page = Math.Max(page ?? 1, 1); pageSize = 15;
                int totalPages = (int)Math.Ceiling((double)totalData / pageSize);
                page = Math.Min(page.Value, Math.Max(1, totalPages));
                var data = await query.Skip((page.Value - 1) * pageSize).Take(pageSize).ToListAsync();

                ViewBag.CurrentPage = page.Value; ViewBag.TotalPages = totalPages; ViewBag.TotalData = totalData;
                ViewBag.SearchTerm = search; ViewBag.PageSize = pageSize; ViewBag.CurrentFechaEjecucion = filterFechaEjecucion.ToString("yyyy-MM-dd");
                ViewBag.CurrentCodSuc = codSuc; ViewBag.CurrentEstado = estado;

                ViewBag.canEditPlaneador = (bool)ViewBag.IsAdmin || ((bool)ViewBag.canEditPlaneador);
                ViewBag.canCargarEfectivoCef = (bool)ViewBag.IsAdmin || ((bool)ViewBag.canCargarEfectivoCef);
                ViewBag.canDescargarEfectivoCef = (bool)ViewBag.IsAdmin || ((bool)ViewBag.canDescargarEfectivoCef);
                ViewBag.canRegistrarSalidaSupervisor = (bool)ViewBag.IsAdmin || ((bool)ViewBag.canRegistrarSalidaSupervisor);
                ViewBag.canRegistrarEntradaSupervisor = (bool)ViewBag.IsAdmin || ((bool)ViewBag.canRegistrarEntradaSupervisor);

                Log.Information("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a CEF Dashboard | Cantidad de rutas: {Count} | Respuesta: Acceso permitido | ", currentUser.UserName, ipAddress, data.Count);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return PartialView("~/Views/RutasDiarias/_OperationsTablePartial.cshtml", data);
                return View(data);
            }
        }

        /// <summary>
        /// Muestra el registros de rutas del dia actual para el SupervisorRutas, con opciones de filtrado y paginación. 
        /// </summary>
        /// <param name="fechaEjecucion">Filtro por una fecha seleccionada (valor por defecto dia actual).</param>
        /// <param name="codSuc">Filtro por ID de sucursal.</param>
        /// <param name="estado">Estado de la ruta generada.</param>
        /// <param name="search">Término de búsqueda por nombre de la ruta.</param>
        /// <param name="page">Número de página actual.</param>
        /// <param name="pageSize">Número de elementos por página.</param>
        /// <returns>La vista de las rutas, con datos paginados.</returns>
        [HttpGet("OperationsDashboard")]
        [RequiredPermission(PermissionType.View, "RUDOP")]
        public async Task<IActionResult> OperationsDashboard(int? page, int pageSize = 15, string search = "", DateOnly? fechaEjecucion = null, int? codSuc = null, int? estado = null)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null) return RedirectToAction("Login", "Account", new { Area = "Identity" });

                await SetCommonViewBagsAsync(currentUser, "Operations");
                var isAdmin = (bool)ViewBag.IsAdmin;

                DateOnly defaultFechaEjecucionForOperations = DateOnly.FromDateTime(DateTime.Today);
                DateOnly filterFechaEjecucion = fechaEjecucion ?? defaultFechaEjecucionForOperations;

                List<int> permittedSucursalIds = new List<int>();
                if (!isAdmin)
                {
                    permittedSucursalIds = await GetUserPermittedSucursalesAsync(currentUser.Id);
                    if (!permittedSucursalIds.Any())
                    {
                        TempData["ErrorMessage"] = "No tiene sucursales asignadas para ver rutas.";
                        return RedirectToAction("Index", "Home");
                    }
                }

                IQueryable<AdmSucursal> sucursalesQuery = _context.AdmSucursales.Where(s => s.Estado == true);

                if (!isAdmin && permittedSucursalIds.Any())
                {
                    Expression<Func<AdmSucursal, bool>> combinedPredicateSucursal = null;

                    foreach (var sucursalId in permittedSucursalIds)
                    {
                        Expression<Func<AdmSucursal, bool>> currentPredicate = s => s.CodSucursal == sucursalId;

                        if (combinedPredicateSucursal == null)
                        {
                            combinedPredicateSucursal = currentPredicate;
                        }
                        else
                        {
                            var parameter = Expression.Parameter(typeof(AdmSucursal), "s");
                            var body = Expression.OrElse(
                                Expression.Invoke(combinedPredicateSucursal, parameter),
                                Expression.Invoke(currentPredicate, parameter)
                            );
                            combinedPredicateSucursal = Expression.Lambda<Func<AdmSucursal, bool>>(body, parameter);
                        }
                    }
                    if (combinedPredicateSucursal != null)
                    {
                        sucursalesQuery = sucursalesQuery.Where(combinedPredicateSucursal);
                    }
                }
                else if (!isAdmin && !permittedSucursalIds.Any())
                {
                    sucursalesQuery = sucursalesQuery.Where(s => false);
                }

                var sucursalesForFilter = await sucursalesQuery.ToListAsync();

                ViewBag.SucursalesFilter = new SelectList(sucursalesForFilter, "CodSucursal", "NombreSucursal", codSuc);
                ViewBag.FechaEjecucionFilter = filterFechaEjecucion;
                ViewBag.EstadoFilter = estado;

                var estadosSelectItems = Enum.GetValues(typeof(EstadoRuta))
                    .Cast<EstadoRuta>()
                    .Select(e => new SelectListItem
                    {
                        Value = ((int)e).ToString(),
                        Text = e.ToString().Replace("_", " ")
                    }).ToList();
                estadosSelectItems.Insert(0, new SelectListItem { Value = "", Text = "-- Selecciona --" });
                ViewBag.EstadosFilterList = new SelectList(estadosSelectItems, "Value", "Text", estado);
                ViewBag.DefaultFechaEjecucionBasedOnRole = defaultFechaEjecucionForOperations.ToString("yyyy-MM-dd");

                IQueryable<TdvRutaDiaria> query = _context.TdvRutasDiarias
                                        .Include(r => r.UsuarioPlaneacionObj)
                                        .AsQueryable();

                if (!isAdmin && permittedSucursalIds.Any())
                {
                    Expression<Func<TdvRutaDiaria, bool>> combinedPredicateRutas = null;

                    foreach (var sucursalId in permittedSucursalIds)
                    {
                        Expression<Func<TdvRutaDiaria, bool>> currentPredicate = r => r.CodSucursal == sucursalId;

                        if (combinedPredicateRutas == null)
                        {
                            combinedPredicateRutas = currentPredicate;
                        }
                        else
                        {
                            var parameter = Expression.Parameter(typeof(TdvRutaDiaria), "r");
                            var body = Expression.OrElse(
                                Expression.Invoke(combinedPredicateRutas, parameter),
                                Expression.Invoke(currentPredicate, parameter)
                            );
                            combinedPredicateRutas = Expression.Lambda<Func<TdvRutaDiaria, bool>>(body, parameter);
                        }
                    }
                    if (combinedPredicateRutas != null)
                    {
                        query = query.Where(combinedPredicateRutas);
                    }
                }
                else if (!isAdmin && !permittedSucursalIds.Any())
                {
                    query = query.Where(r => false);
                }

                if (codSuc.HasValue && codSuc.Value != 0) { query = query.Where(r => r.CodSucursal == codSuc.Value); }

                query = query.Where(r => r.FechaEjecucion == filterFechaEjecucion);

                if (!estado.HasValue && string.IsNullOrEmpty(search))
                {
                    query = query.Where(r => r.Estado == (int)EstadoRuta.GENERADO || 
                    r.Estado == (int)EstadoRuta.PLANEADO || r.Estado == (int)EstadoRuta.CARGUE_REGISTRADO || 
                    r.Estado == (int)EstadoRuta.SALIDA_REGISTRADA || r.Estado == (int)EstadoRuta.DESCARGUE_REGISTRADO ||
                    r.Estado == (int)EstadoRuta.CERRADO);
                }
                else if (estado.HasValue && estado.Value != 0)
                {
                    query = query.Where(r => r.Estado == estado.Value);
                }

                if (!string.IsNullOrEmpty(search))
                {
                    string trimmedSearch = search.Trim().ToLower();
                    query = query.Where(r => r.NombreRuta.ToLower().Contains(trimmedSearch) ||
                                             r.Id.ToLower().Contains(trimmedSearch) ||
                                             r.NombreSucursal.ToLower().Contains(trimmedSearch) ||
                                             (r.NombreJT != null && r.NombreJT.ToLower().Contains(trimmedSearch)) ||
                                             (r.NombreConductor != null && r.NombreConductor.ToLower().Contains(trimmedSearch)));
                }
                query = query.OrderByDescending(r => r.FechaEjecucion).ThenBy(r => r.Id);

                var totalData = await query.CountAsync();
                page = Math.Max(page ?? 1, 1); pageSize = 15;
                int totalPages = (int)Math.Ceiling((double)totalData / pageSize);
                page = Math.Min(page.Value, Math.Max(1, totalPages));
                var data = await query.Skip((page.Value - 1) * pageSize).Take(pageSize).ToListAsync();

                ViewBag.CurrentPage = page.Value; ViewBag.TotalPages = totalPages; ViewBag.TotalData = totalData; 
                ViewBag.SearchTerm = search; ViewBag.PageSize = pageSize; ViewBag.CurrentFechaEjecucion = filterFechaEjecucion.ToString("yyyy-MM-dd"); 
                ViewBag.CurrentCodSuc = codSuc; ViewBag.CurrentEstado = estado;

                ViewBag.canEditPlaneador = (bool)ViewBag.IsAdmin || ((bool)ViewBag.canEditPlaneador);
                ViewBag.canCargarEfectivoCef = (bool)ViewBag.IsAdmin || ((bool)ViewBag.canCargarEfectivoCef);
                ViewBag.canDescargarEfectivoCef = (bool)ViewBag.IsAdmin || ((bool)ViewBag.canDescargarEfectivoCef);
                ViewBag.canRegistrarSalidaSupervisor = (bool)ViewBag.IsAdmin || ((bool)ViewBag.canRegistrarSalidaSupervisor);
                ViewBag.canRegistrarEntradaSupervisor = (bool)ViewBag.IsAdmin || ((bool)ViewBag.canRegistrarEntradaSupervisor);

                Log.Information("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a Operations Dashboard | Cantidad de rutas: {Count} | Respuesta: Acceso permitido | ", currentUser.UserName, ipAddress, data.Count);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return PartialView("~/Views/RutasDiarias/_OperationsTablePartial.cshtml", data);
                return View(data);
            }
        }

        /// <summary>
        /// Muestra el historial de rutas, con opciones de filtrado y paginación. 
        /// </summary>
        /// <param name="fechaEjecucion">Filtro por una fecha seleccionada (valor por defecto dia actual).</param>
        /// <param name="codSuc">Filtro por ID de sucursal.</param>
        /// <param name="estado">Estado de la ruta generada.</param>
        /// <param name="search">Término de búsqueda por nombre de la ruta.</param>
        /// <param name="page">Número de página actual.</param>
        /// <param name="pageSize">Número de elementos por página.</param>
        /// <returns>La vista del historial de rutas, con datos paginados.</returns>
        [HttpGet("HistorialRutas")]
        [HttpGet]
        [RequiredPermission(PermissionType.View, "RUDHIS")]
        public async Task<IActionResult> HistorialRutas(int? page, int pageSize = 15, string search = "", DateOnly? fechaEjecucion = null, int? codSuc = null, int? estado = null)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null) return RedirectToAction("Login", "Account", new { Area = "Identity" });

                await SetCommonViewBagsAsync(currentUser, "Historial");
                var isAdmin = (bool)ViewBag.IsAdmin;

                List<int> permittedSucursalIds = new List<int>();
                if (!isAdmin)
                {
                    permittedSucursalIds = await GetUserPermittedSucursalesAsync(currentUser.Id);
                    if (!permittedSucursalIds.Any())
                    {
                        TempData["ErrorMessage"] = "No tiene sucursales asignadas para ver el historial de rutas.";
                        return RedirectToAction("Index", "Home");
                    }
                }

                IQueryable<AdmSucursal> sucursalesQuery = _context.AdmSucursales.Where(s => s.Estado == true);

                if (!isAdmin && permittedSucursalIds.Any())
                {
                    Expression<Func<AdmSucursal, bool>> combinedPredicateSucursal = null;

                    foreach (var sucursalId in permittedSucursalIds)
                    {
                        Expression<Func<AdmSucursal, bool>> currentPredicate = s => s.CodSucursal == sucursalId;

                        if (combinedPredicateSucursal == null)
                        {
                            combinedPredicateSucursal = currentPredicate;
                        }
                        else
                        {
                            var parameter = Expression.Parameter(typeof(AdmSucursal), "s");
                            var body = Expression.OrElse(
                                Expression.Invoke(combinedPredicateSucursal, parameter),
                                Expression.Invoke(currentPredicate, parameter)
                            );
                            combinedPredicateSucursal = Expression.Lambda<Func<AdmSucursal, bool>>(body, parameter);
                        }
                    }
                    if (combinedPredicateSucursal != null)
                    {
                        sucursalesQuery = sucursalesQuery.Where(combinedPredicateSucursal);
                    }
                }
                else if (!isAdmin && !permittedSucursalIds.Any())
                {
                    sucursalesQuery = sucursalesQuery.Where(s => false);
                }

                var sucursalesForFilter = await sucursalesQuery.ToListAsync();
                ViewBag.SucursalesFilter = new SelectList(sucursalesForFilter, "CodSucursal", "NombreSucursal", codSuc);
                ViewBag.EstadoFilter = estado;

                var estadosSelectItems = Enum.GetValues(typeof(EstadoRuta))
                    .Cast<EstadoRuta>()
                    .Select(e => new SelectListItem
                    {
                        Value = ((int)e).ToString(),
                        Text = e.ToString().Replace("_", " ")
                    }).ToList();
                estadosSelectItems.Insert(0, new SelectListItem { Value = "", Text = "-- Selecciona --" });
                ViewBag.EstadosFilterList = new SelectList(estadosSelectItems, "Value", "Text", estado);

                IQueryable<TdvRutaDiaria> query = _context.TdvRutasDiarias.Include(r => r.UsuarioPlaneacionObj).AsQueryable();

                if (!isAdmin && permittedSucursalIds.Any())
                {
                    Expression<Func<TdvRutaDiaria, bool>> combinedPredicateRutas = null;

                    foreach (var sucursalId in permittedSucursalIds)
                    {
                        Expression<Func<TdvRutaDiaria, bool>> currentPredicate = r => r.CodSucursal == sucursalId;

                        if (combinedPredicateRutas == null)
                        {
                            combinedPredicateRutas = currentPredicate;
                        }
                        else
                        {
                            var parameter = Expression.Parameter(typeof(TdvRutaDiaria), "r");
                            var body = Expression.OrElse(
                                Expression.Invoke(combinedPredicateRutas, parameter),
                                Expression.Invoke(currentPredicate, parameter)
                            );
                            combinedPredicateRutas = Expression.Lambda<Func<TdvRutaDiaria, bool>>(body, parameter);
                        }
                    }
                    if (combinedPredicateRutas != null)
                    {
                        query = query.Where(combinedPredicateRutas);
                    }
                }
                else if (!isAdmin && !permittedSucursalIds.Any())
                {
                    query = query.Where(r => false);
                }

                if (codSuc.HasValue && codSuc.Value != 0) { query = query.Where(r => r.CodSucursal == codSuc.Value); }
                if (fechaEjecucion.HasValue) { query = query.Where(r => r.FechaEjecucion == fechaEjecucion.Value); }
                if (estado.HasValue && estado.Value != 0) { query = query.Where(r => r.Estado == estado.Value); }

                if (!string.IsNullOrEmpty(search))
                {
                    string trimmedSearch = search.Trim().ToLower();
                    query = query.Where(r => r.NombreRuta.ToLower().Contains(trimmedSearch) ||
                                             r.Id.ToLower().Contains(trimmedSearch) ||
                                             r.NombreSucursal.ToLower().Contains(trimmedSearch) ||
                                             (r.NombreJT != null && r.NombreJT.ToLower().Contains(trimmedSearch)) ||
                                             (r.NombreConductor != null && r.NombreConductor.ToLower().Contains(trimmedSearch)));
                }
                query = query.OrderByDescending(r => r.FechaEjecucion).ThenBy(r => r.Id);

                var totalData = await query.CountAsync();
                page = Math.Max(page ?? 1, 1); pageSize = 15;

                int totalPages = (int)Math.Ceiling((double)totalData / pageSize);
                page = Math.Min(page.Value, Math.Max(1, totalPages));
                var data = await query.Skip((page.Value - 1) * pageSize).Take(pageSize).ToListAsync();

                ViewBag.CurrentPage = page.Value; ViewBag.TotalPages = totalPages; ViewBag.TotalData = totalData; 
                ViewBag.SearchTerm = search; ViewBag.PageSize = pageSize; ViewBag.CurrentCodSuc = codSuc; ViewBag.CurrentEstado = estado;

                Log.Information("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a Historial de Rutas | Cantidad de rutas: {Count} | Respuesta: Acceso permitido | ", currentUser.UserName, ipAddress, data.Count);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return PartialView("~/Views/RutasDiarias/_RoutesHistoryTablePartial.cshtml", data);
                return View(data);
            }
        }

        /// <summary>
        /// Generador de rutas por estado activo y por sucursal/les asignada/s al usuario.
        /// </summary>
        [HttpPost("GenerateRoutesForTomorrow")]
        [RequiredPermission(PermissionType.Create, "RUDPL")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateRoutesForTomorrow()
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null)
                {
                    Log.Warning("| Usuario: No autenticado | Ip: {Ip} | Acción: Intento de Generar Rutas | Respuesta: No autorizado (no logueado) |", ipAddress);
                    return Unauthorized();
                }

                bool isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
                IList<string> userRoles = await _userManager.GetRolesAsync(currentUser);
                var currentCodPerfil = userRoles.FirstOrDefault();

                if (!isAdmin && currentCodPerfil != "Planeador")
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Intento de Generar Rutas | Resultado: Acceso denegado (no Admin y no perfil Planeador explícito) |", currentUser.UserName, ipAddress);
                    TempData["ErrorMessage"] = "No tiene permiso para generar rutas.";
                    return RedirectToAction(nameof(PlannerDashboard));
                }

                DateOnly fechaEjecucion = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
                List<int> permittedSucursalIds = new List<int>();

                if (!isAdmin)
                {
                    permittedSucursalIds = await GetUserPermittedSucursalesAsync(currentUser.Id);
                    if (!permittedSucursalIds.Any())
                    {
                        Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Intento de Generar Rutas | Resultado: Acceso denegado por falta de sucursales permitidas |", currentUser.UserName, ipAddress);
                        TempData["ErrorMessage"] = "No tiene sucursales asignadas para generar rutas.";
                        return RedirectToAction(nameof(PlannerDashboard));
                    }
                }
                else
                {
                    permittedSucursalIds = await _context.AdmSucursales.Where(s => s.Estado == true).Select(s => s.CodSucursal).ToListAsync();
                    if (!permittedSucursalIds.Any())
                    {
                        Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Intento de Generar Rutas (Admin) | Resultado: No hay sucursales activas en el sistema |", currentUser.UserName, ipAddress);
                        TempData["ErrorMessage"] = "No hay sucursales activas en el sistema para generar rutas.";
                        return RedirectToAction(nameof(PlannerDashboard));
                    }
                }

                int totalRutasCreadas = 0;
                int totalRutasOmitidas = 0;

                try
                {
                    foreach (var codSuc in permittedSucursalIds)
                    {
                        GeneracionRutasDiariasResult resultadoSucursal = await _rutaDiariaService.GenerarRutasDiariasInicialesPorSucursalAsync(
                            codSuc,
                            fechaEjecucion,
                            currentUser.Id,
                            currentUser.UserName
                        );
                        totalRutasCreadas += resultadoSucursal.RutasCreadas;
                        totalRutasOmitidas += resultadoSucursal.RutasOmitidas;
                    }

                    string mensajeFinal = "";
                    if (totalRutasCreadas > 0)
                    {
                        mensajeFinal += $"Se han generado {totalRutasCreadas} rutas para el {fechaEjecucion.ToShortDateString()} exitosamente.";
                    }
                    if (totalRutasOmitidas > 0)
                    {
                        if (totalRutasCreadas > 0) mensajeFinal += " ";
                        mensajeFinal += $"Se omitieron {totalRutasOmitidas} rutas que ya existían para esa fecha.";
                    }

                    if (totalRutasCreadas == 0 && totalRutasOmitidas == 0)
                    {
                        mensajeFinal = "No se encontraron rutas maestras activas para generar o no hay sucursales asignadas al usuario para esa fecha.";
                        TempData["ErrorMessage"] = mensajeFinal;
                    }
                    else if (totalRutasCreadas > 0 && totalRutasOmitidas > 0)
                    {
                        TempData["InfoMessage"] = mensajeFinal;
                    }
                    else
                    {
                        TempData["SuccessMessage"] = mensajeFinal;
                    }

                    Log.Information("| Usuario: {User} | Ip: {Ip} | Acción: Generar Rutas Completada | Creadas: {Creadas} | Omitidas: {Omitidas} | Fecha: {Fecha} |", currentUser.UserName, ipAddress, totalRutasCreadas, totalRutasOmitidas, fechaEjecucion);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "| Usuario: {User} | Ip: {Ip} | Acción: Error al Generar Rutas | Mensaje: {ErrorMessage} |", currentUser.UserName, ipAddress, ex.Message);
                    TempData["ErrorMessage"] = $"Error al generar rutas: {ex.Message}";
                }

                return RedirectToAction(nameof(PlannerDashboard));
            }
        }

        // --- Método Auxiliar para cargar las listas desplegables para la vista Create ---
        private async Task LoadCreateViewBags(ApplicationUser currentUser)
        {
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            List<int> permittedSucursalIds = new List<int>();

            if (!isAdmin)
            {
                permittedSucursalIds = await GetUserPermittedSucursalesAsync(currentUser.Id);
            }

            IQueryable<AdmSucursal> sucursalesQuery = _context.AdmSucursales.Where(s => s.Estado == true);

            if (!isAdmin && permittedSucursalIds.Any())
            {
                Expression<Func<AdmSucursal, bool>> combinedPredicateSucursal = null;
                foreach (var sucursalId in permittedSucursalIds)
                {
                    Expression<Func<AdmSucursal, bool>> currentPredicate = s => s.CodSucursal == sucursalId;
                    combinedPredicateSucursal = combinedPredicateSucursal == null ? currentPredicate : Expression.Lambda<Func<AdmSucursal, bool>>(Expression.OrElse(Expression.Invoke(combinedPredicateSucursal, currentPredicate.Parameters.Single()), Expression.Invoke(currentPredicate, currentPredicate.Parameters.Single())), currentPredicate.Parameters);
                }
                if (combinedPredicateSucursal != null)
                {
                    sucursalesQuery = sucursalesQuery.Where(combinedPredicateSucursal);
                }
            }
            else if (!isAdmin && !permittedSucursalIds.Any())
            {
                sucursalesQuery = sucursalesQuery.Where(s => false); // No mostrar ninguna si no hay permisos
            }

            var sucursales = await sucursalesQuery.OrderBy(s => s.NombreSucursal).ToListAsync();
            ViewBag.Sucursales = new SelectList(sucursales, "CodSucursal", "NombreSucursal");

            // Las rutas maestras se cargarán dinámicamente vía AJAX después de seleccionar la sucursal
            ViewBag.RutasMaestras = new SelectList(new List<SelectListItem>(), "Value", "Text");
        }

        /// <summary>
        /// Muestra el formulario para crear una nueva ruta diaria individualmente.
        /// </summary>
        /// <remarks>
        /// Requiere el permiso de 'Create' para la vista 'RUD'.
        /// </remarks>
        /// <returns>La vista del formulario de creación.</returns>
        [HttpGet("Create")]
        [RequiredPermission(PermissionType.Create, "RUDPL")]
        public async Task<IActionResult> Create()
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null) return RedirectToAction("Login", "Account", new { Area = "Identity" });

                await SetCommonViewBagsAsync(currentUser, "Crear Ruta Diaria");
                var isAdmin = (bool)ViewBag.IsAdmin;
                var currentCodPerfil = ViewBag.CurrentCodPerfil as string;

                if (!isAdmin && currentCodPerfil != "Planeador")
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Intento de acceso a Crear Ruta Diaria | Respuesta: Acceso denegado (no Admin o no Planeador) | ", currentUser.UserName, ipAddress);
                    TempData["ErrorMessage"] = "No tiene permiso para crear rutas con su perfil.";
                    return RedirectToAction(nameof(PlannerDashboard));
                }

                await LoadCreateViewBags(currentUser);

                var dto = new RutaDiariaCreationDto
                {
                    FechaEjecucion = DateOnly.FromDateTime(DateTime.Now.AddDays(1))
                };

                Log.Information("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a /RutasDiarias/Create (GET) | Respuesta: Acceso permitido | ", currentUser.UserName, ipAddress);
                return View(dto); // Pasamos el DTO a la vista
            }
        }

        /// <summary>
        /// Procesa la creación de una nueva ruta diaria individual.
        /// </summary>
        /// <param name="dto">El objeto RutaDiariaCreationDto con los datos del formulario.</param>
        /// <returns>Redirección a la lista de rutas generadas o la misma vista con errores.</returns>
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Create, "RUDPL")]
        public async Task<IActionResult> Create([FromForm] RutaDiariaCreationDto dto) // Aceptamos el DTO
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null) return Unauthorized();

                await SetCommonViewBagsAsync(currentUser, "Crear Ruta Diaria");
                var isAdmin = (bool)ViewBag.IsAdmin;
                var currentCodPerfil = ViewBag.CurrentCodPerfil as string;

                if (!isAdmin && currentCodPerfil != "Planeador")
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Intento de POST Crear Ruta Diaria | Respuesta: Acceso denegado (no Admin o no Planeador) | ", currentUser.UserName, ipAddress);
                    TempData["ErrorMessage"] = "No tiene permiso para crear rutas con su perfil.";
                    return RedirectToAction(nameof(PlannerDashboard));
                }

                if (dto.FechaEjecucion < DateOnly.FromDateTime(DateTime.Today))
                {
                    ModelState.AddModelError("FechaEjecucion", "La fecha de ejecución no puede ser una fecha pasada.");
                }

                AdmRuta rutaMaster = null;
                if (!string.IsNullOrEmpty(dto.CodRutaSuc))
                {
                    rutaMaster = await _context.AdmRutas
                                               .Where(rm => rm.CodRutaSuc == dto.CodRutaSuc && rm.EstadoRuta == true)
                                               .FirstOrDefaultAsync();
                    if (rutaMaster == null)
                    {
                        ModelState.AddModelError("CodRutaSuc", "La Ruta Maestra seleccionada no es válida o no está activa.");
                    }
                }

                AdmSucursal sucursal = null;
                if (dto.CodSucursal > 0)
                {
                    sucursal = await _context.AdmSucursales
                                             .Where(s => s.CodSucursal == dto.CodSucursal && s.Estado == true)
                                             .FirstOrDefaultAsync();
                    if (sucursal == null)
                    {
                        ModelState.AddModelError("CodSucursal", "La Sucursal seleccionada no es válida o no está activa.");
                    }
                }

                var permittedSucursales = await GetUserPermittedSucursalesAsync(currentUser.Id);
                if (!isAdmin && dto.CodSucursal > 0 && !permittedSucursales.Contains(dto.CodSucursal))
                {
                    ModelState.AddModelError("CodSucursal", "No tiene permiso para crear rutas en esta sucursal.");
                }

                if (ModelState.IsValid) // Solo si las validaciones previas pasaron
                {
                    bool rutaYaExiste = await _context.TdvRutasDiarias
                        .AnyAsync(r => r.CodRutaSuc == dto.CodRutaSuc &&
                                       r.CodSucursal == dto.CodSucursal &&
                                       r.FechaEjecucion == dto.FechaEjecucion);

                    if (rutaYaExiste)
                    {
                        ModelState.AddModelError("", "Ya existe una ruta diaria con la Ruta Maestra, Sucursal y Fecha de Ejecución seleccionadas.");
                        Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Intento de crear ruta duplicada | Ruta: {Ruta}, Sucursal: {Sucursal}, Fecha: {Fecha} | Respuesta: Duplicado detectado | ",
                            currentUser.UserName, ipAddress, dto.CodRutaSuc, dto.CodSucursal, dto.FechaEjecucion);
                    }
                }

                if (!ModelState.IsValid)
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/Create | Respuesta: Fallo de validación de DTO | ", currentUser.UserName, ipAddress);
                    foreach (var entry in ModelState)
                    {
                        if (entry.Value.ValidationState == ModelValidationState.Invalid)
                        {
                            foreach (var error in entry.Value.Errors)
                            {
                                Log.Warning(" --- Error de Validación: Campo = {FieldName}, Error = {ErrorMessage}", entry.Key, error.ErrorMessage);
                            }
                        }
                    }
                    await LoadCreateViewBags(currentUser); 
                    return View(dto);
                }


                var newRutaEntity = new TdvRutaDiaria
                {
                    CodRutaSuc = dto.CodRutaSuc,
                    FechaEjecucion = dto.FechaEjecucion,
                    CodSucursal = dto.CodSucursal,
                    NombreRuta = rutaMaster.NombreRuta,
                    TipoRuta = rutaMaster.TipoRuta,
                    TipoVehiculo = rutaMaster.TipoVehiculo,
                    NombreSucursal = sucursal.NombreSucursal,
                    FechaPlaneacion = DateOnly.FromDateTime(DateTime.Now),
                    HoraPlaneacion = TimeOnly.FromDateTime(DateTime.Now),
                    UsuarioPlaneacion = currentUser.Id,
                    Estado = (int)EstadoRuta.GENERADO,
                    CodVehiculo = null,
                    CedulaJT = null,
                    NombreJT = null,
                    CodCargoJT = null,
                    FechaIngresoJT = null,
                    HoraIngresoJT = null,
                    FechaSalidaJT = null,
                    HoraSalidaJT = null,
                    CedulaConductor = null,
                    NombreConductor = null,
                    CodCargoConductor = null,
                    CedulaTripulante = null,
                    NombreTripulante = null,
                    CodCargoTripulante = null,
                    FechaCargue = null,
                    HoraCargue = null,
                    CantBolsaBilleteEntrega = null,
                    CantBolsaMonedaEntrega = null,
                    UsuarioCEFCargue = null,
                    FechaDescargue = null,
                    HoraDescargue = null,
                    CantBolsaBilleteRecibe = null,
                    CantBolsaMonedaRecibe = null,
                    UsuarioCEFDescargue = null,
                    KmInicial = null,
                    FechaSalidaRuta = null,
                    HoraSalidaRuta = null,
                    UsuarioSupervisorApertura = null,
                    KmFinal = null,
                    FechaEntradaRuta = null,
                    HoraEntradaRuta = null,
                    UsuarioSupervisorCierre = null
                };

                try
                {
                    var nuevaRuta = await _rutaDiariaService.CrearRutaDiariaInicialAsync(newRutaEntity);
                    TempData["SuccessMessage"] = $"Ruta diaria '{nuevaRuta.NombreRuta}' (ID: {nuevaRuta.Id}) generada exitosamente.";
                    Log.Information("| Usuario: {User} | Ip: {Ip} | Acción: Ruta diaria creada individualmente | RutaId: {RutaId} | Estado: GENERADO | ", currentUser.UserName, ipAddress, nuevaRuta.Id);
                    return RedirectToAction(nameof(PlannerDashboard));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/Create | Respuesta: Error de operación inválida: {ErrorMessage} | ", currentUser.UserName, ipAddress, ex.Message);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error inesperado al crear una ruta diaria para el usuario {User}.", currentUser.UserName);
                    ModelState.AddModelError("", $"Error inesperado al crear la ruta: {ex.Message}");
                }

                await LoadCreateViewBags(currentUser);
                return View(dto);
            }
        }

        /// <summary>   
        /// Muestra el formulario para que el planeador pueda asignar un vehiculo, JT, Conductor y tripulante a la ruta.
        /// </summary>
        /// <param name="id">El ID del registro de la ruta.</param>
        /// <returns>La vista del formulario de asignacion de vehiculo y tripulacion.</returns>
        [HttpGet("Edit/{id}")]
        [RequiredPermission(PermissionType.Edit, "RUDPL")]
        public async Task<IActionResult> Edit(string id)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null) return RedirectToAction("Login", "Account", new { Area = "Identity" });

                await SetCommonViewBagsAsync(currentUser, "Edit");
                var isAdmin = (bool)ViewBag.IsAdmin;
                var currentCodPerfil = ViewBag.CurrentCodPerfil as string;

                if (!isAdmin && currentCodPerfil != "Planeador")
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Intento de acceso a Editar Ruta Diaria ({Id}) | Respuesta: Acceso denegado (no Admin o no Planeador) | ", currentUser.UserName, ipAddress, id);
                    TempData["ErrorMessage"] = "No tiene permiso para editar rutas con su perfil.";
                    return RedirectToAction(nameof(PlannerDashboard));
                }

                if (string.IsNullOrEmpty(id))
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a Editar Ruta Diaria | Respuesta: ID de ruta nulo o vacío | ", currentUser.UserName, ipAddress);
                    return NotFound();
                }

                var rutaDiaria = await _context.TdvRutasDiarias
                                               .Include(r => r.Sucursal)
                                               .Include(r => r.Vehiculo)
                                               .Include(r => r.JT)
                                               .Include(r => r.Conductor)
                                               .Include(r => r.Tripulante)
                                               .Include(r => r.CargoJTObj)
                                               .Include(r => r.CargoConductorObj)
                                               .Include(r => r.CargoTripulanteObj)
                                               .FirstOrDefaultAsync(r => r.Id == id);

                if (rutaDiaria == null || rutaDiaria.Estado != (int)EstadoRuta.GENERADO)
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a Editar Ruta Diaria ({Id}) | Respuesta: Ruta no encontrada o no en estado GENERADO (Estado Actual: {EstadoActual}) | ", currentUser.UserName, ipAddress, id, rutaDiaria?.Estado.ToString() ?? "NULL");
                    TempData["ErrorMessage"] = "Ruta no encontrada o no está en el estado correcto para edición por el planeador.";
                    return RedirectToAction(nameof(PlannerDashboard));
                }

                var permittedSucursales = await GetUserPermittedSucursalesAsync(currentUser.Id);
                if (!isAdmin && !permittedSucursales.Contains(rutaDiaria.CodSucursal))
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a Editar Ruta Diaria ({Id}) de sucursal no permitida ({SucId}) | Respuesta: Acceso denegado. | ", currentUser.UserName, ipAddress, id, rutaDiaria.CodSucursal);
                    return Forbid();
                }

                await LoadEditViewBags(rutaDiaria, currentUser, permittedSucursales);

                Log.Information("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a /RutasDiarias/Edit/{Id} | Respuesta: Acceso permitido | ", currentUser.UserName, ipAddress, id);
                return View(rutaDiaria);
            }
        }

        // --- Método Auxiliar para Cargar ViewBags de la Vista de Edición ---
        private async Task LoadEditViewBags(TdvRutaDiaria ruta, ApplicationUser currentUser, List<int> permittedSucursales)
        {
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            var allActiveEmployees = await _context.AdmEmpleados
                                          .Include(e => e.Cargo)
                                          .Where(e => e.EmpleadoEstado == EstadoEmpleado.Activo)
                                          .ToListAsync();

            var employeesForDropdowns = isAdmin ? allActiveEmployees :
                                                allActiveEmployees.Where(e => e.CodSucursal.HasValue && permittedSucursales.Contains(e.CodSucursal.Value)).ToList();
            var jtCargoId = 64;
            var conductorCargoId = 45;
            var tripulanteCargoId = 51;

            Func<int?, int, int, List<SelectListItem>> getEmployeeSelectList =
                (currentAssignedCedula, cargoId, routeSucursalId) =>
                {
                    var items = new List<SelectListItem>();
                    items.Insert(0, new SelectListItem { Value = "", Text = "-- Seleccione --" });

                    if (currentAssignedCedula.HasValue)
                    {
                        var assignedEmployee = allActiveEmployees.FirstOrDefault(e => e.CodCedula == currentAssignedCedula.Value);
                        if (assignedEmployee != null)
                        {
                            if (!items.Any(i => i.Value == assignedEmployee.CodCedula.ToString()))
                            {
                                items.Add(new SelectListItem
                                {
                                    Value = assignedEmployee.CodCedula.ToString(),
                                    Text = assignedEmployee.NombreCompleto ?? string.Empty,
                                    Selected = true
                                });
                            }
                        }
                    }

                    var employeesForSpecificRoleAndBranch = employeesForDropdowns
                                                            .Where(e => e.CodCargo == cargoId && e.CodSucursal.HasValue && e.CodSucursal.Value == routeSucursalId)
                                                            .OrderBy(e => e.NombreCompleto)
                                                            .Select(e => new SelectListItem { Value = e.CodCedula.ToString(), Text = e.NombreCompleto ?? string.Empty });

                    foreach (var item in employeesForSpecificRoleAndBranch)
                    {
                        if (!items.Any(i => i.Value == item.Value))
                        {
                            items.Add(item);
                        }
                    }

                    items.ForEach(item => item.Selected = (item.Value == currentAssignedCedula?.ToString()));

                    return items;
                };

            ViewBag.JefesTurno = getEmployeeSelectList(ruta.CedulaJT, jtCargoId, ruta.CodSucursal);
            ViewBag.Conductores = getEmployeeSelectList(ruta.CedulaConductor, conductorCargoId, ruta.CodSucursal);
            ViewBag.Tripulantes = getEmployeeSelectList(ruta.CedulaTripulante, tripulanteCargoId, ruta.CodSucursal);

            var tiposRutaItems = new List<SelectListItem>
            {
                new SelectListItem { Value = "T", Text = "TRADICIONAL" },
                new SelectListItem { Value = "A", Text = "ATM" },
                new SelectListItem { Value = "M", Text = "MIXTA" },
                new SelectListItem { Value = "L", Text = "LIBERACIÓN DE EFECTIVO" }
            };
            tiposRutaItems.ForEach(item => item.Selected = (item.Value == ruta.TipoRuta));
            ViewBag.TiposRuta = tiposRutaItems;


            var tiposVehiculoItems = new List<SelectListItem>
            {
                new SelectListItem { Value = "C", Text = "CAMIONETA" },
                new SelectListItem { Value = "B", Text = "BLINDADO" },
                new SelectListItem { Value = "M", Text = "MOTO" },
                new SelectListItem { Value = "T", Text = "CAMION" }
            };
            tiposVehiculoItems.ForEach(item => item.Selected = (item.Value == ruta.TipoVehiculo));
            ViewBag.TiposVehiculo = tiposVehiculoItems;
        }

        /// <summary>
        /// Registra los datos del vehiculo y tripulantes a la ruta.
        /// </summary>
        /// <param name="id">El ID del registro de la ruta.</param>
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "RUDPL")]
        public async Task<IActionResult> Edit(string id, [FromForm] TdvRutaDiaria model)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null) return Unauthorized();

                await SetCommonViewBagsAsync(currentUser, "Edit");
                var isAdmin = (bool)ViewBag.IsAdmin;
                var currentCodPerfil = ViewBag.CurrentCodPerfil as string;

                if (!isAdmin && currentCodPerfil != "Planeador")
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Intento de POST Editar Ruta Diaria ({Id}) | Respuesta: Acceso denegado (no Admin o no Planeador) | ", currentUser.UserName, ipAddress, id);
                    TempData["ErrorMessage"] = "No tiene permiso para editar rutas con su perfil.";
                    return RedirectToAction(nameof(PlannerDashboard));
                }

                if (id != model.Id)
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/Edit | Respuesta: ID de ruta en URL no coincide con ID del modelo ({IdUrl} != {IdModel}) | ", currentUser.UserName, ipAddress, id, model.Id);
                    return NotFound();
                }

                var rutaExistente = await _rutaDiariaService.ObtenerRutaDiariaPorIdAsync(id);
                if (rutaExistente == null || rutaExistente.Estado != (int)EstadoRuta.GENERADO)
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/Edit | RutaId: {Id} | Respuesta: Ruta no encontrada o no en estado GENERADO (Estado Actual: {EstadoActual}) | ", currentUser.UserName, ipAddress, id, rutaExistente?.Estado.ToString() ?? "NULL");
                    ModelState.AddModelError("", "Ruta no encontrada o no está en el estado correcto para edición por el planeador.");
                    await LoadEditViewBags(model, currentUser, await GetUserPermittedSucursalesAsync(currentUser.Id));
                    return View(model);
                }

                var permittedSucursales = await GetUserPermittedSucursalesAsync(currentUser.Id);
                if (!isAdmin && !permittedSucursales.Contains(model.CodSucursal))
                {
                    ModelState.AddModelError("CodSucursal", "No tiene permiso para editar rutas en esta sucursal.");
                }

                model.CodRutaSuc = rutaExistente.CodRutaSuc;
                model.NombreRuta = rutaExistente.NombreRuta;
                model.FechaPlaneacion = rutaExistente.FechaPlaneacion;
                model.HoraPlaneacion = rutaExistente.HoraPlaneacion;
                model.UsuarioPlaneacion = rutaExistente.UsuarioPlaneacion;
                model.Estado = (int)EstadoRuta.PLANEADO;


                if (model.CedulaJT.HasValue)
                {
                    var empleadoJT = await _context.AdmEmpleados.Include(e => e.Cargo).FirstOrDefaultAsync(e => e.CodCedula == model.CedulaJT.Value && e.EmpleadoEstado == EstadoEmpleado.Activo); // <-- USAR EmpleadoEstado
                    if (empleadoJT != null)
                    {
                        model.NombreJT = empleadoJT.NombreCompleto;
                        model.CodCargoJT = empleadoJT.Cargo?.CodCargo;
                    }
                    else
                    {
                        ModelState.AddModelError("CedulaJT", "El Jefe de Turno seleccionado no es válido o no está activo.");
                    }
                }
                else if (model.TipoVehiculo == "M")
                {
                    model.NombreJT = null; model.CodCargoJT = null;
                }
                else
                {
                    ModelState.AddModelError("CedulaJT", "Debe seleccionar un Jefe de Turno para este tipo de vehículo.");
                }

                if (model.CedulaConductor.HasValue)
                {
                    var empleadoConductor = await _context.AdmEmpleados.Include(e => e.Cargo).FirstOrDefaultAsync(e => e.CodCedula == model.CedulaConductor.Value && e.EmpleadoEstado == EstadoEmpleado.Activo);
                    if (empleadoConductor != null)
                    {
                        model.NombreConductor = empleadoConductor.NombreCompleto;
                        model.CodCargoConductor = empleadoConductor.Cargo?.CodCargo;
                    }
                    else
                    {
                        ModelState.AddModelError("CedulaConductor", "El Conductor seleccionado no es válido o no está activo.");
                    }
                }
                else if (model.TipoVehiculo != "M")
                {
                    ModelState.AddModelError("CedulaConductor", "Debe seleccionar un Conductor para este tipo de vehículo.");
                }


                if (model.CedulaTripulante.HasValue)
                {
                    var empleadoTripulante = await _context.AdmEmpleados.Include(e => e.Cargo).FirstOrDefaultAsync(e => e.CodCedula == model.CedulaTripulante.Value && e.EmpleadoEstado == EstadoEmpleado.Activo);
                    if (empleadoTripulante != null)
                    {
                        model.NombreTripulante = empleadoTripulante.NombreCompleto;
                        model.CodCargoTripulante = empleadoTripulante.Cargo?.CodCargo;
                    }
                    else
                    {
                        ModelState.AddModelError("CedulaTripulante", "El Tripulante seleccionado no es válido o no está activo.");
                    }
                }
                else if (model.TipoVehiculo == "B")
                {
                    ModelState.AddModelError("CedulaTripulante", "Debe seleccionar un Tripulante para este tipo de vehículo Blindado.");
                }


                if (!string.IsNullOrEmpty(model.CodVehiculo))
                {
                    var vehiculo = await _context.AdmVehiculos.FirstOrDefaultAsync(v => v.CodVehiculo == model.CodVehiculo && v.Estado == true);
                    if (vehiculo == null)
                    {
                        ModelState.AddModelError("CodVehiculo", "El vehículo seleccionado no es válido o no está activo.");
                    }
                }
                else
                {
                    ModelState.AddModelError("CodVehiculo", "Debe seleccionar un vehículo.");
                }

                ClearModelStateForLaterStages(ModelState);

                if (!ModelState.IsValid)
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/Edit | RutaId: {Id} | Respuesta: Fallo de validación de modelo | ", currentUser.UserName, ipAddress, id);
                    foreach (var entry in ModelState)
                    {
                        if (entry.Value.ValidationState == ModelValidationState.Invalid)
                        {
                            foreach (var error in entry.Value.Errors)
                            {
                                Log.Warning(" --- Error de Validación: Campo = {FieldName}, Error = {ErrorMessage}", entry.Key, error.ErrorMessage);
                            }
                        }
                    }
                    await LoadEditViewBags(model, currentUser, permittedSucursales);
                    return View(model);
                }

                try
                {
                    var success = await _rutaDiariaService.ActualizarRutaDiariaPlaneadorAsync(model);
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Ruta diaria actualizada a estado PLANEADO exitosamente.";
                        Log.Information("| Usuario: {User} | Ip: {Ip} | Acción: Ruta diaria actualizada por Planeador | RutaId: {RutaId} | Estado: PLANEADO | ", currentUser.UserName, ipAddress, model.Id);
                        return RedirectToAction(nameof(PlannerDashboard));
                    }
                    else
                    {
                        ModelState.AddModelError("", "No se pudo actualizar la ruta diaria. Verifique el estado.");
                        Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/Edit | RutaId: {Id} | Respuesta: Fallo en actualización del servicio | ", currentUser.UserName, ipAddress, id);
                    }
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/Edit | RutaId: {Id} | Respuesta: Error de operación inválida: {ErrorMessage} | ", currentUser.UserName, ipAddress, id, ex.Message);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error inesperado al actualizar la ruta {Id} para el usuario {User}.", id, currentUser.UserName);
                    ModelState.AddModelError("", $"Error inesperado al actualizar la ruta: {ex.Message}");
                }

                await LoadEditViewBags(model, currentUser, permittedSucursales);
                return View(model);
            }
        }

        /// <summary>
        /// Metodo de redireccion segun el rol del usuario Planeador/CEF/SupervisorDeRutas.
        /// </summary>
        /// <remarks>
        /// Requiere el permiso de 'View' para la vista 'RUD'.
        /// </remarks>
        /// <returns>Redireccion hacia Planner/Operations.</returns>
        /// 
        [HttpGet("DetailRUD")]
        [RequiredPermission(PermissionType.View, "RUD")]
        public async Task<IActionResult> DetailRUD(string id)
        {
            return await GetRouteDetail(id, "RUD", Url.Action(nameof(PlannerDashboard)));
        }

        /// <summary>
        /// Metodo de redireccion segun el rol del usuario Planeador/CEF/SupervisorDeRutas.
        /// </summary>
        /// <remarks>
        /// Requiere el permiso de 'View' para la vista 'RUDHIS'.
        /// </remarks>
        /// <returns>Redireccion hacia HistorialRutas.</returns>
        /// 
        [HttpGet("DetailRUDHIS")]
        [RequiredPermission(PermissionType.View, "RUDHIS")]
        public async Task<IActionResult> DetailRUDHIS(string id)
        {
            return await GetRouteDetail(id, "RUDHIS", Url.Action(nameof(HistorialRutas)));
        }

        // --- Método auxiliar para cargar los detalles de la ruta (lógica común) ---
        private async Task<IActionResult> GetRouteDetail(string id, string sourceViewName, string returnUrl = null)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null)
                {
                    Log.Warning("| Usuario: No autenticado | Ip: {Ip} | Acción: Acceso a /RutasDiarias/Detail ({Id}) desde {Source} | Respuesta: Redirigiendo a Login/Home |", ipAddress, id, sourceViewName);
                    return RedirectToAction("Login", "Account", new { Area = "Identity" });
                }

                await SetCommonViewBagsAsync(currentUser, "Detalle de Ruta");
                var isAdmin = (bool)ViewBag.IsAdmin;
                var currentCodPerfil = ViewBag.CurrentCodPerfil as string;

                if (string.IsNullOrEmpty(id))
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a Ver Ruta Diaria desde {Source} | Respuesta: ID de ruta nulo o vacío | ", currentUser.UserName, ipAddress, sourceViewName);
                    return NotFound();
                }

                var rutaDiaria = await _context.TdvRutasDiarias
                                               .Include(r => r.Sucursal)
                                               .Include(r => r.Vehiculo)
                                               .Include(r => r.JT)
                                               .Include(r => r.Conductor)
                                               .Include(r => r.Tripulante)
                                               .Include(r => r.CargoJTObj)
                                               .Include(r => r.CargoConductorObj)
                                               .Include(r => r.CargoTripulanteObj)
                                               .Include(r => r.UsuarioPlaneacionObj)
                                               .Include(r => r.UsuarioCEFCargueObj)
                                               .Include(r => r.UsuarioCEFDescargueObj)
                                               .Include(r => r.UsuarioSupervisorAperturaObj)
                                               .Include(r => r.UsuarioSupervisorCierreObj)
                                               .FirstOrDefaultAsync(r => r.Id == id);

                if (rutaDiaria == null)
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a Ver Ruta Diaria ({Id}) desde {Source} | Respuesta: Ruta no encontrada | ", currentUser.UserName, ipAddress, id, sourceViewName);
                    TempData["ErrorMessage"] = "Ruta no encontrada.";
                    // Redirigir al OperationsDashboard por defecto si la ruta no se encuentra
                    return RedirectToAction(nameof(OperationsDashboard));
                }

                var permittedSucursales = await GetUserPermittedSucursalesAsync(currentUser.Id);
                if (!isAdmin && !permittedSucursales.Contains(rutaDiaria.CodSucursal))
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a Ver Ruta Diaria ({Id}) de sucursal no permitida ({SucId}) desde {Source} | Respuesta: Acceso denegado. | ", currentUser.UserName, ipAddress, id, rutaDiaria.CodSucursal, sourceViewName);
                    return Forbid();
                }

                ViewBag.ReturnUrl = returnUrl;

                Log.Information("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a /RutasDiarias/Detail/{Id} desde {Source} | Respuesta: Acceso permitido | ", currentUser.UserName, ipAddress, id, sourceViewName);
                return View("Detail", rutaDiaria);
            }
        }

        /// <summary>
        /// Muestra el formulario para cargar el efectivo de la ruta.
        /// </summary>
        /// <remarks>
        /// Requiere el permiso de 'Edit' para la vista 'RUDCE'.
        /// La ruta debe estar en estado 'PLANEADO' para edición por el CEF.
        /// </remarks>
        /// <param name="id">El ID del registro de log a editar.</param>
        /// <returns>La vista del formulario de edición con los datos de la ruta.</returns>
        [HttpGet("CargarEfectivo/{id}")]
        [RequiredPermission(PermissionType.Edit, "RUDCE")]
        public async Task<IActionResult> CargarEfectivo(string id)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null)
                {
                    Log.Warning("| Usuario: No autenticado | Ip: {Ip} | Acción: Acceso a /RutasDiarias/CargarEfectivo/{Id} | Respuesta: Redirigiendo a Login/Home |", ipAddress, id);
                    return RedirectToAction("Login", "Account", new { Area = "Identity" });
                }

                await SetCommonViewBagsAsync(currentUser, "Cargar Efectivo");
                var isAdmin = (bool)ViewBag.IsAdmin;
                var currentCodPerfil = ViewBag.CurrentCodPerfil as string; 

                if (string.IsNullOrEmpty(id))
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a Cargar Efectivo | Respuesta: ID de ruta nulo o vacío | ", currentUser.UserName, ipAddress);
                    return NotFound();
                }

                var rutaDiaria = await _rutaDiariaService.ObtenerRutaDiariaPorIdAsync(id);

                if (rutaDiaria == null || rutaDiaria.Estado != (int)EstadoRuta.PLANEADO)
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a Cargar Efectivo ({Id}) | Respuesta: Ruta no encontrada o no en estado PLANEADO (Estado Actual: {EstadoActual}) | ", currentUser.UserName, ipAddress, id, rutaDiaria?.Estado.ToString() ?? "NULL");
                    TempData["ErrorMessage"] = "Ruta no encontrada o no está en el estado 'PLANEADO' para registrar el cargue de efectivo.";
                    return RedirectToAction(nameof(CefDashboard));
                }

                rutaDiaria.FechaCargue = DateOnly.FromDateTime(DateTime.Now);
                rutaDiaria.HoraCargue = TimeOnly.FromDateTime(DateTime.Now);

                Log.Information("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a /RutasDiarias/CargarEfectivo/{Id} | Respuesta: Acceso permitido | ", currentUser.UserName, ipAddress, id);
                return View(rutaDiaria);
            }
        }

        /// <summary>
        /// Actualiza un registro de la ruta existente, utilizado para insertar bolsas de billetes y/o monedas.
        /// </summary>
        /// <remarks>
        /// Requiere el permiso de 'Edit' para la vista 'RUDCE' (para completar el registro).
        /// </remarks>
        /// <param name="id">El ID del registro de la ruta a actualizar.</param>
        [HttpPost("CargarEfectivo/{id}")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "RUDCE")]
        public async Task<IActionResult> CargarEfectivo(string id, [FromForm] TdvRutaDiaria model)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null)
                {
                    Log.Warning("| Usuario: No autenticado | Ip: {Ip} | Acción: POST /RutasDiarias/CargarEfectivo/{Id} | Respuesta: Redirigiendo a Login/Home |", ipAddress, id);
                    return RedirectToAction("Login", "Account", new { Area = "Identity" });
                }

                await SetCommonViewBagsAsync(currentUser, "Cargar Efectivo");
                var isAdmin = (bool)ViewBag.IsAdmin;
                var currentCodPerfil = ViewBag.CurrentCodPerfil as string;

                if (id != model.Id)
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/CargarEfectivo | Respuesta: ID de ruta en URL no coincide con ID del modelo ({IdUrl} != {IdModel}) | ", currentUser.UserName, ipAddress, id, model.Id);
                    return NotFound();
                }

                var rutaExistente = await _rutaDiariaService.ObtenerRutaDiariaPorIdAsync(id);
                if (rutaExistente == null || rutaExistente.Estado != (int)EstadoRuta.PLANEADO)
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/CargarEfectivo | RutaId: {Id} | Respuesta: Ruta no encontrada o no en estado PLANEADO (Estado Actual: {EstadoActual}) | ", currentUser.UserName, ipAddress, id, rutaExistente?.Estado.ToString() ?? "NULL");
                    ModelState.AddModelError("", "Ruta no encontrada o no está en el estado 'PLANEADO' para registrar el cargue de efectivo.");
                    return View(model);
                }

                model.CodRutaSuc = rutaExistente.CodRutaSuc;
                model.NombreRuta = rutaExistente.NombreRuta;
                model.TipoRuta = rutaExistente.TipoRuta;
                model.TipoVehiculo = rutaExistente.TipoVehiculo;
                model.FechaEjecucion = rutaExistente.FechaEjecucion;
                model.CodSucursal = rutaExistente.CodSucursal;
                model.NombreSucursal = rutaExistente.NombreSucursal;
                model.FechaPlaneacion = rutaExistente.FechaPlaneacion;
                model.HoraPlaneacion = rutaExistente.HoraPlaneacion;
                model.UsuarioPlaneacion = rutaExistente.UsuarioPlaneacion;
                model.CedulaJT = rutaExistente.CedulaJT;
                model.NombreJT = rutaExistente.NombreJT;
                model.CodCargoJT = rutaExistente.CodCargoJT;
                model.CedulaConductor = rutaExistente.CedulaConductor;
                model.NombreConductor = rutaExistente.NombreConductor;
                model.CodCargoConductor = rutaExistente.CodCargoConductor;
                model.CedulaTripulante = rutaExistente.CedulaTripulante;
                model.NombreTripulante = rutaExistente.NombreTripulante;
                model.CodCargoTripulante = rutaExistente.CodCargoTripulante;

                model.FechaIngresoJT = rutaExistente.FechaIngresoJT;
                model.HoraIngresoJT = rutaExistente.HoraIngresoJT;
                model.FechaSalidaJT = rutaExistente.FechaSalidaJT;
                model.HoraSalidaJT = rutaExistente.HoraSalidaJT;

                model.KmInicial = rutaExistente.KmInicial;
                model.FechaSalidaRuta = rutaExistente.FechaSalidaRuta;
                model.HoraSalidaRuta = rutaExistente.HoraSalidaRuta;
                model.UsuarioSupervisorApertura = rutaExistente.UsuarioSupervisorApertura;
                model.KmFinal = rutaExistente.KmFinal;
                model.FechaEntradaRuta = rutaExistente.FechaEntradaRuta;
                model.HoraEntradaRuta = rutaExistente.HoraEntradaRuta;
                model.UsuarioSupervisorCierre = rutaExistente.UsuarioSupervisorCierre;
                model.UsuarioCEFCargue = currentUser.Id; 

                ClearModelStateForLaterStages(ModelState);

                if (ModelState.ContainsKey(nameof(model.Id))) { ModelState[nameof(model.Id)].Errors.Clear(); ModelState[nameof(model.Id)].ValidationState = ModelValidationState.Valid; }


                if (!ModelState.IsValid)
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/CargarEfectivo | RutaId: {Id} | Respuesta: Fallo de validación de modelo | ", currentUser.UserName, ipAddress, id);
                    foreach (var entry in ModelState)
                    {
                        if (entry.Value.ValidationState == ModelValidationState.Invalid)
                        {
                            foreach (var error in entry.Value.Errors)
                            {
                                Log.Warning(" --- Error de Validación: Campo = {FieldName}, Error = {ErrorMessage}",
                                            entry.Key, error.ErrorMessage);
                            }
                        }
                    }
                    return View(model);
                }

                try
                {
                    var success = await _rutaDiariaService.RegistrarCargueCEFAsync(
                        model.Id,
                        model.FechaCargue.Value,
                        model.HoraCargue.Value,
                        model.CantBolsaBilleteEntrega ?? 0,
                        model.CantBolsaMonedaEntrega ?? 0,
                        model.CantPlanillaEntrega ?? 0,
                        model.UsuarioCEFCargue
                    );

                    if (success)
                    {
                        TempData["SuccessMessage"] = "Cargue de efectivo registrado exitosamente.";
                        Log.Information("| Usuario: {User} | Ip: {Ip} | Acción: Cargue CEF registrado | RutaId: {RutaId} | Estado: EN_PROCESO | ", currentUser.UserName, ipAddress, model.Id);
                        return RedirectToAction(nameof(CefDashboard));
                    }
                    else
                    {
                        ModelState.AddModelError("", "No se pudo registrar el cargue de efectivo. Verifique los datos o el estado de la ruta.");
                        Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/CargarEfectivo | RutaId: {Id} | Respuesta: Fallo en el servicio de registro de cargue CEF | ", currentUser.UserName, ipAddress, id);
                    }
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/CargarEfectivo | RutaId: {Id} | Respuesta: Error de operación inválida: {ErrorMessage} | ", currentUser.UserName, ipAddress, id, ex.Message);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error inesperado al registrar el cargue de efectivo para la ruta {Id} y usuario {User}.", id, currentUser.UserName);
                    ModelState.AddModelError("", $"Error inesperado al registrar el cargue de efectivo: {ex.Message}");
                }

                return View(model);
            }
        }

        /// <summary>
        /// Muestra el formulario para registrar la salida del vehiculo a la ruta.
        /// </summary>
        /// <remarks>
        /// Requiere el permiso de 'Edit' para la vista 'RUDOP'.
        /// La ruta debe estar en estado 'CARGUE_REGISTRADO' para edición por el SupervisorRuta.
        /// </remarks>
        /// <param name="id">El ID del registro de log a editar.</param>
        /// <returns>La vista del formulario de edición con los datos de la ruta.</returns>
        [HttpGet("RegistrarSalida/{id}")]
        [RequiredPermission(PermissionType.Edit, "RUDOP")] 
        public async Task<IActionResult> RegistrarSalida(string id)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null)
                {
                    Log.Warning("| Usuario: No autenticado | Ip: {Ip} | Acción: Acceso a /RutasDiarias/RegistrarSalida/{Id} | Respuesta: Redirigiendo a Login/Home |", ipAddress, id);
                    return RedirectToAction("Login", "Account", new { Area = "Identity" });
                }

                await SetCommonViewBagsAsync(currentUser, "Registrar Salida");
                var isAdmin = (bool)ViewBag.IsAdmin;
                var currentCodPerfil = ViewBag.CurrentCodPerfil as string;

                if (string.IsNullOrEmpty(id))
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a Registrar Salida | Respuesta: ID de ruta nulo o vacío | ", currentUser.UserName, ipAddress);
                    return NotFound();
                }

                var rutaDiaria = await _context.TdvRutasDiarias
                                               .Include(r => r.Sucursal)
                                               .Include(r => r.UsuarioPlaneacionObj)
                                               .Include(r => r.JT)
                                               .Include(r => r.Conductor)
                                               .Include(r => r.Tripulante)
                                               .Include(r => r.Vehiculo)
                                               .Include(r => r.CargoJTObj)
                                               .Include(r => r.CargoConductorObj)
                                               .Include(r => r.CargoTripulanteObj)
                                               .Include(r => r.UsuarioCEFCargueObj)
                                               .FirstOrDefaultAsync(r => r.Id == id);

                if (rutaDiaria == null || rutaDiaria.Estado != (int)EstadoRuta.CARGUE_REGISTRADO)
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a Registrar Salida ({Id}) | Respuesta: Ruta no encontrada o no en estado CARGUE REGISTRADO (Estado Actual: {EstadoActual}) | ", currentUser.UserName, ipAddress, id, rutaDiaria?.Estado.ToString() ?? "NULL");
                    TempData["ErrorMessage"] = "Ruta no encontrada o no está en el estado 'CARGUE REGISTRADO' para registrar la salida del vehículo.";
                    return RedirectToAction(nameof(OperationsDashboard));
                }

                rutaDiaria.FechaSalidaRuta = DateOnly.FromDateTime(DateTime.Now);
                rutaDiaria.HoraSalidaRuta = TimeOnly.FromDateTime(DateTime.Now);

                Log.Information("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a /RutasDiarias/RegistrarSalida/{Id} | Respuesta: Acceso permitido | ", currentUser.UserName, ipAddress, id);
                return View(rutaDiaria);
            }
        }

        /// <summary>
        /// Actualiza un registro de la ruta existente, utilizado para insertar KM inicial del vehiculo.
        /// </summary>
        /// <remarks>
        /// Requiere el permiso de 'Edit' para la vista 'RUDOP' (para completar el registro).
        /// </remarks>
        /// <param name="id">El ID del registro de la ruta a actualizar.</param>
        [HttpPost("RegistrarSalida/{id}")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "RUDOP")]
        public async Task<IActionResult> RegistrarSalida(string id, [FromForm] TdvRutaDiaria model)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null)
                {
                    Log.Warning("| Usuario: No autenticado | Ip: {Ip} | Acción: POST /RutasDiarias/RegistrarSalida/{Id} | Respuesta: Redirigiendo a Login/Home |", ipAddress, id);
                    return RedirectToAction("Login", "Account", new { Area = "Identity" });
                }

                await SetCommonViewBagsAsync(currentUser, "Registrar Salida");
                var isAdmin = (bool)ViewBag.IsAdmin;
                var currentCodPerfil = ViewBag.CurrentCodPerfil as string;

                if (id != model.Id)
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/RegistrarSalida | Respuesta: ID de ruta en URL no coincide con ID del modelo ({IdUrl} != {IdModel}) | ", currentUser.UserName, ipAddress, id, model.Id);
                    return NotFound();
                }

                var rutaExistente = await _rutaDiariaService.ObtenerRutaDiariaPorIdAsync(id);
                if (rutaExistente == null || rutaExistente.Estado != (int)EstadoRuta.CARGUE_REGISTRADO)
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/RegistrarSalida | RutaId: {Id} | Respuesta: Ruta no encontrada o no en estado CARGUE REGISTRADO (Estado Actual: {EstadoActual}) | ", currentUser.UserName, ipAddress, id, rutaExistente?.Estado.ToString() ?? "NULL");
                    ModelState.AddModelError("", "Ruta no encontrada o no está en el estado 'CARGUE REGISTRADO' para registrar la salida del vehículo.");
                    return View(model);
                }

                model.CodRutaSuc = rutaExistente.CodRutaSuc; model.NombreRuta = rutaExistente.NombreRuta;
                model.TipoRuta = rutaExistente.TipoRuta; model.TipoVehiculo = rutaExistente.TipoVehiculo;
                model.FechaEjecucion = rutaExistente.FechaEjecucion; model.CodSucursal = rutaExistente.CodSucursal;
                model.NombreSucursal = rutaExistente.NombreSucursal; model.FechaPlaneacion = rutaExistente.FechaPlaneacion;
                model.HoraPlaneacion = rutaExistente.HoraPlaneacion; model.UsuarioPlaneacion = rutaExistente.UsuarioPlaneacion;
                model.CodVehiculo = rutaExistente.CodVehiculo;
                model.CedulaJT = rutaExistente.CedulaJT; model.NombreJT = rutaExistente.NombreJT; model.CodCargoJT = rutaExistente.CodCargoJT;
                model.CedulaConductor = rutaExistente.CedulaConductor; model.NombreConductor = rutaExistente.NombreConductor; model.CodCargoConductor = rutaExistente.CodCargoConductor;
                model.CedulaTripulante = rutaExistente.CedulaTripulante; model.NombreTripulante = rutaExistente.NombreTripulante; model.CodCargoTripulante = rutaExistente.CodCargoTripulante;
                model.FechaIngresoJT = rutaExistente.FechaIngresoJT; model.HoraIngresoJT = rutaExistente.HoraIngresoJT;
                model.FechaSalidaJT = rutaExistente.FechaSalidaJT; model.HoraSalidaJT = rutaExistente.HoraSalidaJT;
                model.CantBolsaBilleteRecibe = rutaExistente.CantBolsaBilleteRecibe;
                model.CantBolsaMonedaRecibe = rutaExistente.CantBolsaMonedaRecibe;
                model.UsuarioCEFDescargue = rutaExistente.UsuarioCEFDescargue;
                model.KmFinal = rutaExistente.KmFinal;
                model.FechaEntradaRuta = rutaExistente.FechaEntradaRuta;
                model.HoraEntradaRuta = rutaExistente.HoraEntradaRuta;
                model.UsuarioSupervisorCierre = rutaExistente.UsuarioSupervisorCierre;
                model.UsuarioSupervisorApertura = currentUser.Id;
                model.Estado = rutaExistente.Estado;

                ClearModelStateForLaterStages(ModelState);

                if (!model.KmInicial.HasValue || model.KmInicial.Value <= 0)
                {
                    ModelState.AddModelError(nameof(model.KmInicial), "El Kilometraje Inicial es requerido y debe ser un valor positivo.");
                }
                if (!model.FechaSalidaRuta.HasValue)
                {
                    ModelState.AddModelError(nameof(model.FechaSalidaRuta), "La fecha de salida es requerida.");
                }
                if (!model.HoraSalidaRuta.HasValue)
                {
                    ModelState.AddModelError(nameof(model.HoraSalidaRuta), "La hora de salida es requerida.");
                }
                if (model.FechaSalidaRuta.HasValue && model.FechaSalidaRuta.Value > DateOnly.FromDateTime(DateTime.Today))
                {
                    ModelState.AddModelError(nameof(model.FechaSalidaRuta), "La fecha de salida de la ruta no puede ser una fecha futura.");
                }

                if (!ModelState.IsValid)
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/RegistrarSalida | RutaId: {Id} | Respuesta: Fallo de validación de modelo (controlador) | ", currentUser.UserName, ipAddress, id);
                    foreach (var entry in ModelState)
                    {
                        if (entry.Value.ValidationState == ModelValidationState.Invalid)
                        {
                            foreach (var error in entry.Value.Errors)
                            {
                                Log.Warning(" --- Error de Validación: Campo = {FieldName}, Error = {ErrorMessage}", entry.Key, error.ErrorMessage);
                            }
                        }
                    }
                    return View(model);
                }

                try
                {
                    var success = await _rutaDiariaService.RegistrarSalidaVehiculoAsync(
                        model.Id,
                        model.KmInicial.Value,
                        model.FechaSalidaRuta,
                        model.HoraSalidaRuta,
                        model.UsuarioSupervisorApertura
                    );

                    if (success)
                    {
                        TempData["SuccessMessage"] = "Salida del vehículo registrada exitosamente.";
                        Log.Information("| Usuario: {User} | Ip: {Ip} | Acción: Salida de vehículo registrada | RutaId: {RutaId} | Estado: EN_PROCESO | ", currentUser.UserName, ipAddress, model.Id);
                        return RedirectToAction(nameof(OperationsDashboard));
                    }
                    else
                    {
                        ModelState.AddModelError("", "No se pudo registrar la salida del vehículo. Verifique los datos o el estado de la ruta (posible conflicto de concurrencia).");
                        Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/RegistrarSalida | RutaId: {Id} | Respuesta: Fallo en el servicio de registro de salida | ", currentUser.UserName, ipAddress, id);
                    }
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/RegistrarSalida | RutaId: {Id} | Respuesta: Error de operación inválida (validación del servicio): {ErrorMessage} | ", currentUser.UserName, ipAddress, id, ex.Message);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error inesperado al registrar la salida del vehículo para la ruta {Id} y usuario {User}.", id, currentUser.UserName);
                    ModelState.AddModelError("", $"Error inesperado al registrar la salida del vehículo: {ex.Message}");
                }
                return View(model);
            }
        }

        /// <summary>
        /// Muestra el formulario para descargar el efectivo de la ruta.
        /// </summary>
        /// <remarks>
        /// Requiere el permiso de 'Edit' para la vista 'RUDCE'.
        /// La ruta debe estar en estado 'SALIDA_REGISTRADA' para edición por el CEF.
        /// </remarks>
        /// <param name="id">El ID del registro de log a editar.</param>
        /// <returns>La vista del formulario de edición con los datos de la ruta.</returns>
        [HttpGet("DescargarEfectivo/{id}")]
        [RequiredPermission(PermissionType.Edit, "RUDCE")]
        public async Task<IActionResult> DescargarEfectivo(string id)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null)
                {
                    Log.Warning("| Usuario: No autenticado | Ip: {Ip} | Acción: Acceso a /RutasDiarias/DescargarEfectivo/{Id} | Respuesta: Redirigiendo a Login/Home |", ipAddress, id);
                    return RedirectToAction("Login", "Account", new { Area = "Identity" });
                }

                await SetCommonViewBagsAsync(currentUser, "Descargar Efectivo");
                var isAdmin = (bool)ViewBag.IsAdmin;
                var currentCodPerfil = ViewBag.CurrentCodPerfil as string;

                if (string.IsNullOrEmpty(id))
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a Descargar Efectivo | Respuesta: ID de ruta nulo o vacío | ", currentUser.UserName, ipAddress);
                    return NotFound();
                }

                var rutaDiaria = await _context.TdvRutasDiarias
                                               .Include(r => r.Sucursal)
                                               .Include(r => r.UsuarioPlaneacionObj)
                                               .Include(r => r.JT)
                                               .Include(r => r.Conductor)
                                               .Include(r => r.Tripulante)
                                               .Include(r => r.Vehiculo)
                                               .Include(r => r.CargoJTObj)
                                               .Include(r => r.CargoConductorObj)
                                               .Include(r => r.CargoTripulanteObj)
                                               .Include(r => r.UsuarioCEFCargueObj)
                                               .Include(r => r.UsuarioSupervisorAperturaObj)
                                               .FirstOrDefaultAsync(r => r.Id == id);

                if (rutaDiaria == null || rutaDiaria.Estado != (int)EstadoRuta.SALIDA_REGISTRADA || !rutaDiaria.FechaSalidaRuta.HasValue)
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a Descargar Efectivo ({Id}) | Respuesta: Ruta no encontrada o no en estado SALIDA REGISTRADA (Estado Actual: {EstadoActual}) | ", currentUser.UserName, ipAddress, id, rutaDiaria?.Estado.ToString() ?? "NULL");
                    TempData["ErrorMessage"] = "Ruta no encontrada o no está en el estado 'SALIDA REGISTRADA' para registrar el descargue de efectivo.";
                    return RedirectToAction(nameof(OperationsDashboard));
                }

                rutaDiaria.FechaDescargue = DateOnly.FromDateTime(DateTime.Now);
                rutaDiaria.HoraDescargue = TimeOnly.FromDateTime(DateTime.Now);

                Log.Information("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a /RutasDiarias/DescargarEfectivo/{Id} | Respuesta: Acceso permitido | ", currentUser.UserName, ipAddress, id);
                return View(rutaDiaria);
            }
        }

        /// <summary>
        /// Actualiza un registro de la ruta existente, utilizado para insertar bolsas de billetes y/o monedas.
        /// </summary>
        /// <remarks>
        /// Requiere el permiso de 'Edit' para la vista 'RUDCE' (para completar el registro).
        /// </remarks>
        /// <param name="id">El ID del registro de la ruta a actualizar.</param>
        [HttpPost("DescargarEfectivo/{id}")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "RUDCE")]
        public async Task<IActionResult> DescargarEfectivo(string id, [FromForm] TdvRutaDiaria model)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null)
                {
                    Log.Warning("| Usuario: No autenticado | Ip: {Ip} | Acción: POST /RutasDiarias/DescargarEfectivo/{Id} | Respuesta: Redirigiendo a Login/Home |", ipAddress, id);
                    return RedirectToAction("Login", "Account", new { Area = "Identity" });
                }

                await SetCommonViewBagsAsync(currentUser, "Descargar Efectivo");
                var isAdmin = (bool)ViewBag.IsAdmin;
                var currentCodPerfil = ViewBag.CurrentCodPerfil as string;

                if (id != model.Id)
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/DescargarEfectivo | Respuesta: ID de ruta en URL no coincide con ID del modelo ({IdUrl} != {IdModel}) | ", currentUser.UserName, ipAddress, id, model.Id);
                    return NotFound();
                }

                var rutaExistente = await _rutaDiariaService.ObtenerRutaDiariaPorIdAsync(id);
                if (rutaExistente == null || rutaExistente.Estado != (int)EstadoRuta.SALIDA_REGISTRADA || !rutaExistente.FechaSalidaRuta.HasValue)
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/DescargarEfectivo | RutaId: {Id} | Respuesta: Ruta no encontrada o no en estado SALIDA REGISTRADA (Estado Actual: {EstadoActual}) | ", currentUser.UserName, ipAddress, id, rutaExistente?.Estado.ToString() ?? "NULL");
                    ModelState.AddModelError("", "Ruta no encontrada o no está en el estado 'SALIDA REGISTRADA' para registrar el descargue de efectivo.");
                    return View(model);
                }

                model.CodRutaSuc = rutaExistente.CodRutaSuc; model.NombreRuta = rutaExistente.NombreRuta;
                model.TipoRuta = rutaExistente.TipoRuta; model.TipoVehiculo = rutaExistente.TipoVehiculo;
                model.FechaEjecucion = rutaExistente.FechaEjecucion; model.CodSucursal = rutaExistente.CodSucursal;
                model.NombreSucursal = rutaExistente.NombreSucursal; model.FechaPlaneacion = rutaExistente.FechaPlaneacion;
                model.HoraPlaneacion = rutaExistente.HoraPlaneacion; model.UsuarioPlaneacion = rutaExistente.UsuarioPlaneacion;
                model.CodVehiculo = rutaExistente.CodVehiculo;
                model.CedulaJT = rutaExistente.CedulaJT; model.NombreJT = rutaExistente.NombreJT; model.CodCargoJT = rutaExistente.CodCargoJT;
                model.CedulaConductor = rutaExistente.CedulaConductor; model.NombreConductor = rutaExistente.NombreConductor; model.CodCargoConductor = rutaExistente.CodCargoConductor;
                model.CedulaTripulante = rutaExistente.CedulaTripulante; model.NombreTripulante = rutaExistente.NombreTripulante; model.CodCargoTripulante = rutaExistente.CodCargoTripulante;
                model.FechaIngresoJT = rutaExistente.FechaIngresoJT; model.HoraIngresoJT = rutaExistente.HoraIngresoJT;
                model.FechaSalidaJT = rutaExistente.FechaSalidaJT; model.HoraSalidaJT = rutaExistente.HoraSalidaJT;
                model.FechaCargue = rutaExistente.FechaCargue;
                model.HoraCargue = rutaExistente.HoraCargue;
                model.CantBolsaBilleteEntrega = rutaExistente.CantBolsaBilleteEntrega;
                model.CantBolsaMonedaEntrega = rutaExistente.CantBolsaMonedaEntrega;
                model.UsuarioCEFCargue = rutaExistente.UsuarioCEFCargue;
                model.KmInicial = rutaExistente.KmInicial;
                model.FechaSalidaRuta = rutaExistente.FechaSalidaRuta;
                model.HoraSalidaRuta = rutaExistente.HoraSalidaRuta;
                model.UsuarioSupervisorApertura = rutaExistente.UsuarioSupervisorApertura;
                model.KmFinal = rutaExistente.KmFinal;
                model.FechaEntradaRuta = rutaExistente.FechaEntradaRuta;
                model.HoraEntradaRuta = rutaExistente.HoraEntradaRuta;
                model.UsuarioSupervisorCierre = rutaExistente.UsuarioSupervisorCierre;
                model.UsuarioCEFDescargue = currentUser.Id;
                model.Estado = (int)EstadoRuta.DESCARGUE_REGISTRADO;


                ClearModelStateForLaterStages(ModelState);

                if (!model.FechaDescargue.HasValue)
                {
                    ModelState.AddModelError(nameof(model.FechaDescargue), "La fecha de descargue es requerida.");
                }
                if (!model.HoraDescargue.HasValue)
                {
                    ModelState.AddModelError(nameof(model.HoraDescargue), "La hora de descargue es requerida.");
                }
                if (model.FechaDescargue.HasValue && model.FechaDescargue.Value > DateOnly.FromDateTime(DateTime.Today))
                {
                    ModelState.AddModelError(nameof(model.FechaDescargue), "La fecha de descargue no puede ser una fecha futura.");
                }
                if (!model.CantBolsaBilleteRecibe.HasValue || model.CantBolsaBilleteRecibe.Value < 0)
                {
                    ModelState.AddModelError(nameof(model.CantBolsaBilleteRecibe), "La cantidad de bolsas de billete recibidas es requerida y no puede ser negativa.");
                }
                if (!model.CantBolsaMonedaRecibe.HasValue || model.CantBolsaMonedaRecibe.Value < 0)
                {
                    ModelState.AddModelError(nameof(model.CantBolsaMonedaRecibe), "La cantidad de bolsas de moneda recibidas es requerida y no puede ser negativa.");
                }


                if (!ModelState.IsValid)
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/DescargarEfectivo | RutaId: {Id} | Respuesta: Fallo de validación de modelo (controlador) | ", currentUser.UserName, ipAddress, id);
                    foreach (var entry in ModelState)
                    {
                        if (entry.Value.ValidationState == ModelValidationState.Invalid)
                        {
                            foreach (var error in entry.Value.Errors)
                            {
                                Log.Warning(" --- Error de Validación: Campo = {FieldName}, Error = {ErrorMessage}", entry.Key, error.ErrorMessage);
                            }
                        }
                    }
                    return View(model);
                }

                try
                {
                    var success = await _rutaDiariaService.RegistrarDescargueCEFAsync(
                        model.Id,
                        model.FechaDescargue.Value,
                        model.HoraDescargue.Value,
                        model.CantBolsaBilleteRecibe ?? 0,
                        model.CantBolsaMonedaRecibe ?? 0,
                        model.CantPlanillaRecibe ?? 0,
                        model.UsuarioCEFDescargue
                    );

                    if (success)
                    {
                        TempData["SuccessMessage"] = "Descargue de efectivo registrado exitosamente.";
                        Log.Information("| Usuario: {User} | Ip: {Ip} | Acción: Descargue CEF registrado | RutaId: {RutaId} | Estado: EN_PROCESO | ", currentUser.UserName, ipAddress, model.Id);
                        return RedirectToAction(nameof(CefDashboard));
                    }
                    else
                    {
                        ModelState.AddModelError("", "No se pudo registrar el descargue de efectivo. Verifique los datos o el estado de la ruta.");
                        Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/DescargarEfectivo | RutaId: {Id} | Respuesta: Fallo en el servicio de registro de descargue CEF | ", currentUser.UserName, ipAddress, id);
                    }
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/DescargarEfectivo | RutaId: {Id} | Respuesta: Error de operación inválida: {ErrorMessage} | ", currentUser.UserName, ipAddress, id, ex.Message);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error inesperado al registrar el descargue de efectivo para la ruta {Id} y usuario {User}.", id, currentUser.UserName);
                    ModelState.AddModelError("", $"Error inesperado al registrar el descargue de efectivo: {ex.Message}");
                }
                return View(model);
            }
        }

        /// <summary>
        /// Muestra el formulario para registrar la entrada del vehiculo a la empresa.
        /// </summary>
        /// <remarks>
        /// Requiere el permiso de 'Edit' para la vista 'RUDOP'.
        /// La ruta debe estar en estado 'DESCARGUE_REGISTRADO' para edición por el SupervisorRuta.
        /// </remarks>
        /// <param name="id">El ID del registro de log a editar.</param>
        /// <returns>La vista del formulario de edición con los datos de la ruta.</returns>
        [HttpGet("RegistrarEntrada/{id}")]
        [RequiredPermission(PermissionType.Edit, "RUDOP")]
        public async Task<IActionResult> RegistrarEntrada(string id)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null)
                {
                    Log.Warning("| Usuario: No autenticado | Ip: {Ip} | Acción: Acceso a /RutasDiarias/RegistrarEntrada/{Id} | Respuesta: Redirigiendo a Login/Home |", ipAddress, id);
                    return RedirectToAction("Login", "Account", new { Area = "Identity" });
                }

                await SetCommonViewBagsAsync(currentUser, "Unloading");
                var isAdmin = (bool)ViewBag.IsAdmin;
                var currentCodPerfil = ViewBag.CurrentCodPerfil as string;

                if (string.IsNullOrEmpty(id))
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a Registrar Entrada | Respuesta: ID de ruta nulo o vacío | ", currentUser.UserName, ipAddress);
                    return NotFound();
                }

                var rutaDiaria = await _context.TdvRutasDiarias
                                               .Include(r => r.Sucursal)
                                               .Include(r => r.UsuarioPlaneacionObj)
                                               .Include(r => r.JT)
                                               .Include(r => r.Conductor)
                                               .Include(r => r.Tripulante)
                                               .Include(r => r.Vehiculo)
                                               .Include(r => r.CargoJTObj)
                                               .Include(r => r.CargoConductorObj)
                                               .Include(r => r.CargoTripulanteObj)
                                               .Include(r => r.UsuarioCEFCargueObj)
                                               .Include(r => r.UsuarioSupervisorAperturaObj)
                                               .FirstOrDefaultAsync(r => r.Id == id);

                if (rutaDiaria == null || rutaDiaria.Estado != (int)EstadoRuta.DESCARGUE_REGISTRADO || !rutaDiaria.FechaSalidaRuta.HasValue || !rutaDiaria.HoraSalidaRuta.HasValue)
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a Registrar Entrada ({Id}) | Respuesta: Ruta no encontrada o no en estado DESCARGUE REGISTRADO (Estado Actual: {EstadoActual}) | ", currentUser.UserName, ipAddress, id, rutaDiaria?.Estado.ToString() ?? "NULL");
                    TempData["ErrorMessage"] = "Ruta no encontrada o no está en el estado 'DESCARGUE REGISTRADO' para registrar la entrada del vehículo.";
                    return RedirectToAction(nameof(OperationsDashboard));
                }

                rutaDiaria.FechaEntradaRuta = DateOnly.FromDateTime(DateTime.Now);
                rutaDiaria.HoraEntradaRuta = TimeOnly.FromDateTime(DateTime.Now);

                Log.Information("| Usuario: {User} | Ip: {Ip} | Acción: Acceso a /RutasDiarias/RegistrarEntrada/{Id} | Respuesta: Acceso permitido | ", currentUser.UserName, ipAddress, id);
                return View(rutaDiaria);
            }
        }

        /// <summary>
        /// Actualiza un registro de la ruta existente, utilizado para insertar KM final del vehiculo y cerrar la ruta.
        /// </summary>
        /// <remarks>
        /// Requiere el permiso de 'Edit' para la vista 'RUDOP' (para completar el registro).
        /// </remarks>
        /// <param name="id">El ID del registro de la ruta a actualizar.</param>
        [HttpPost("RegistrarEntrada/{id}")]
        [ValidateAntiForgeryToken]
        [RequiredPermission(PermissionType.Edit, "RUDOP")]
        public async Task<IActionResult> RegistrarEntrada(string id, [FromForm] TdvRutaDiaria model)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null) return Unauthorized();

                await SetCommonViewBagsAsync(currentUser, "Registrar Entrada");
                var isAdmin = (bool)ViewBag.IsAdmin;
                var currentCodPerfil = ViewBag.CurrentCodPerfil as string;

                if (id != model.Id)
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/RegistrarEntrada | Respuesta: ID de ruta en URL no coincide con ID del modelo ({IdUrl} != {IdModel}) | ", currentUser.UserName, ipAddress, id, model.Id);
                    return NotFound();
                }

                var rutaExistente = await _rutaDiariaService.ObtenerRutaDiariaPorIdAsync(id);
                if (rutaExistente == null || rutaExistente.Estado != (int)EstadoRuta.DESCARGUE_REGISTRADO || !rutaExistente.FechaSalidaRuta.HasValue || !rutaExistente.HoraSalidaRuta.HasValue)
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/RegistrarEntrada | RutaId: {Id} | Respuesta: Ruta no encontrada o no en estado DESCARGUE REGISTRADO (Estado Actual: {EstadoActual}) | ", currentUser.UserName, ipAddress, id, rutaExistente?.Estado.ToString() ?? "NULL");
                    ModelState.AddModelError("", "Ruta no encontrada o no está en el estado 'DESCARGUE REGISTRADO' para registrar la entrada del vehículo.");
                    return View(model);
                }

                model.CodRutaSuc = rutaExistente.CodRutaSuc; model.NombreRuta = rutaExistente.NombreRuta;
                model.TipoRuta = rutaExistente.TipoRuta; model.TipoVehiculo = rutaExistente.TipoVehiculo;
                model.FechaEjecucion = rutaExistente.FechaEjecucion; model.CodSucursal = rutaExistente.CodSucursal;
                model.NombreSucursal = rutaExistente.NombreSucursal; model.FechaPlaneacion = rutaExistente.FechaPlaneacion;
                model.HoraPlaneacion = rutaExistente.HoraPlaneacion; model.UsuarioPlaneacion = rutaExistente.UsuarioPlaneacion;
                model.CodVehiculo = rutaExistente.CodVehiculo;
                model.CedulaJT = rutaExistente.CedulaJT; model.NombreJT = rutaExistente.NombreJT; model.CodCargoJT = rutaExistente.CodCargoJT;
                model.CedulaConductor = rutaExistente.CedulaConductor; model.NombreConductor = rutaExistente.NombreConductor; model.CodCargoConductor = rutaExistente.CodCargoConductor;
                model.CedulaTripulante = rutaExistente.CedulaTripulante; model.NombreTripulante = rutaExistente.NombreTripulante; model.CodCargoTripulante = rutaExistente.CodCargoTripulante;
                model.FechaIngresoJT = rutaExistente.FechaIngresoJT; model.HoraIngresoJT = rutaExistente.HoraIngresoJT;
                model.FechaSalidaJT = rutaExistente.FechaSalidaJT; model.HoraSalidaJT = rutaExistente.HoraSalidaRuta;
                model.FechaCargue = rutaExistente.FechaCargue; model.HoraCargue = rutaExistente.HoraCargue;
                model.CantBolsaBilleteEntrega = rutaExistente.CantBolsaBilleteEntrega; model.CantBolsaMonedaEntrega = rutaExistente.CantBolsaMonedaEntrega;
                model.UsuarioCEFCargue = rutaExistente.UsuarioCEFCargue;
                model.FechaDescargue = rutaExistente.FechaDescargue; model.HoraDescargue = rutaExistente.HoraDescargue;
                model.CantBolsaBilleteRecibe = rutaExistente.CantBolsaBilleteRecibe; model.CantBolsaMonedaRecibe = rutaExistente.CantBolsaMonedaRecibe;
                model.UsuarioCEFDescargue = rutaExistente.UsuarioCEFDescargue;
                model.KmInicial = rutaExistente.KmInicial; model.FechaSalidaRuta = rutaExistente.FechaSalidaRuta;
                model.HoraSalidaRuta = rutaExistente.HoraSalidaRuta; model.UsuarioSupervisorApertura = rutaExistente.UsuarioSupervisorApertura;
                model.UsuarioSupervisorCierre = currentUser.Id;
                model.Estado = (int)EstadoRuta.CERRADO;

                ClearModelStateForLaterStages(ModelState);
                if (!model.KmFinal.HasValue || model.KmFinal.Value <= 0)
                {
                    ModelState.AddModelError(nameof(model.KmFinal), "El Kilometraje Final es requerido y debe ser un valor positivo.");
                }
                else if (rutaExistente.KmInicial.HasValue && model.KmFinal.Value < rutaExistente.KmInicial.Value)
                {
                    ModelState.AddModelError(nameof(model.KmFinal), "El Kilometraje Final no puede ser menor que el Kilometraje Inicial.");
                }

                if (!model.FechaEntradaRuta.HasValue)
                {
                    ModelState.AddModelError(nameof(model.FechaEntradaRuta), "La fecha de entrada es requerida.");
                }
                if (!model.HoraEntradaRuta.HasValue)
                {
                    ModelState.AddModelError(nameof(model.HoraEntradaRuta), "La hora de entrada es requerida.");
                }
                if (model.FechaEntradaRuta.HasValue && model.FechaEntradaRuta.Value > DateOnly.FromDateTime(DateTime.Today))
                {
                    ModelState.AddModelError(nameof(model.FechaEntradaRuta), "La fecha de entrada de la ruta no puede ser una fecha futura.");
                }

                if (model.FechaSalidaRuta.HasValue && model.HoraSalidaRuta.HasValue && model.FechaEntradaRuta.HasValue && model.HoraEntradaRuta.HasValue)
                {
                    var salidaDateTime = new DateTime(model.FechaSalidaRuta.Value.Year, model.FechaSalidaRuta.Value.Month, model.FechaSalidaRuta.Value.Day,
                                                      model.HoraSalidaRuta.Value.Hour, model.HoraSalidaRuta.Value.Minute, 0); // Segundos a 0
                    var entradaDateTime = new DateTime(model.FechaEntradaRuta.Value.Year, model.FechaEntradaRuta.Value.Month, model.FechaEntradaRuta.Value.Day,
                                                       model.HoraEntradaRuta.Value.Hour, model.HoraEntradaRuta.Value.Minute, 0); // Segundos a 0

                    if (entradaDateTime < salidaDateTime)
                    {
                        ModelState.AddModelError(nameof(model.FechaEntradaRuta), "La fecha y hora de entrada no pueden ser anteriores a la fecha y hora de salida.");
                        ModelState.AddModelError(nameof(model.HoraEntradaRuta), "La fecha y hora de entrada no pueden ser anteriores a la fecha y hora de salida.");
                    }
                }

                if (!ModelState.IsValid)
                {
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/RegistrarEntrada | RutaId: {Id} | Respuesta: Fallo de validación de modelo | ", currentUser.UserName, ipAddress, id);
                    foreach (var entry in ModelState)
                    {
                        if (entry.Value.ValidationState == ModelValidationState.Invalid)
                        {
                            foreach (var error in entry.Value.Errors)
                            {
                                Log.Warning(" --- Error de Validación: Campo = {FieldName}, Error = {ErrorMessage}", entry.Key, error.ErrorMessage);
                            }
                        }
                    }
                    return View(model);
                }

                try
                {
                    var success = await _rutaDiariaService.RegistrarEntradaVehiculoAsync(
                        model.Id,
                        model.KmFinal.Value,
                        model.FechaEntradaRuta.Value,
                        model.HoraEntradaRuta.Value,
                        model.UsuarioSupervisorCierre
                    );

                    if (success)
                    {
                        TempData["SuccessMessage"] = "Entrada del vehículo registrada y ruta cerrada exitosamente.";
                        Log.Information("| Usuario: {User} | Ip: {Ip} | Acción: Entrada de vehículo registrada y ruta cerrada | RutaId: {RutaId} | Estado: CERRADO | ", currentUser.UserName, ipAddress, model.Id);
                        return RedirectToAction(nameof(OperationsDashboard));
                    }
                    else
                    {
                        ModelState.AddModelError("", "No se pudo registrar la entrada del vehículo y cerrar la ruta. Verifique los datos o el estado de la ruta.");
                        Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/RegistrarEntrada | RutaId: {Id} | Respuesta: Fallo en el servicio de registro de entrada | ", currentUser.UserName, ipAddress, id);
                    }
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                    Log.Warning("| Usuario: {User} | Ip: {Ip} | Acción: POST /RutasDiarias/RegistrarEntrada | RutaId: {Id} | Respuesta: Error de operación inválida: {ErrorMessage} | ", currentUser.UserName, ipAddress, id, ex.Message);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error inesperado al registrar la entrada del vehículo para la ruta {Id} y usuario {User}.", id, currentUser.UserName);
                    ModelState.AddModelError("", $"Error inesperado al registrar la entrada del vehículo: {ex.Message}");
                }
                return View(model);
            }
        }

        /// <summary>
        /// Metodo para exportar las rutas diarias (Excel, CSV, PDF y JSON).
        /// </summary>
        /// <param name="exportFormat">El tipo de formato de exportacion.</param>
        /// <param name="search">Valor buscado en la tabla de rutas.</param>
        /// <param name="fechaEjecucion">Fecha de ejecucion de la ruta (opcional).</param>
        /// /// <param name="codSuc">El código de la sucursal para filtrar las rutas (opcional).</param>
        /// <param name="estado">Estado de la ruta (opcional).</param>
        [HttpGet("ExportRutasDiarias")]
        public async Task<IActionResult> ExportRutasDiarias(
           string exportFormat,
           string search = "",
           DateOnly? fechaEjecucion = null,
           int? codSuc = null,
           int? estado = null)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                var currentUser = await GetCurrentApplicationUserAsync();
                if (currentUser == null) return Unauthorized();

                var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

                var rawData = await _rutaDiariaService.GetFilteredRutasForExportAsync(
                    currentUser.Id,
                    codSuc,
                    fechaEjecucion,
                    estado,
                    search,
                    isAdmin
                );
                // --- FIN DE LA MODIFICACIÓN ---

                var dataToExport = rawData.Select(r => new TdvRutaDiariaExportViewModel
                {
                    IdRuta = r.Id,
                    NombreRuta = r.NombreRuta,
                    NombreSucursal = r.Sucursal != null ? r.Sucursal.NombreSucursal : "N/A",
                    FechaEjecucion = r.FechaEjecucion,
                    UsuarioPlaneacion = r.UsuarioPlaneacionObj != null ? r.UsuarioPlaneacionObj.NombreUsuario : "N/A",
                    TipoRuta = r.TipoRuta switch
                    {
                        "T" => "TRADICIONAL",
                        "A" => "ATM",
                        "M" => "MIXTA",
                        "L" => "LIBERACIÓN DE EFECTIVO",
                        _ => "Otro"
                    },
                    TipoVehiculo = r.TipoVehiculo switch
                    {
                        "C" => "CAMIONETA",
                        "B" => "BLINDADO",
                        "M" => "MOTO",
                        "T" => "CAMION",
                        _ => "Otro"
                    },
                    EstadoRuta = ((EstadoRuta)r.Estado).ToString().Replace("_", " "),
                    NombreJT = r.JT != null ? r.JT.NombreCompleto : "N/A",
                    NombreConductor = r.Conductor != null ? r.Conductor.NombreCompleto : "N/A",
                    NombreTripulante = r.Tripulante != null ? r.Tripulante.NombreCompleto : "N/A",
                    NombreVehiculo = r.CodVehiculo ?? "N/A",
                    NombreCargoJT = r.CargoJTObj != null ? r.CargoJTObj.NombreCargo : "N/A",
                    NombreCargoConductor = r.CargoConductorObj != null ? r.CargoConductorObj.NombreCargo : "N/A",
                    NombreCargoTripulante = r.CargoTripulanteObj != null ? r.CargoTripulanteObj.NombreCargo : "N/A",
                    KmInicial = r.KmInicial,
                    KmFinal = r.KmFinal,
                    FechaSalidaRuta = r.FechaSalidaRuta,
                    HoraSalidaRuta = r.HoraSalidaRuta,
                    FechaEntradaRuta = r.FechaEntradaRuta,
                    HoraEntradaRuta = r.HoraEntradaRuta,
                    FechaCargue = r.FechaCargue,
                    HoraCargue = r.HoraCargue,
                    CantBolsaBilleteEntrega = r.CantBolsaBilleteEntrega,
                    CantBolsaMonedaEntrega = r.CantBolsaMonedaEntrega,
                    FechaDescargue = r.FechaDescargue,
                    HoraDescargue = r.HoraDescargue,
                    CantBolsaBilleteRecibe = r.CantBolsaBilleteRecibe,
                    CantBolsaMonedaRecibe = r.CantBolsaMonedaRecibe
                }).ToList();

                var columnDisplayNames = new Dictionary<string, string>
        {
            { "IdRuta", "ID de Ruta" }, { "NombreRuta", "Ruta" }, { "NombreSucursal", "Sucursal" },
            { "FechaEjecucion", "Fecha de Programación" }, { "UsuarioPlaneacion", "Planeador" },
            { "TipoRuta", "Tipo de Ruta" }, { "TipoVehiculo", "Tipo de Vehículo" }, { "EstadoRuta", "Estado" },
            { "NombreJT", "Jefe de Tripulación" }, { "NombreCargoJT", "Cargo JT" },
            { "NombreConductor", "Conductor" }, { "NombreCargoConductor", "Cargo Conductor" },
            { "NombreTripulante", "Tripulante" }, { "NombreCargoTripulante", "Cargo Tripulante" },
            { "NombreVehiculo", "Vehículo" }, { "KmInicial", "KM Inicial" }, { "KmFinal", "KM Final" },
            { "FechaSalidaRuta", "Fecha Salida" }, { "HoraSalidaRuta", "Hora Salida" },
            { "FechaEntradaRuta", "Fecha Entrada" }, { "HoraEntradaRuta", "Hora Entrada" },
            { "FechaCargue", "Fecha Cargue CEF" }, { "HoraCargue", "Hora Cargue CEF" },
            { "CantBolsaBilleteEntrega", "Bolsas Billetes Entrega" }, { "CantBolsaMonedaEntrega", "Bolsas Monedas Entrega" },
            { "FechaDescargue", "Fecha Descargue CEF" }, { "HoraDescargue", "Hora Descargue CEF" },
            { "CantBolsaBilleteRecibe", "Bolsas Billetes Recibe" }, { "CantBolsaMonedaRecibe", "Bolsas Monedas Recibe" }
        };

                Log.Information("| Usuario: {User} | Ip: {Ip} | Acción: Exportar Rutas Diarias | Formato: {Format} | Cantidad: {Count} |",
                                currentUser.UserName, ipAddress, exportFormat, dataToExport.Count);

                try
                {
                    var fileResult = await _exportService.ExportDataAsync(dataToExport, exportFormat, "RutasDiarias", columnDisplayNames);
                    return fileResult;
                }
                catch (NotImplementedException ex)
                {
                    Log.Warning(ex, "| Usuario: {User} | Ip: {Ip} | Acción: Error al exportar Rutas Diarias | Formato no implementado: {Format} |", currentUser.UserName, ipAddress, exportFormat);
                    TempData["ErrorMessage"] = $"Formato de exportación '{exportFormat}' no implementado aún.";
                    return RedirectToAction(nameof(PlannerDashboard), new { page = 1, pageSize = 15, search, fechaEjecucion, codSuc, estado });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "| Usuario: {User} | Ip: {Ip} | Acción: Error al exportar Rutas Diarias | Mensaje: {ErrorMessage} |", currentUser.UserName, ipAddress, ex.Message);
                    TempData["ErrorMessage"] = $"Error al exportar rutas: {ex.Message}";
                    return RedirectToAction(nameof(PlannerDashboard), new { page = 1, pageSize = 15, search, fechaEjecucion, codSuc, estado });
                }
            }
        }

        /// <summary>
        /// Obtener el jefe de tripulacion por medio de la sucursal de la ruta.
        /// </summary>
        /// <param name="codSuc">Valor de la sucursal.</param>
        [HttpGet("GetJefesTurnoBySucursal")]
        public async Task<JsonResult> GetJefesTurnoBySucursal(int? codSuc)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            var isAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin");

            if (!codSuc.HasValue || codSuc.Value <= 0)
            {
                return Json(new List<SelectListItem> { new SelectListItem { Value = "", Text = "-- Seleccione un Jefe de Tripulación --" } });
            }

            var jtCargoId = 64;

            var jefesTurnoQuery = _context.AdmEmpleados
                                         .Where(e => e.EmpleadoEstado == EstadoEmpleado.Activo && e.CodCargo == jtCargoId);

            if (!isAdmin)
            {
                jefesTurnoQuery = jefesTurnoQuery.Where(e => e.CodSucursal.HasValue && e.CodSucursal.Value == codSuc.Value);
            }

            var jefesTurno = await jefesTurnoQuery
                                         .OrderBy(e => e.NombreCompleto)
                                         .Select(e => new { value = e.CodCedula, text = e.NombreCompleto ?? string.Empty })
                                         .ToListAsync();

            return Json(jefesTurno);
        }

        /// <summary>
        /// Obtener el conductor por medio de la sucursal de la ruta.
        /// </summary>
        /// <param name="codSuc">Valor de la sucursal.</param>
        [HttpGet("GetConductoresBySucursal")]
        public async Task<JsonResult> GetConductoresBySucursal(int? codSuc)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            var isAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin");

            if (!codSuc.HasValue || codSuc.Value <= 0)
            {
                return Json(new List<SelectListItem> { new SelectListItem { Value = "", Text = "-- Seleccione un Conductor --" } });
            }

            var conductorCargoId = 45; 

            var conductoresQuery = _context.AdmEmpleados
                                          .Where(e => e.EmpleadoEstado == EstadoEmpleado.Activo && e.CodCargo == conductorCargoId);
            if (!isAdmin)
            {
                conductoresQuery = conductoresQuery.Where(e => e.CodSucursal.HasValue && e.CodSucursal.Value == codSuc.Value);
            }

            var conductores = await conductoresQuery
                                                  .OrderBy(e => e.NombreCompleto)
                                                  .Select(e => new { value = e.CodCedula, text = e.NombreCompleto ?? string.Empty })
                                                  .ToListAsync();

            return Json(conductores);
        }

        /// <summary>
        /// Obtener el conductor por medio de la sucursal de la ruta.
        /// </summary>
        /// <param name="codSuc">Valor de la sucursal.</param>
        [HttpGet("GetTripulantesBySucursal")]
        public async Task<JsonResult> GetTripulantesBySucursal(int? codSuc)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            var isAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin");

            if (!codSuc.HasValue || codSuc.Value <= 0)
            {
                return Json(new List<SelectListItem> { new SelectListItem { Value = "", Text = "-- Seleccione un Tripulante --" } });
            }

            var tripulanteCargoId = 51;

            var tripulantesQuery = _context.AdmEmpleados
                                          .Where(e => e.EmpleadoEstado == EstadoEmpleado.Activo && e.CodCargo == tripulanteCargoId);
            if (!isAdmin)
            {
                tripulantesQuery = tripulantesQuery.Where(e => e.CodSucursal.HasValue && e.CodSucursal.Value == codSuc.Value);
            }

            var tripulantes = await tripulantesQuery
                                                  .OrderBy(e => e.NombreCompleto)
                                                  .Select(e => new { value = e.CodCedula, text = e.NombreCompleto ?? string.Empty })
                                                  .ToListAsync();

            return Json(tripulantes);
        }


        /// <summary>
        /// Obtener los datos de los tripulanes selecciondos (Nombre, Cargo, Sucursal).
        /// </summary>
        /// <param name="cedula">Cedula del empleado.</param>
        [HttpGet("GetEmployeeDetails")]
        public async Task<JsonResult> GetEmployeeDetails(int cedula)
        {
            if (cedula <= 0)
            {
                return Json(new { nombreCompleto = string.Empty, codCargo = (int?)null, codSucursal = (int?)null });
            }

            var employee = await _context.AdmEmpleados
                                         .Include(e => e.Cargo)
                                         .Where(e => e.CodCedula == cedula && e.EmpleadoEstado == EstadoEmpleado.Activo)
                                         .Select(e => new
                                         {
                                             nombreCompleto = e.NombreCompleto ?? string.Empty,
                                             codCargo = e.Cargo != null ? e.Cargo.CodCargo : (int?)null,
                                             codSucursal = e.CodSucursal
                                         })
                                         .FirstOrDefaultAsync();

            if (employee == null)
            {
                return Json(new { nombreCompleto = string.Empty, codCargo = (int?)null, codSucursal = (int?)null });
            }
            return Json(employee);
        }

        /// <summary>
        /// Obtener los vehiculos por la sucursal de la ruta.
        /// </summary>
        /// <param name="codSuc">Valor de la sucursal.</param>
        [HttpGet("GetVehiclesBySucursal")]
        public async Task<JsonResult> GetVehiclesBySucursal(int? codSuc)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            var isAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin");

            if (!codSuc.HasValue || codSuc.Value <= 0)
            {
                return Json(new List<SelectListItem> { new SelectListItem { Value = "", Text = "-- Seleccione un Vehículo --" } });
            }

            var vehiculosQuery = _context.AdmVehiculos
                                         .Where(v => v.Estado == true && v.CodSucursal.HasValue && v.CodSucursal.Value == codSuc.Value);

            if (isAdmin)
            {
                vehiculosQuery = _context.AdmVehiculos.Where(v => v.Estado == true);
            }

            var vehiculos = await vehiculosQuery
                                    .OrderBy(v => v.CodVehiculo)
                                    .Select(v => new { 
                                        value = v.CodVehiculo ?? string.Empty, 
                                        text = $"{v.CodVehiculo} - {v.Marca} {v.Linea}" ?? string.Empty })
                                    .ToListAsync();

            return Json(vehiculos);
        }

        /// <summary>
        /// Obtener las rutas maestras por la sucursal.
        /// </summary>
        /// <param name="codSuc">Código de la sucursal.</param>
        [HttpGet("GetRutasMaestrasBySucursal")]
        public async Task<JsonResult> GetRutasMaestrasBySucursal(int? codSuc)
        {
            var currentUser = await GetCurrentApplicationUserAsync();
            var isAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin");

            if (!codSuc.HasValue || codSuc.Value <= 0)
            {
                return Json(new List<SelectListItem> { new SelectListItem { Value = "", Text = "-- Seleccione una Ruta Maestra --" } });
            }

            var rutasMaestrasQuery = _context.AdmRutas
                                              .Where(rm => rm.EstadoRuta == true && rm.CodSucursal == codSuc.Value);

            var rutasMaestras = await rutasMaestrasQuery
                                                .OrderBy(rm => rm.NombreRuta)
                                                .Select(rm => new { value = rm.CodRutaSuc, text = $"{rm.CodRuta} - {rm.NombreRuta}" })
                                                .ToListAsync();

            return Json(rutasMaestras);
        }

        /// <summary>
        /// Obtener los detalles de una ruta maestra.
        /// </summary>
        /// <param name="codRutaSuc">Código de la ruta maestra de sucursal.</param>
        [HttpGet("GetRutaDetails")]
        public async Task<JsonResult> GetRutaDetails(string codRutaSuc)
        {
            if (string.IsNullOrEmpty(codRutaSuc))
            {
                return Json(null);
            }

            var rutaMaster = await _context.AdmRutas
                                           .Where(rm => rm.CodRutaSuc == codRutaSuc && rm.EstadoRuta == true)
                                           .Select(rm => new
                                           {
                                               nombreRuta = rm.NombreRuta,
                                               rutaTipo = rm.TipoRuta,
                                               rutaTipoVeh = rm.TipoVehiculo
                                           })
                                           .FirstOrDefaultAsync();

            if (rutaMaster == null)
            {
                return Json(new { nombreRuta = string.Empty, rutaTipo = string.Empty, rutaTipoVeh = string.Empty });
            }
            return Json(rutaMaster);
        }

        /// <summary>
        /// Obtener los detalles de una sucursal.
        /// </summary>
        /// <param name="codSuc">Código de la sucursal.</param>
        [HttpGet("GetSucursalDetails")]
        public async Task<JsonResult> GetSucursalDetails(int? codSuc)
        {
            if (!codSuc.HasValue || codSuc.Value <= 0)
            {
                return Json(new { nombreSucursal = string.Empty });
            }

            var sucursal = await _context.AdmSucursales
                                         .Where(s => s.CodSucursal == codSuc.Value && s.Estado == true)
                                         .Select(s => new { nombreSucursal = s.NombreSucursal })
                                         .FirstOrDefaultAsync();

            if (sucursal == null)
            {
                return Json(new { nombreSucursal = string.Empty });
            }
            return Json(sucursal);
        }
    }
}