using VCashApp.Data;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.CentroEfectivo;
using VCashApp.Enums;
using VCashApp.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using Microsoft.Data.SqlClient;
using System;

namespace VCashApp.Services.Cef
{
    /// <summary>
    /// Implementación del servicio para la creación unificada temporal de Servicios y Transacciones CEF.
    /// </summary>
    public class CefServiceCreationService : ICefServiceCreationService
    {
        private readonly AppDbContext _context;

        public CefServiceCreationService(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<(List<SelectListItem> AvailableBranches, List<SelectListItem> AvailableServiceModalities, List<SelectListItem> AvailableFailedReponsibles)> GetDropdownListsAsync(string currentUserId, bool isAdmin)
        {
            var allActiveBranches = await _context.AdmSucursales
                .Where(s => s.Estado && s.CodSucursal != 32)
                .Select(s => new { s.CodSucursal, s.NombreSucursal })
                .ToListAsync();

            List<SelectListItem> permittedBranchesList;

            if (!isAdmin)
            {
                var permittedBranchIds = await _context.UserClaims
                    .Where(uc => uc.UserId == currentUserId && uc.ClaimType == "SucursalId")
                    .Select(uc => int.Parse(uc.ClaimValue))
                    .ToListAsync();

                permittedBranchesList = allActiveBranches
                    .Where(s => permittedBranchIds.Contains(s.CodSucursal))
                    .Select(s => new SelectListItem { Value = s.CodSucursal.ToString(), Text = s.NombreSucursal })
                    .ToList();
            }
            else
            {
                permittedBranchesList = allActiveBranches
                    .Select(s => new SelectListItem { Value = s.CodSucursal.ToString(), Text = s.NombreSucursal })
                    .ToList();
            }

            var serviceModalities = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "PROGRAMADO" },
                new SelectListItem { Value = "2", Text = "A PEDIDO" },
                new SelectListItem { Value = "3", Text = "FRECUENTE" }
            };

            var serviceCurrencies = new List<SelectListItem>
            {
                new SelectListItem { Value = "COP", Text = "COP" },
                new SelectListItem { Value = "USD", Text = "USD" },
                new SelectListItem { Value = "EUR", Text = "EUR" }
            };

            var failedResponsibles = new List<SelectListItem>
            {
                new SelectListItem { Value = "Cliente", Text = "Cliente" },
                new SelectListItem { Value = "Vatco", Text = "Vatco" }
            };

            return (permittedBranchesList, serviceModalities, failedResponsibles);
        }

        /// <inheritdoc/>
        public async Task<CefServiceCreationViewModel> PrepareCefServiceCreationViewModelAsync(string currentUserId, string currentIP, string? initialServiceConceptCode = null)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
            string userName = user?.NombreUsuario ?? "Desconocido";

            var viewModel = new CefServiceCreationViewModel
            {
                RegistrationDate = DateTime.Now,
                RegistrationUserName = userName,
                IPAddress = currentIP,
                RequestDate = DateOnly.FromDateTime(DateTime.Now),
                RequestTime = TimeOnly.FromDateTime(DateTime.Now),
                ProgrammingDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
                ProgrammingTime = TimeOnly.FromDateTime(DateTime.Now),

                // Inicializar listas de SelectListItem
                AvailableServiceConcepts = await GetServiceConceptsForDropdownAsync(),
                AvailableBranches = await _context.AdmSucursales
                                                    .Where(s => s.Estado && s.CodSucursal != 32)
                                                    .Select(s => new SelectListItem { Value = s.CodSucursal.ToString(), Text = s.NombreSucursal })
                                                    .ToListAsync(),
                AvailableClients = await GetClientsForDropdownAsync(),
                AvailableOriginPoints = new List<SelectListItem>(),
                AvailableOriginFunds = new List<SelectListItem>(),
                AvailableDestinationPoints = new List<SelectListItem>(),
                AvailableDestinationFunds = new List<SelectListItem>(),
                AvailableCities = await _context.AdmCiudades
                                                .Where(c => c.Estado) // Asumo que AdmCiudad tiene una propiedad 'Estado'
                                                .Select(c => new SelectListItem { Value = c.CodCiudad.ToString(), Text = c.NombreCiudad })
                                                .ToListAsync(),
                /*AvailableRanks = await _context.AdmRangos
                                                .Where(r => r.RangoEstado == 1) // Asumo que AdmRango tiene una propiedad 'RangoEstado'
                                                .Select(r => new SelectListItem { Value = r.CodRango, Text = r.InfoRangoAtencion })
                                                .ToListAsync(),*/
                AvailableEmployees = new List<SelectListItem>(),
                AvailableVehicles = new List<SelectListItem>(),
                AvailableServiceModalities = await GetServiceModalitiesForDropdownAsync(),
                AvailableCurrencies = await GetCurrenciesForDropdownAsync(),
                AvailableFailedResponsibles = await GetFailedResponsiblesForDropdown()
            };

