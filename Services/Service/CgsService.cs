using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Models.ViewModels.Servicio;
using VCashApp.Services.DTOs;
using VCashApp.Utils;
using VCashApp.Infrastructure.Branches;

namespace VCashApp.Services.Service
{
    public class CgsService : ICgsServiceService
    {
        private readonly AppDbContext _context;
        private readonly IBranchContext _branchContext;

        public CgsService(
            AppDbContext context,
            IBranchContext branchContext
            )
        {
            _context = context;
            _branchContext = branchContext;
        }

        public async Task<CgsServiceRequestViewModel> PrepareServiceRequestViewModelAsync(string currentUserId, string currentIP)
        {
            var viewModel = new CgsServiceRequestViewModel();

            viewModel.AvailableClients = await GetClientsForDropdownAsync();
            viewModel.AvailableBranches = await GetBranchesForDropdownAsync();
            viewModel.AvailableConcepts = await GetServiceConceptsForDropdownAsync();
            viewModel.AvailableStatuses = await GetServiceStatusesForDropdownAsync();
            viewModel.AvailableOriginTypes = GetLocationTypeSelectList();
            viewModel.AvailableDestinationTypes = GetLocationTypeSelectList();
            viewModel.AvailableTransferTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "N", Text = "Normal (Predeterminado)" },
                new SelectListItem { Value = "I", Text = "Interno" },
                new SelectListItem { Value = "T", Text = "Transportadora" }
            };
            viewModel.AvailableFailedResponsibles = await GetFailedResponsiblesForDropdown();
            viewModel.AvailableServiceModalities = await GetServiceModalitiesForDropdownAsync();
            viewModel.AvailableCurrencies = GetCurrenciesForDropdown();
            viewModel.Currency = nameof(CurrencyEnum.COP);

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
            viewModel.CgsOperatorUserName = currentUser?.NombreUsuario ?? "Desconocido";
            viewModel.OperatorIpAddress = currentIP;

            var activeBranch = _branchContext.CurrentBranchId;
            if (activeBranch.HasValue)
            {
                var operatorBranch = await _context.AdmSucursales
                    .FirstOrDefaultAsync(s => s.CodSucursal == activeBranch.Value);
                viewModel.OperatorBranchName = operatorBranch?.NombreSucursal ?? "N/A";
                viewModel.BranchCode = activeBranch.Value; // si tu VM tiene esa propiedad
            }
            else
            {
                viewModel.OperatorBranchName = "N/A";
            }

            viewModel.RequestDate = DateOnly.FromDateTime(DateTime.Now);
            viewModel.RequestTime = TimeOnly.FromDateTime(DateTime.Now);
            viewModel.ProgrammingDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
            viewModel.ProgrammingTime = TimeOnly.FromDateTime(DateTime.Now);
            viewModel.IsFailed = false;

