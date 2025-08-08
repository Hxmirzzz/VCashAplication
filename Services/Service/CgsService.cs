using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
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
            if (viewModel.ConceptCode == 5 && (viewModel.TransferType != "I" && viewModel.TransferType != "T"))
            {
                return ServiceResult.FailureResult("Para el concepto 'TRASLADO', el Tipo de Traslado debe ser 'Interno' o 'Transportadora'.");
            }
            else if (viewModel.ConceptCode != 5)
            {
                viewModel.TransferType = "N";
            }

            if (viewModel.OriginIndicatorType == "P" && !await _context.AdmPuntos.AnyAsync(p => p.PointCode == viewModel.OriginPointCode))
            {
                return ServiceResult.FailureResult($"El Código de Punto de Origen '{viewModel.OriginPointCode}' no es válido.");
            }
            if (viewModel.OriginIndicatorType == "F" && !await _context.AdmFondos.AnyAsync(f => f.FundCode == viewModel.OriginPointCode))
            {
                return ServiceResult.FailureResult($"El Código de Fondo de Origen '{viewModel.OriginPointCode}' no es válido.");
            }
            if (viewModel.DestinationIndicatorType == "P" && !await _context.AdmPuntos.AnyAsync(p => p.PointCode == viewModel.DestinationPointCode))
            {
                return ServiceResult.FailureResult($"El Código de Punto de Destino '{viewModel.DestinationPointCode}' no es válido.");
            }
            if (viewModel.DestinationIndicatorType == "F" && !await _context.AdmFondos.AnyAsync(f => f.FundCode == viewModel.DestinationPointCode))
            {
                return ServiceResult.FailureResult($"El Código de Fondo de Destino '{viewModel.DestinationPointCode}' no es válido.");
            }

            DateOnly? acceptanceDate;
            TimeOnly? acceptanceTime;

            if (viewModel.StatusCode == 2)
            {
                acceptanceDate = null;
                acceptanceTime = null;
            }
            else
            {
                acceptanceDate = viewModel.AcceptanceDate ?? DateOnly.FromDateTime(DateTime.Now);
                acceptanceTime = viewModel.AcceptanceTime ?? TimeOnly.FromDateTime(DateTime.Now);
            }

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
            var cgsOperatorName = currentUser?.NombreUsuario ?? "System";
            var cgsOperatorBranch = await _context.AdmSucursales
                                                  .Where(s => s.CodSucursal == viewModel.BranchCode)
                                                  .Select(s => s.NombreSucursal)
                                                  .FirstOrDefaultAsync() ?? "N/A";

            var numeroPedidoParam = new SqlParameter("@NumeroPedido", (object)viewModel.RequestNumber ?? DBNull.Value);
            var codClienteParam = new SqlParameter("@CodCliente", (object)viewModel.OriginClientCode ?? DBNull.Value);
            var codOsClienteParam = new SqlParameter("@CodOsCliente", (object)viewModel.ClientServiceOrderCode ?? DBNull.Value);
            var codSucParam = new SqlParameter("@CodSucursal", viewModel.BranchCode);
            var fechaSolicitudParam = new SqlParameter("@FechaSolicitud", viewModel.RequestDate.ToDateTime(TimeOnly.MinValue));
            var horaSolicitudParam = new SqlParameter("@HoraSolicitud", viewModel.RequestTime.ToTimeSpan());
            var codConceptoParam = new SqlParameter("@CodConcepto", viewModel.ConceptCode);
            var tipoTrasladoParam = new SqlParameter("@TipoTraslado", (object)viewModel.TransferType ?? DBNull.Value);
            var codEstadoParam = new SqlParameter("@CodEstado", viewModel.StatusCode);
            var codFlujoParam = new SqlParameter("@CodFlujo", (object)viewModel.ConceptCode ?? DBNull.Value);
            var codClienteOrigenParam = new SqlParameter("@CodClienteOrigen", (object)viewModel.OriginClientCode ?? DBNull.Value);
            var codPuntoOrigenParam = new SqlParameter("@CodPuntoOrigen", viewModel.OriginPointCode);
            var indicadorTipoOrigenParam = new SqlParameter("@IndicadorTipoOrigen", viewModel.OriginIndicatorType);
            var codClienteDestinoParam = new SqlParameter("@CodClienteDestino", (object)viewModel.DestinationClientCode ?? DBNull.Value);
            var codPuntoDestinoParam = new SqlParameter("@CodPuntoDestino", viewModel.DestinationPointCode);
            var indicadorTipoDestinoParam = new SqlParameter("@IndicadorTipoDestino", viewModel.DestinationIndicatorType);
            var fechaAceptacionParam = new SqlParameter("@FechaAceptacion", (object)acceptanceDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
            var horaAceptacionParam = new SqlParameter("@HoraAceptacion", (object)acceptanceTime?.ToTimeSpan() ?? DBNull.Value);
            var fechaProgramacionParam = new SqlParameter("@FechaProgramacion", (object)viewModel.ProgrammingDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
            var horaProgramacionParam = new SqlParameter("@HoraProgramacion", (object)viewModel.ProgrammingTime?.ToTimeSpan() ?? DBNull.Value);
            var fechaAtencionInicialParam = new SqlParameter("@FechaAtencionInicial", (object)viewModel.InitialAttentionDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
            var horaAtencionInicialParam = new SqlParameter("@HoraAtencionInicial", (object)viewModel.InitialAttentionTime?.ToTimeSpan() ?? DBNull.Value);
            var fechaAtencionFinalParam = new SqlParameter("@FechaAtencionFinal", (object)viewModel.FinalAttentionDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
            var horaAtencionFinalParam = new SqlParameter("@HoraAtencionFinal", (object)viewModel.FinalAttentionTime?.ToTimeSpan() ?? DBNull.Value);
            var fechaCancelacionParam = new SqlParameter("@FechaCancelacion", (object)viewModel.CancellationDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
            var horaCancelacionParam = new SqlParameter("@HoraCancelacion", (object)viewModel.CancellationTime?.ToTimeSpan() ?? DBNull.Value);
            var fechaRechazoParam = new SqlParameter("@FechaRechazo", (object)viewModel.RejectionDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
            var horaRechazoParam = new SqlParameter("@HoraRechazo", (object)viewModel.RejectionTime?.ToTimeSpan() ?? DBNull.Value);
            var fallidoParam = new SqlParameter("@Fallido", viewModel.IsFailed);
            var responsableFallidoParam = new SqlParameter("@ResponsableFallido", (object)viewModel.FailedResponsible ?? DBNull.Value);
            var razonFallidoParam = new SqlParameter("@RazonFallido", (object)viewModel.FailedReason ?? DBNull.Value);
            var personaCancelacionParam = new SqlParameter("@PersonaCancelacion", (object)viewModel.CancellationPerson ?? DBNull.Value);
            var operadorCancelacionParam = new SqlParameter("@OperadorCancelacion", (object)viewModel.CancellationOperator ?? DBNull.Value);
            var motivoCancelacionParam = new SqlParameter("@MotivoCancelacion", (object)viewModel.CancellationReason ?? DBNull.Value);
            var modalidadServicioParam = new SqlParameter("@ModalidadServicio", (object)viewModel.ServiceModality ?? DBNull.Value);
            var observacionesParam = new SqlParameter("@Observaciones", (object)viewModel.Observations ?? DBNull.Value);
            var claveParam = new SqlParameter("@Clave", (object)viewModel.KeyValue ?? DBNull.Value);
            var operadorCgsIdParam = new SqlParameter("@OperadorCgsId", currentUserId);
            var sucursalCgsParam = new SqlParameter("@SucursalCgs", cgsOperatorBranch);
            var ipOperadorParam = new SqlParameter("@IpOperador", currentIP);
            var valorBilleteParam = new SqlParameter("@ValorBillete", (object)viewModel.BillValue ?? DBNull.Value);
            var valorMonedaParam = new SqlParameter("@ValorMoneda", (object)viewModel.CoinValue ?? DBNull.Value);
            var valorServicioParam = new SqlParameter("@ValorServicio", viewModel.BillValue + viewModel.CoinValue);
            var numeroKitsCambioParam = new SqlParameter("@NumeroKitsCambio", (object)viewModel.NumberOfChangeKits ?? DBNull.Value);
            var numeroBolsasMonedaParam = new SqlParameter("@NumeroBolsasMoneda", (object)viewModel.NumberOfCoinBags ?? DBNull.Value);
            var archivoDetalleParam = new SqlParameter("@ArchivoDetalle", (object)viewModel.DetailFile ?? DBNull.Value);

            try
            {
                var sqlQuery = "EXEC dbo.AddCgsService " +
                               "@NumeroPedido, @CodCliente, @CodOsCliente, @CodSucursal, " +
                               "@FechaSolicitud, @HoraSolicitud, @CodConcepto, @TipoTraslado, @CodEstado, @CodFlujo, " +
                               "@CodClienteOrigen, @CodPuntoOrigen, @IndicadorTipoOrigen, " +
                               "@CodClienteDestino, @CodPuntoDestino, @IndicadorTipoDestino, " +
                               "@FechaAceptacion, @HoraAceptacion, @FechaProgramacion, @HoraProgramacion, " +
                               "@FechaAtencionInicial, @HoraAtencionInicial, @FechaAtencionFinal, @HoraAtencionFinal, " +
                               "@FechaCancelacion, @HoraCancelacion, @FechaRechazo, @HoraRechazo, " +
                               "@Fallido, @ResponsableFallido, @RazonFallido, @PersonaCancelacion, @OperadorCancelacion, @MotivoCancelacion, " +
                               "@ModalidadServicio, @Observaciones, @Clave, @OperadorCgsId, @SucursalCgs, @IpOperador, " +
                               "@ValorBillete, @ValorMoneda, @ValorServicio, @NumeroKitsCambio, @NumeroBolsasMoneda, @ArchivoDetalle";

                var result = (await _context.Database
                    .SqlQueryRaw<string>(sqlQuery,
                        numeroPedidoParam, codClienteParam, codOsClienteParam, codSucParam,
                        fechaSolicitudParam, horaSolicitudParam, codConceptoParam, tipoTrasladoParam, codEstadoParam, codFlujoParam,
                        codClienteOrigenParam, codPuntoOrigenParam, indicadorTipoOrigenParam,
                        codClienteDestinoParam, codPuntoDestinoParam, indicadorTipoDestinoParam,
                        fechaAceptacionParam, horaAceptacionParam, fechaProgramacionParam, horaProgramacionParam,
                        fechaAtencionInicialParam, horaAtencionInicialParam, fechaAtencionFinalParam, horaAtencionFinalParam,
                        fechaCancelacionParam, horaCancelacionParam, fechaRechazoParam, horaRechazoParam,
                        fallidoParam, responsableFallidoParam, razonFallidoParam, personaCancelacionParam, operadorCancelacionParam, motivoCancelacionParam,
                        modalidadServicioParam, observacionesParam, claveParam, operadorCgsIdParam, sucursalCgsParam, ipOperadorParam,
                        valorBilleteParam, valorMonedaParam, valorServicioParam, numeroKitsCambioParam, numeroBolsasMonedaParam, archivoDetalleParam)
                    .ToListAsync())
                    .FirstOrDefault();


                if (string.IsNullOrEmpty(result))
                {
                    return ServiceResult.FailureResult("Error al generar el número de Orden de Servicio.");
                }

                return ServiceResult.SuccessResult("Solicitud de servicio creada exitosamente.", new { serviceOrderId = result });
            }
            catch (SqlException sqlEx)
            {
                return ServiceResult.FailureResult($"Error de base de datos al crear la solicitud: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                return ServiceResult.FailureResult($"Ocurrió un error inesperado al crear la solicitud: {ex.Message}");
            }
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
    }
}