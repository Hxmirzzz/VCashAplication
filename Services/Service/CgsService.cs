using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using VCashApp.Utils;
using VCashApp.Data;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.Servicio;
using VCashApp.Services.DTOs;

namespace VCashApp.Services.Service
{
    public class CgsService : ICgsServiceService
    {
        private readonly AppDbContext _context;

        public CgsService(AppDbContext context)
        {
            _context = context;
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

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
            viewModel.CgsOperatorUserName = currentUser?.NombreUsuario ?? "Desconocido";
            viewModel.OperatorIpAddress = currentIP;

            var operatorBranchCodeClaim = await _context.UserClaims
                .Where(uc => uc.UserId == currentUserId && uc.ClaimType == "SucursalId")
                .Select(uc => uc.ClaimValue)
                .FirstOrDefaultAsync();

            if (int.TryParse(operatorBranchCodeClaim, out int operatorBranchCode))
            {
                var operatorBranch = await _context.AdmSucursales
                    .FirstOrDefaultAsync(s => s.CodSucursal == operatorBranchCode);
                viewModel.OperatorBranchName = operatorBranch?.NombreSucursal ?? "N/A";
            }
            else
            {
                viewModel.OperatorBranchName = "N/A";
            }

            viewModel.RequestDate = DateOnly.FromDateTime(DateTime.Now);
            viewModel.RequestTime = TimeOnly.FromDateTime(DateTime.Now);
            viewModel.ProgrammingDate = DateOnly.FromDateTime(DateTime.Now);
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
            var declaredDocs = 0m;                         // si no lo capturas aún, va 0
            var totalDeclared = declaredBill + declaredCoin + declaredDocs;
            var totalDeclaredLetters = AmountInWordsHelper.ToSpanishCurrency(totalDeclared, "COP");

            var declaredBags = viewModel.NumberOfCoinBags ?? 0;
            var declaredEnv = 0;
            var declaredChecks = 0;
            var declaredDocsCt = 0;

            var cefEstado = "Registrada";
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
                new SqlParameter("@ValorServicio",          viewModel.BillValue + viewModel.CoinValue),
                new SqlParameter("@NumeroKitsCambio",       (object?)viewModel.NumberOfChangeKits ?? 0),
                new SqlParameter("@NumeroBolsasMoneda",     (object?)viewModel.NumberOfCoinBags ?? 0),
                new SqlParameter("@ArchivoDetalle",         (object?)viewModel.DetailFile ?? DBNull.Value),

                // ===== CefTransacciones =====
                new SqlParameter("@CefCodRuta",                    (object?)DBNull.Value),
                new SqlParameter("@CefNumeroPlanilla",             cefPlanilla),
                new SqlParameter("@CefDivisa",                     (object?)DBNull.Value),
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
            var permittedBranchIds = new List<int>();
            if (!isAdmin && !string.IsNullOrEmpty(currentUserId))
            {
                permittedBranchIds = await _context.UserClaims
                    .Where(uc => uc.UserId == currentUserId && uc.ClaimType == "SucursalId")
                    .Select(uc => int.Parse(uc.ClaimValue))
                    .ToListAsync();
            }

            var tvpTable = new DataTable();
            tvpTable.Columns.Add("Value", typeof(int));
            if (permittedBranchIds.Any())
            {
                foreach (int id in permittedBranchIds)
                {
                    tvpTable.Rows.Add(id);
                }
            }

            var pPermittedBranchIds = new SqlParameter("@PermittedBranchIds", tvpTable)
            {
                TypeName = "dbo.IntListType",
                SqlDbType = SqlDbType.Structured
            };
            if (!permittedBranchIds.Any() && !isAdmin)
            {
                pPermittedBranchIds.Value = DBNull.Value;
            }

            var pBranchIdFilter = new SqlParameter("@BranchIdFilter", branchCode ?? (object)DBNull.Value);
            var pClientCode = new SqlParameter("@ClientCode", clientCode ?? (object)DBNull.Value);
            var pConcepto = new SqlParameter("@Concepto", conceptCode ?? (object)DBNull.Value);
            var pStartDate = new SqlParameter("@StartDate", startDate.HasValue ? (object)startDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
            var pEndDate = new SqlParameter("@EndDate", endDate.HasValue ? (object)endDate.Value.ToDateTime(TimeOnly.MaxValue) : DBNull.Value);
            var pStatus = new SqlParameter("@Status", status ?? (object)DBNull.Value);
            var pSearchTerm = new SqlParameter("@SearchTerm", string.IsNullOrEmpty(search) ? (object)DBNull.Value : search);
            var pPage = new SqlParameter("@Page", page);
            var pPageSize = new SqlParameter("@PageSize", pageSize);

            var servicesSummary = new List<CgsServiceSummaryViewModel>();
            var totalRecords = 0;

            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "dbo.GetFilteredCgsServices";
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddRange(new[] {
                    pPermittedBranchIds, pBranchIdFilter, pClientCode, pConcepto,
                    pStartDate, pEndDate, pStatus, pSearchTerm, pPage, pPageSize
                });

                await _context.Database.OpenConnectionAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        totalRecords = reader.GetInt32(0);
                    }
                    await reader.NextResultAsync();

                    while (await reader.ReadAsync())
                    {
                        servicesSummary.Add(new CgsServiceSummaryViewModel
                        {
                            ServiceOrderId = reader.IsDBNull(reader.GetOrdinal("ServiceOrderId")) ? string.Empty : reader.GetString(reader.GetOrdinal("ServiceOrderId")),
                            KeyValue = reader.IsDBNull(reader.GetOrdinal("KeyValue")) ? 0 : reader.GetInt32(reader.GetOrdinal("KeyValue")),
                            ClientName = reader.IsDBNull(reader.GetOrdinal("ClientName")) ? string.Empty : reader.GetString(reader.GetOrdinal("ClientName")),
                            BranchName = reader.IsDBNull(reader.GetOrdinal("BranchName")) ? string.Empty : reader.GetString(reader.GetOrdinal("BranchName")),
                            OriginPointName = reader.IsDBNull(reader.GetOrdinal("OriginPointName")) ? string.Empty : reader.GetString(reader.GetOrdinal("OriginPointName")),
                            DestinationPointName = reader.IsDBNull(reader.GetOrdinal("DestinationPointName")) ? string.Empty : reader.GetString(reader.GetOrdinal("DestinationPointName")),
                            ConceptName = reader.IsDBNull(reader.GetOrdinal("ConceptName")) ? string.Empty : reader.GetString(reader.GetOrdinal("ConceptName")),
                            RequestDate = DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("RequestDate"))),
                            RequestTime = TimeOnly.FromTimeSpan(reader.GetFieldValue<TimeSpan>(reader.GetOrdinal("RequestTime"))),
                            ProgrammingDate = reader.IsDBNull(reader.GetOrdinal("ProgrammingDate")) ? null : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("ProgrammingDate"))),
                            ProgrammingTime = reader.IsDBNull(reader.GetOrdinal("ProgrammingTime")) ? null : TimeOnly.FromTimeSpan(reader.GetFieldValue<TimeSpan>(reader.GetOrdinal("ProgrammingTime"))),
                            StatusCode = reader.IsDBNull(reader.GetOrdinal("StatusCode")) ? 0 : reader.GetInt32(reader.GetOrdinal("StatusCode")),
                            StatusName = reader.IsDBNull(reader.GetOrdinal("StatusName")) ? string.Empty : reader.GetString(reader.GetOrdinal("StatusName"))
                        });
                    }
                }
            }
            return Tuple.Create(servicesSummary, totalRecords);
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
                    rangeCode = point.RangeCode ?? "N/A",
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
    }
}