            return viewModel;
        }

        public async Task<ServiceResult> CreateServiceRequestAsync(CgsServiceRequestViewModel viewModel, string currentUserId, string currentIP)
        {
            var serviceConcept = await _context.AdmConceptos.AsNoTracking().FirstOrDefaultAsync(c => c.CodConcepto == viewModel.ConceptCode);
            if (serviceConcept == null)
            {
                throw new InvalidOperationException("Código de concepto de servicio no válido.");
            }

            var branchName = await _context.AdmSucursales
                .Where(s => s.CodSucursal == viewModel.BranchCode)
                .Select(s => s.NombreSucursal)
                .FirstOrDefaultAsync();

            string cefEstado =
                (viewModel.ConceptCode == 1 || viewModel.ConceptCode == 4)
                    ? nameof(CefTransactionStatusEnum.RegistroTesoreria)
                    : (viewModel.ConceptCode == 2 || viewModel.ConceptCode == 3)
                        ? nameof(CefTransactionStatusEnum.ProvisionEnProceso)
                        : nameof(CefTransactionStatusEnum.RegistroTesoreria);

            // === Validaciones ===
            if (viewModel.ConceptCode == 5 && (viewModel.TransferType != "I" && viewModel.TransferType != "T"))
                return ServiceResult.FailureResult("Para TRASLADO, Tipo de Traslado debe ser 'Interno' o 'Transportadora'.");
            else if (viewModel.ConceptCode != 5)
                viewModel.TransferType = "N";

            if (viewModel.OriginIndicatorType == "P" && !await _context.AdmPuntos.AnyAsync(p => p.PointCode == viewModel.OriginPointCode))
                return ServiceResult.FailureResult($"Código de Punto de Origen '{viewModel.OriginPointCode}' inválido.");
            if (viewModel.OriginIndicatorType == "F" && !await _context.AdmFondos.AnyAsync(f => f.FundCode == viewModel.OriginPointCode))
                return ServiceResult.FailureResult($"Código de Fondo de Origen '{viewModel.OriginPointCode}' inválido.");
            if (viewModel.DestinationIndicatorType == "P" && !await _context.AdmPuntos.AnyAsync(p => p.PointCode == viewModel.DestinationPointCode))
                return ServiceResult.FailureResult($"Código de Punto de Destino '{viewModel.DestinationPointCode}' inválido.");
            if (viewModel.DestinationIndicatorType == "F" && !await _context.AdmFondos.AnyAsync(f => f.FundCode == viewModel.DestinationPointCode))
                return ServiceResult.FailureResult($"Código de Fondo de Destino '{viewModel.DestinationPointCode}' inválido.");

            // === Valores mínimos para crear CEF desde Servicios ===
            var declaredBill = viewModel.BillValue ?? 0m;
            var declaredCoin = viewModel.CoinValue ?? 0m;
            var declaredDocs = 0m;
            var totalDeclared = viewModel.ServiceValue ?? 0m;
            var totalDeclaredLetters = AmountInWordsHelper.ToSpanishCurrency(totalDeclared, "COP");
            var totalCounted = 0;
            var totalCountedLetters = AmountInWordsHelper.ToSpanishCurrency(totalCounted, "COP");

            var declaredBags = viewModel.NumberOfCoinBags ?? 0;
            var declaredEnv = 0;
            var declaredChecks = 0;
            var declaredDocsCt = 0;
            int cefPlanilla = 0;
            var acceptanceDate = DateOnly.FromDateTime(DateTime.Now);
            var acceptanceTime = TimeOnly.FromDateTime(DateTime.Now);

            var p = new[]
            {
                // ===== CgsServicios =====
                new SqlParameter("@NumeroPedido",           (object?)viewModel.RequestNumber ?? DBNull.Value),
                new SqlParameter("@CodCliente",             viewModel.OriginClientCode),
                new SqlParameter("@CodOsCliente",           (object?)viewModel.ClientServiceOrderCode ?? DBNull.Value),
                new SqlParameter("@CodSucursal",            viewModel.BranchCode),
                new SqlParameter("@FechaSolicitud",         viewModel.RequestDate.ToDateTime(TimeOnly.MinValue)),
                new SqlParameter("@HoraSolicitud",          viewModel.RequestTime.ToTimeSpan()),
                new SqlParameter("@CodConcepto",            viewModel.ConceptCode),
                new SqlParameter("@TipoTraslado",           (object?)viewModel.TransferType ?? DBNull.Value),
                new SqlParameter("@CodEstado",              viewModel.StatusCode),
                new SqlParameter("@CodFlujo",               (object?)viewModel.ConceptCode ?? DBNull.Value),

                new SqlParameter("@CodClienteOrigen",       (object?)viewModel.OriginClientCode ?? DBNull.Value),
                new SqlParameter("@CodPuntoOrigen",         viewModel.OriginPointCode),
                new SqlParameter("@IndicadorTipoOrigen",    viewModel.OriginIndicatorType),

                new SqlParameter("@CodClienteDestino",      (object?)viewModel.DestinationClientCode ?? DBNull.Value),
                new SqlParameter("@CodPuntoDestino",        viewModel.DestinationPointCode),
                new SqlParameter("@IndicadorTipoDestino",   viewModel.DestinationIndicatorType),

                new SqlParameter("@FechaAceptacion",        acceptanceDate),
                new SqlParameter("@HoraAceptacion",         acceptanceTime),
                new SqlParameter("@FechaProgramacion",      (object?)viewModel.ProgrammingDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value),
                new SqlParameter("@HoraProgramacion",       (object?)viewModel.ProgrammingTime?.ToTimeSpan() ?? DBNull.Value),

                new SqlParameter("@FechaAtencionInicial",   (object?)viewModel.InitialAttentionDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value),
                new SqlParameter("@HoraAtencionInicial",    (object?)viewModel.InitialAttentionTime?.ToTimeSpan() ?? DBNull.Value),
                new SqlParameter("@FechaAtencionFinal",     (object?)viewModel.FinalAttentionDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value),
                new SqlParameter("@HoraAtencionFinal",      (object?)viewModel.FinalAttentionTime?.ToTimeSpan() ?? DBNull.Value),

                new SqlParameter("@FechaCancelacion",       (object?)viewModel.CancellationDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value),
                new SqlParameter("@HoraCancelacion",        (object?)viewModel.CancellationTime?.ToTimeSpan() ?? DBNull.Value),
                new SqlParameter("@FechaRechazo",           (object?)viewModel.RejectionDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value),
                new SqlParameter("@HoraRechazo",            (object?)viewModel.RejectionTime?.ToTimeSpan() ?? DBNull.Value),

                new SqlParameter("@Fallido",                viewModel.IsFailed),
                new SqlParameter("@ResponsableFallido",     (object?)viewModel.FailedResponsible ?? DBNull.Value),
                new SqlParameter("@RazonFallido",           (object?)viewModel.FailedReason ?? DBNull.Value),

                new SqlParameter("@PersonaCancelacion",     (object?)viewModel.CancellationPerson ?? DBNull.Value),
                new SqlParameter("@OperadorCancelacion",    (object?)viewModel.CancellationOperator ?? DBNull.Value),
                new SqlParameter("@MotivoCancelacion",      (object?)viewModel.CancellationReason ?? DBNull.Value),

                new SqlParameter("@ModalidadServicio",      (object?)viewModel.ServiceModality ?? DBNull.Value),
                new SqlParameter("@Observaciones",          (object?)viewModel.Observations ?? DBNull.Value),
                new SqlParameter("@Clave",                  (object?)viewModel.KeyValue ?? DBNull.Value),
                new SqlParameter("@OperadorCgsId",          currentUserId),
                new SqlParameter("@SucursalCgs",            branchName),
                new SqlParameter("@IpOperador",             (object?)currentIP ?? DBNull.Value),

                new SqlParameter("@ValorBillete",           (object?)viewModel.BillValue ?? 0m),
                new SqlParameter("@ValorMoneda",            (object?)viewModel.CoinValue ?? 0m),
                new SqlParameter("@ValorServicio",          totalDeclared),
                new SqlParameter("@NumeroKitsCambio",       (object?)viewModel.NumberOfChangeKits ?? 0),
                new SqlParameter("@NumeroBolsasMoneda",     (object?)viewModel.NumberOfCoinBags ?? 0),
                new SqlParameter("@ArchivoDetalle",         (object?)viewModel.DetailFile ?? DBNull.Value),

                // ===== CefTransacciones =====
                new SqlParameter("@CefCodRuta",                    (object?)DBNull.Value),
                new SqlParameter("@CefNumeroPlanilla",             cefPlanilla),
                new SqlParameter("@CefDivisa",                     (object?)viewModel.Currency ?? DBNull.Value),
                new SqlParameter("@CefTipoTransaccion",            serviceConcept.TipoConcepto),
                new SqlParameter("@CefNumeroMesaConteo",           (object?)DBNull.Value),
                new SqlParameter("@CefCantidadBolsasDeclaradas",   declaredBags),
                new SqlParameter("@CefCantidadSobresDeclarados",   declaredEnv),
                new SqlParameter("@CefCantidadChequesDeclarados",  declaredChecks),
                new SqlParameter("@CefCantidadDocumentosDeclarados", declaredDocsCt),
                new SqlParameter("@CefValorBilletesDeclarado",     declaredBill),
                new SqlParameter("@CefValorMonedasDeclarado",      declaredCoin),
                new SqlParameter("@CefValorDocumentosDeclarado",   declaredDocs),
                new SqlParameter("@CefValorTotalDeclarado",        totalDeclared),
                new SqlParameter("@CefValorTotalDeclaradoLetras",  (object?)totalDeclaredLetters ?? DBNull.Value),
                new SqlParameter("@CefValorTotalContadoLetras",   (object?)totalCountedLetters ?? DBNull.Value),
                new SqlParameter("@CefNovedadInformativa",         (object?)DBNull.Value),
                new SqlParameter("@CefEsCustodia",                 false),
                new SqlParameter("@CefEsPuntoAPunto",              false),
                new SqlParameter("@CefEstadoTransaccion",          cefEstado),
                new SqlParameter("@CefFechaRegistro",              DateTime.Now),
                new SqlParameter("@CefUsuarioRegistroId",          currentUserId),
                new SqlParameter("@CefIPRegistro",                 (object?)currentIP ?? DBNull.Value),
                new SqlParameter("@CefReponsableEntregaId",        (object?)DBNull.Value),
                new SqlParameter("@CefResponsableRecibeId",        (object?)DBNull.Value),
            };

            var sql = "EXEC dbo.AddServiceTransaction "
                      + string.Join(", ", p.Select(x => x.ParameterName));

            var rows = await _context.Database.SqlQueryRaw<string>(sql, p).ToListAsync();
            var orden = rows.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(orden))
                return ServiceResult.FailureResult("No se pudo generar la Orden de Servicio.");

            return ServiceResult.SuccessResult($"Servicio creado: {orden}", orden);
        }

        public async Task<List<SelectListItem>> GetPointsByClientAndBranchAsync(int clientCode, int branchCode, int pointType)
        {
            var points = await _context.AdmPuntos
                                     .Where(p => p.ClientCode == clientCode &&
                                                 p.BranchCode == branchCode &&
                                                 p.PointType == pointType &&
                                                 p.Status == true)
                                     .Select(p => new SelectListItem { Value = p.PointCode, Text = p.PointName ?? p.PointCode })
                                     .OrderBy(p => p.Text)
                                     .ToListAsync();

            return points;
        }

        public async Task<List<SelectListItem>> GetFundsByClientAndBranchAsync(int clientCode, int branchCode, int fundType)
        {
            var funds = await _context.AdmFondos
                                    .Where(f => f.ClientCode == clientCode &&
                                                f.BranchCode == branchCode &&
                                                f.FundType == fundType &&
                                                f.FundStatus == true)
                                    .Select(f => new SelectListItem { Value = f.FundCode, Text = f.FundName ?? f.FundCode })
                                    .OrderBy(f => f.Text)
                                    .ToListAsync();

            return funds;
        }

        public async Task<dynamic?> GetPointDetailsAsync(string pointCode)
        {
            var pointDetails = await _context.AdmPuntos
                                             .Include(p => p.City)
                                             .Include(p => p.Branch)
                                             .Where(p => p.PointCode == pointCode)
                                             .Select(p => new
                                             {
                                                 PointName = p.PointName,
                                                 Address = p.Address,
                                                 CityName = p.City != null ? p.City.NombreCiudad : "N/A",
                                                 BranchName = p.Branch != null ? p.Branch.NombreSucursal : "N/A",
                                                 RangeAttentionInfo = p.RangeAttentionInfo,
                                                 Responsible = p.Responsible
                                             })
                                             .FirstOrDefaultAsync();
            return pointDetails;
        }

        public async Task<dynamic?> GetFundDetailsAsync(string fundCode)
        {
            var fundDetails = await _context.AdmFondos
                                            .Include(f => f.City)
                                            .Include(f => f.Branch)
                                            .Where(f => f.FundCode == fundCode)
                                            .Select(f => new
                                            {
                                                FundName = f.FundName,
                                                CityName = f.City != null ? f.City.NombreCiudad : "N/A",
                                                BranchName = f.Branch != null ? f.Branch.NombreSucursal : "N/A"
                                            })
                                            .FirstOrDefaultAsync();
            return fundDetails;
        }

        public async Task<Tuple<List<CgsServiceSummaryViewModel>, int>> GetFilteredServiceRequestsAsync(
            string? search, int? clientCode, int? branchCode, int? conceptCode, DateOnly? startDate, DateOnly? endDate, int? status,
            int page = 1, int pageSize = 10, string? currentUserId = null, bool isAdmin = false)
        {
            int? effectiveBranch = branchCode ?? _branchContext.CurrentBranchId;

            DateTime? start = startDate?.ToDateTime(TimeOnly.MinValue);
            DateTime? end = endDate?.ToDateTime(TimeOnly.MaxValue);

            var q = 
                from s in _context.CgsServicios.AsNoTracking()
                join c in _context.AdmClientes.AsNoTracking() on s.ClientCode equals c.ClientCode into cj
                from c in cj.DefaultIfEmpty()
                join b in _context.AdmSucursales.AsNoTracking() on s.BranchCode equals b.CodSucursal into bj
                from b in bj.DefaultIfEmpty()
                join cc in _context.AdmConceptos.AsNoTracking() on s.ConceptCode equals cc.CodConcepto into ccj
                from cc in ccj.DefaultIfEmpty()
                select new
                {
                    S = s,
                    ClientName = c != null ? c.ClientName : "",
                    BranchName = b != null ? b.NombreSucursal : "",
                    ConceptName = cc != null ? cc.NombreConcepto : ""
                };
            
            if (!isAdmin && effectiveBranch.HasValue)
            {
                q = q.Where(x => x.S.BranchCode == effectiveBranch.Value);
            }

            if (isAdmin && effectiveBranch.HasValue)
                q = q.Where(x => x.S.BranchCode == effectiveBranch.Value);

            if (clientCode.HasValue) q = q.Where(x => x.S.ClientCode == clientCode.Value);
            if (conceptCode.HasValue) q = q.Where(x => x.S.ConceptCode == conceptCode.Value);
            if (status.HasValue) q = q.Where(x => x.S.StatusCode == status.Value);

            if (start.HasValue) q = q.Where(x => x.S.RequestDate >= DateOnly.FromDateTime(start.Value));
            if (end.HasValue) q = q.Where(x => x.S.RequestDate <= DateOnly.FromDateTime(end.Value));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                q = q.Where(x =>
                    x.S.ServiceOrderId.Contains(term) ||
                    x.ClientName.Contains(term) ||
                    x.BranchName.Contains(term) ||
                    x.ConceptName.Contains(term));
            }

            int total = await q.CountAsync();

            var rows = await q
                .OrderByDescending(x => x.S.RequestDate)
                .ThenByDescending(x => x.S.RequestTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new CgsServiceSummaryViewModel
                {
                    ServiceOrderId = x.S.ServiceOrderId,
                    KeyValue = x.S.KeyValue ?? 0,
                    ClientName = x.ClientName,
                    BranchName = x.BranchName,
                    OriginPointName = x.S.OriginPointCode,
                    DestinationPointName = x.S.DestinationPointCode,
                    ConceptName = x.ConceptName,
                    RequestDate = x.S.RequestDate,
                    RequestTime = x.S.RequestTime,
                    ProgrammingDate = x.S.ProgrammingDate,
                    ProgrammingTime = x.S.ProgrammingTime,
                    StatusCode = x.S.StatusCode,
                    StatusName = x.S.StatusCode.ToString()
                })
                .ToListAsync();

            return Tuple.Create(rows, total);
        }

        // --- MÉTODOS PARA POPULAR DROPDOWNS --
        /// <inheritdoc/>
        public async Task<object?> GetLocationDetailsByCodeAsync(string code, int clientId, bool isPoint)
        {
            if (isPoint)
            {
                var point = await _context.AdmPuntos
                    .Include(p => p.City)
                    .Include(p => p.Branch)
                    .FirstOrDefaultAsync(p => p.PointCode == code && p.ClientCode == clientId);

                return point != null ? new
                {
                    cityName = point.City?.NombreCiudad,
                    branchName = point.Branch?.NombreSucursal,
                    rangeCode = point.RangeCode,
                    rangeDetails = point.RangeAttentionInfo ?? "N/A"
                } : null;
            }
            else // Es un fondo
            {
                var fund = await _context.AdmFondos
                    .Include(f => f.City)
                    .Include(f => f.Branch)
                    .FirstOrDefaultAsync(f => f.FundCode == code && f.ClientCode == clientId);

                return fund != null ? new
                {
                    cityName = fund.City?.NombreCiudad,
                    branchName = fund.Branch?.NombreSucursal,
                    rangeCode = "N/A",
                    rangeDetails = "N/A"
                } : null;
            }
        }

        /// <summary>
        /// Obtiene las opciones de cliente del servicio para los dropdowns.
        /// </summary>
        /// <returns>Lista de SelectListItem para los clientes</returns>
        public async Task<List<SelectListItem>> GetClientsForDropdownAsync()
        {
            return await _context.AdmClientes
                                 .Where(c => c.Status == true)
                                 .Select(c => new SelectListItem { Value = c.ClientCode.ToString(), Text = c.ClientName })
                                 .OrderBy(c => c.Text)
                                 .ToListAsync();
        }

        /// <summary>
        /// Obtiene las opciones de sucursal del servicio para los dropdowns.
        /// </summary>
        /// <returns>Lista de SelectListItem para las sucursales del servicio</returns>
        public async Task<List<SelectListItem>> GetBranchesForDropdownAsync()
        {
            return await _context.AdmSucursales
                                 .Where(s => s.Estado == true && s.CodSucursal != 32)
                                 .Select(s => new SelectListItem { Value = s.CodSucursal.ToString(), Text = s.NombreSucursal })
                                 .OrderBy(s => s.Text)
                                 .ToListAsync();
        }

        /// <summary>
        /// Obtiene las opciones de concepto del servicio para los dropdowns.
        /// </summary>
        /// <returns>Lista de SelectListItem para los conceptos del servicio</returns>
        public async Task<List<SelectListItem>> GetServiceConceptsForDropdownAsync()
        {
            return await _context.AdmConceptos
                                 .Select(c => new SelectListItem { Value = c.CodConcepto.ToString(), Text = c.NombreConcepto })
                                 .ToListAsync();
        }

        /// <summary>
        /// Obtiene las opciones de estado del servicio para los dropdowns.
        /// </summary>
        /// <returns>Lista de SelectListItem para los estados del servicio</returns>
        public async Task<List<SelectListItem>> GetServiceStatusesForDropdownAsync()
        {
            return await _context.AdmEstados
                                 .Select(e => new SelectListItem { Value = e.StateCode.ToString(), Text = e.StateName })
                                 .OrderBy(e => e.Text)
                                 .ToListAsync();
        }

        /// <summary>
        /// Obtiene las opciones de modalidad del servicio para los dropdowns.
        /// </summary>
        /// <returns>Lista de SelectListItem para las modalidades del servicio</returns>
        public async Task<List<SelectListItem>> GetServiceModalitiesForDropdownAsync()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Programado" },
                new SelectListItem { Value = "2", Text = "Pedido" },
                new SelectListItem { Value = "3", Text = "Frecuente" }
            };
        }

        /// <summary>
        /// Obtiene las opciones de responsables al fallo del servicio para los dropdowns.
        /// </summary>
        /// <returns>Lista de SelectListItem para los responsables del fallo del servicio</returns>
        public async Task<List<SelectListItem>> GetFailedResponsiblesForDropdown()
        {
            return await Task.FromResult(new List<SelectListItem>
            {
                new SelectListItem { Value = "Cliente", Text = "Cliente" },
                new SelectListItem { Value = "Vatco", Text = "Vatco" }
            });
        }

        /// <summary>
        /// Obtiene las opciones de tipo de ubicación para los dropdowns.
        /// </summary>
        /// <returns>Lista de SelectListItem para los tipos de ubicación</returns>
        public static List<SelectListItem> GetLocationTypeSelectList()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "P", Text = "Punto" },
                new SelectListItem { Value = "A", Text = "ATM" },
                new SelectListItem { Value = "F", Text = "Fondo" }
            };
        }

        private static List<SelectListItem> GetCurrenciesForDropdown()
        {
            return Enum.GetNames(typeof(CurrencyEnum))
                .Select(code => new SelectListItem { Value = code, Text = code })
                .ToList();
        }
    }
}