            if (!string.IsNullOrEmpty(initialServiceConceptCode))
            {
                viewModel.ServiceConceptCode = initialServiceConceptCode;
            }

            return viewModel;
        }

        // Reemplaza el método existente en CefServiceCreationService.cs

        /// <inheritdoc/>
        public async Task<string> ProcessCefServiceCreationAsync(CefServiceCreationViewModel viewModel, string currentUserId, string currentIP)
        {
            if (viewModel.OriginClientId == viewModel.DestinationClientId && viewModel.OriginCode == viewModel.DestinationCode)
            {
                throw new InvalidOperationException("El punto de origen y el de destino no pueden ser el mismo.");
            }

            var serviceConcept = await _context.AdmConceptos.AsNoTracking().FirstOrDefaultAsync(c => c.TipoConcepto == viewModel.ServiceConceptCode);
            if (serviceConcept == null)
            {
                throw new InvalidOperationException("Código de concepto de servicio no válido.");
            }

            var parameters = new List<SqlParameter>
            {
                // Parámetros para CgsServicios
                new SqlParameter("@NumeroPedido", (object)viewModel.ClientOrderNumber ?? DBNull.Value),
                new SqlParameter("@CodCliente", viewModel.OriginClientId),
                new SqlParameter("@CodOsCliente", (object)viewModel.ClientServiceOrderCode ?? DBNull.Value),
                new SqlParameter("@CodSucursal", viewModel.BranchId),
                new SqlParameter("@FechaSolicitud", viewModel.RequestDate),
                new SqlParameter("@HoraSolicitud", viewModel.RequestTime),
                new SqlParameter("@CodConcepto", serviceConcept.CodConcepto),
                new SqlParameter("@TipoTraslado", (object)viewModel.ServiceModality ?? DBNull.Value),
                new SqlParameter("@CodEstado", '0'), 
                new SqlParameter("@CodFlujo", 1),
                new SqlParameter("@CodClienteOrigen", viewModel.OriginClientId),
                new SqlParameter("@CodPuntoOrigen", viewModel.OriginCode),
                new SqlParameter("@IndicadorTipoOrigen", viewModel.OriginType.ToString().Substring(0, 1)),
                new SqlParameter("@CodClienteDestino", viewModel.DestinationClientId),
                new SqlParameter("@CodPuntoDestino", viewModel.DestinationCode),
                new SqlParameter("@IndicadorTipoDestino", viewModel.DestinationType.ToString().Substring(0, 1)),
                new SqlParameter("@FechaAceptacion", DBNull.Value),
                new SqlParameter("@HoraAceptacion", DBNull.Value),
                new SqlParameter("@FechaProgramacion", viewModel.ProgrammingDate),
                new SqlParameter("@HoraProgramacion", viewModel.ProgrammingTime),
                new SqlParameter("@FechaAtencionInicial", DBNull.Value),
                new SqlParameter("@HoraAtencionInicial", DBNull.Value),
                new SqlParameter("@FechaAtencionFinal", DBNull.Value),
                new SqlParameter("@HoraAtencionFinal", DBNull.Value),
                new SqlParameter("@FechaCancelacion", DBNull.Value),
                new SqlParameter("@HoraCancelacion", DBNull.Value),
                new SqlParameter("@FechaRechazo", DBNull.Value),
                new SqlParameter("@HoraRechazo", DBNull.Value),
                new SqlParameter("@Fallido", viewModel.IsFailed),
                new SqlParameter("@ResponsableFallido", (object)viewModel.FailedResponsible ?? DBNull.Value),
                new SqlParameter("@RazonFallido", (object)viewModel.FailedReason ?? DBNull.Value),
                new SqlParameter("@PersonaCancelacion", DBNull.Value),
                new SqlParameter("@OperadorCancelacion", DBNull.Value),
                new SqlParameter("@MotivoCancelacion", DBNull.Value),
                new SqlParameter("@ModalidadServicio", (object)viewModel.ServiceModality ?? DBNull.Value),
                new SqlParameter("@Observaciones", (object)viewModel.ServiceObservations ?? DBNull.Value),
                new SqlParameter("@Clave", DBNull.Value),
                new SqlParameter("@OperadorCgsId", currentUserId),
                new SqlParameter("@SucursalCgs", DBNull.Value),
                new SqlParameter("@IpOperador", (object)currentIP ?? DBNull.Value),
                new SqlParameter("@ValorBillete", (object)viewModel.DeclaredBillValue),
                new SqlParameter("@ValorMoneda", (object)viewModel.DeclaredCoinValue),
                new SqlParameter("@ValorServicio", (object)viewModel.ServiceValue ?? DBNull.Value),
                new SqlParameter("@NumeroKitsCambio", (object)viewModel.ExchangeKitCount ?? DBNull.Value),
                new SqlParameter("@NumeroBolsasMoneda", (object)viewModel.DeclaredBagCount),
                new SqlParameter("@ArchivoDetalle", DBNull.Value),

                // Parámetros para CefTransacciones
                new SqlParameter("@CefCodRuta", DBNull.Value),
                new SqlParameter("@CefNumeroPlanilla", viewModel.SlipNumber),
                new SqlParameter("@CefDivisa", viewModel.Currency),
                new SqlParameter("@CefTipoTransaccion", viewModel.ServiceConceptCode),
                new SqlParameter("@CefNumeroMesaConteo", DBNull.Value),
                new SqlParameter("@CefCantidadBolsasDeclaradas", viewModel.DeclaredBagCount),
                new SqlParameter("@CefCantidadSobresDeclarados", viewModel.DeclaredEnvelopeCount),
                new SqlParameter("@CefCantidadChequesDeclarados", viewModel.DeclaredCheckCount),
                new SqlParameter("@CefCantidadDocumentosDeclarados", viewModel.DeclaredDocumentCount),
                new SqlParameter("@CefValorBilletesDeclarado", viewModel.DeclaredBillValue),
                new SqlParameter("@CefValorMonedasDeclarado", viewModel.DeclaredCoinValue),
                new SqlParameter("@CefValorDocumentosDeclarado", viewModel.DeclaredDocumentValue),
                new SqlParameter("@CefValorTotalDeclarado", viewModel.DeclaredBillValue + viewModel.DeclaredCoinValue + viewModel.DeclaredDocumentValue),
                new SqlParameter("@CefValorTotalDeclaradoLetras", DBNull.Value),
                new SqlParameter("@CefNovedadInformativa", (object)viewModel.InformativeIncident ?? DBNull.Value),
                new SqlParameter("@CefEsCustodia", viewModel.IsCustody),
                new SqlParameter("@CefEsPuntoAPunto", viewModel.IsPointToPoint),
                new SqlParameter("@CefEstadoTransaccion", CefTransactionStatusEnum.Checkin.ToString()),
                new SqlParameter("@CefFechaRegistro", DateTime.Now),
                new SqlParameter("@CefUsuarioRegistroId", currentUserId),
                new SqlParameter("@CefIPRegistro", (object)currentIP ?? DBNull.Value)
            };

            var parameterNames = string.Join(", ", parameters.Select(p => p.ParameterName));
            var sql = $"EXEC [dbo].[AddServicioAndCefTransaction] {parameterNames}";

            var result = await _context.Database
                                       .SqlQueryRaw<string>(sql, parameters.ToArray())
                                       .ToListAsync();

            var newServiceOrderId = result.FirstOrDefault();

            if (string.IsNullOrEmpty(newServiceOrderId))
            {
                throw new InvalidOperationException("La ejecución del procedimiento almacenado no devolvió un ID de Orden de Servicio.");
            }

            return newServiceOrderId;
        }

        // --- MÉTODOS AUXILIARES PARA DROPDOWNS Y DETALLES (Se expandirán según la necesidad) ---

        /// <inheritdoc/>
        public async Task<List<SelectListItem>> GetServiceConceptsForDropdownAsync()
        {
            return await _context.AdmConceptos
                                 .Where(c => c.CodConcepto >= 1 && c.CodConcepto <= 4)
                                 .Select(c => new SelectListItem { Value = c.TipoConcepto, Text = c.NombreConcepto })
                                 .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<SelectListItem>> GetClientsForDropdownAsync()
        {
            return await _context.AdmClientes
                                 .Where(c => c.Status)
                                 .Select(c => new SelectListItem { Value = c.ClientCode.ToString(), Text = c.ClientName })
                                 .ToListAsync();
        }


        /// <inheritdoc/>
        // Reemplaza el método existente con este
        public async Task<List<SelectListItem>> GetLocationsForDropdownAsync(int clientId, int? branchId, LocationTypeEnum locationType, string? serviceConceptCode)
        {
            if (locationType == LocationTypeEnum.Punto)
            {
                // Puntos oficina (TipoPunto = 0)
                IQueryable<AdmPunto> query = _context.AdmPuntos
                    .Where(p => p.ClientCode == clientId && p.Status && p.PointType == 0);

                if (branchId.HasValue && branchId.Value > 0)
                {
                    query = query.Where(p => p.BranchCode == branchId.Value);
                }

                return await query
                    .Select(p => new SelectListItem
                    {
                        Value = p.PointCode,
                        Text = $"{p.PointName} ({p.PointCode})"
                    })
                    .ToListAsync();
            }
            else if (locationType == LocationTypeEnum.ATM)
            {
                // ATMs (TipoPunto = 1)
                IQueryable<AdmPunto> query = _context.AdmPuntos
                    .Where(p => p.ClientCode == clientId && p.Status && p.PointType == 1);

                if (branchId.HasValue && branchId.Value > 0)
                {
                    query = query.Where(p => p.BranchCode == branchId.Value);
                }

                return await query
                    .Select(p => new SelectListItem
                    {
                        Value = p.PointCode,
                        Text = $"{p.PointName} ({p.PointCode})"
                    })
                    .ToListAsync();
            }
            else if (locationType == LocationTypeEnum.Fondo)
            {
                // Fondos (tabla AdmFondos)
                IQueryable<AdmFondo> query = _context.AdmFondos
                    .Where(f => f.ClientCode == clientId && f.FundStatus);

                if (branchId.HasValue && branchId.Value > 0)
                {
                    query = query.Where(f => f.BranchCode == branchId.Value);
                }

                if (!string.IsNullOrEmpty(serviceConceptCode))
                {
                    if (serviceConceptCode == "RC" || serviceConceptCode == "PV")
                    {
                        query = query.Where(f => f.FundType == 0);
                    }
                    else if (serviceConceptCode == "PR" || serviceConceptCode == "ET")
                    {
                        query = query.Where(f => f.FundType == 1);
                    }
                }

                return await query
                    .Select(f => new SelectListItem
                    {
                        Value = f.FundCode,
                        Text = $"{f.FundName} ({f.FundCode})"
                    })
                    .ToListAsync();
            }

            return new List<SelectListItem>();
        }

        /// <inheritdoc/>
        public async Task<List<SelectListItem>> GetFundsForDropdownAsync(int clientId, int? branchId)
        {
            IQueryable<AdmFondo> query = _context.AdmFondos.Where(f => f.ClientCode == clientId && f.FundStatus);

            if (branchId.HasValue && branchId.Value > 0)
            {
                query = query.Where(f => f.BranchCode == branchId.Value);
            }

            return await query.Select(f => new SelectListItem { Value = f.FundCode, Text = $"{f.FundName} ({f.FundCode})" }).ToListAsync();
        }

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
                    name = point.PointName,
                    cityName = point.City?.NombreCiudad,
                    cityId = point.CityCode,
                    branchId = point.BranchCode,
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
                    name = fund.FundName,
                    cityName = fund.City?.NombreCiudad,
                    cityId = fund.CityCode,
                    branchId = fund.BranchCode,
                    branchName = fund.Branch?.NombreSucursal,
                    rangeCode = "N/A",
                    rangeDetails = "N/A"
                } : null;
            }
        }

        /// <inheritdoc/>
        public async Task<List<SelectListItem>> GetEmployeesForDropdownAsync(int branchId, int cargoId)
        {
            return await _context.AdmEmpleados
                                 .Where(e => e.CodSucursal == branchId && e.CodCargo == cargoId && e.EmpleadoEstado == Enums.EstadoEmpleado.Activo)
                                 .Select(e => new SelectListItem { Value = e.CodCedula.ToString(), Text = $"{e.NombreCompleto} ({e.CodCedula})" })
                                 .ToListAsync();
        }

        /// <summary>
        /// Obtiene las modalidades de servicio disponibles para los dropdowns.
        /// </summary>
        /// <returns>Lista de SelectListItem para modalidades de servicio.</returns>
        ///
        public async Task<List<SelectListItem>> GetServiceModalitiesForDropdownAsync()
        {
            return await Task.FromResult(new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "PROGRAMADO" },
                new SelectListItem { Value = "2", Text = "A PEDIDO" },
                new SelectListItem { Value = "3", Text = "FRECUENTE" }
            });
        }

        /// <summary>
        /// Obtiene las divisas disponibles para los dropdowns.
        /// </summary>
        /// <returns>Lista de SelectListItem para las divisas</returns>
        public async Task<List<SelectListItem>> GetCurrenciesForDropdownAsync()
        {
            return await Task.FromResult(new List<SelectListItem>
            {
                new SelectListItem { Value = "COP", Text = "COP" },
                new SelectListItem { Value = "USD", Text = "USD" },
                new SelectListItem { Value = "EUR", Text = "EUR" }
            });
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