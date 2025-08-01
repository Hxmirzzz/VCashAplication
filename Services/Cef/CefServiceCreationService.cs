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
                AvailableServiceModalities = await GetServiceModalitiesForDropdownAsync()
            };

            if (!string.IsNullOrEmpty(initialServiceConceptCode))
            {
                viewModel.ServiceConceptCode = initialServiceConceptCode;
            }

            return viewModel;
        }

        /// <inheritdoc/>
        public async Task<string> ProcessCefServiceCreationAsync(CefServiceCreationViewModel viewModel, string currentUserId, string currentIP)
        {
            if (viewModel.OriginClientId == viewModel.DestinationClientId && viewModel.OriginCode == viewModel.DestinationCode)
            {
                throw new InvalidOperationException("El punto de origen y el de destino no pueden ser el mismo.");
            }

            var resultList = await _context.Database
                                            .SqlQueryRaw<string>("EXEC GenerarNuevoOrdenServicioId @prefijo = 'S';")
                                            .AsNoTracking()
                                            .ToListAsync();
            var newServiceOrderId = resultList.FirstOrDefault();

            if (string.IsNullOrEmpty(newServiceOrderId))
            {
                throw new InvalidOperationException("No se pudo generar un nuevo ID de Orden de Servicio.");
            }

            var serviceConcept = await _context.AdmConceptos.FirstOrDefaultAsync(c => c.TipoConcepto == viewModel.ServiceConceptCode);
            if (serviceConcept == null) throw new InvalidOperationException("Código de concepto de servicio no válido.");

            int statusCode = 0;

            var cgsService = new CgsService
            {
                ServiceOrderId = newServiceOrderId,
                RequestNumber = viewModel.ClientOrderNumber,
                ClientCode = viewModel.OriginClientId,
                ClientServiceOrderCode = viewModel.ClientServiceOrderCode,
                BranchCode = viewModel.BranchId,
                RequestDate = viewModel.RequestDate,
                RequestTime = viewModel.RequestTime,
                ConceptCode = serviceConcept.CodConcepto,
                TransferType = viewModel.ServiceModality,
                StatusCode = statusCode,
                FlowCode = 1,

                OriginClientCode = viewModel.OriginClientId,
                OriginPointCode = viewModel.OriginCode,
                OriginIndicatorType = viewModel.OriginType.ToString().Substring(0, 1), // "P" o "F" de LocationTypeEnum

                DestinationClientCode = viewModel.DestinationClientId,
                DestinationPointCode = viewModel.DestinationCode,
                DestinationIndicatorType = viewModel.DestinationType.ToString().Substring(0, 1),

                AcceptanceDate = DateOnly.FromDateTime(DateTime.Now),
                AcceptanceTime = TimeOnly.FromDateTime(DateTime.Now),
                ProgrammingDate = viewModel.ProgrammingDate,
                ProgrammingTime = viewModel.ProgrammingTime,
                InitialAttentionDate = null,
                InitialAttentionTime = null,
                FinalAttentionDate = null,
                FinalAttentionTime = null,
                CancellationDate = null,
                CancellationTime = null,
                RejectionDate = null,
                RejectionTime = null,

                IsFailed = false,
                FailedResponsible = null,
                CancellationPerson = null,
                CancellationOperator = null,
                CancellationReason = null,
                ServiceModality = viewModel.ServiceModality,
                Observations = viewModel.ServiceObservations,
                KeyValue = (statusCode == 0) ? new Random().Next(1000, 9999) : 0,
                CgsOperatorId = currentUserId,
                CgsBranchName = _context.AdmSucursales.FirstOrDefault(s => s.CodSucursal == viewModel.BranchId)?.NombreSucursal,
                OperatorIpAddress = currentIP,
                BillValue = viewModel.DeclaredBillValue,
                CoinValue = viewModel.DeclaredCoinValue,
                ServiceValue = viewModel.ServiceValue,
                NumberOfChangeKits = viewModel.ExchangeKitCount,
                NumberOfCoinBags = viewModel.DeclaredBagCount,
                DetailFile = null,
            };

            await _context.CgsServicios.AddAsync(cgsService);

            var cefTransaction = new CefTransaction
            {
                ServiceOrderId = newServiceOrderId,
                RouteId = null,
                TransactionType = viewModel.ServiceConceptCode,
                SlipNumber = 0,
                CountingTableNumber = null,
                DeclaredBagCount = viewModel.DeclaredBagCount,
                DeclaredEnvelopeCount = viewModel.DeclaredEnvelopeCount,
                DeclaredCheckCount = viewModel.DeclaredCheckCount,
                DeclaredDocumentCount = viewModel.DeclaredDocumentCount,
                DeclaredBillValue = viewModel.DeclaredBillValue,
                DeclaredCoinValue = viewModel.DeclaredCoinValue,
                DeclaredDocumentValue = viewModel.DeclaredDocumentValue,
                TotalDeclaredValue = viewModel.TotalDeclaredValue,
                TotalDeclaredValueInWords = "Pendiente de implementar",
                IsCustody = viewModel.IsCustody,
                IsPointToPoint = viewModel.IsPointToPoint,
                InformativeIncident = viewModel.InformativeIncident,
                TransactionStatus = CefTransactionStatusEnum.Checkin.ToString(),
                RegistrationDate = DateTime.Now,
                RegistrationUser = currentUserId,
                RegistrationIP = currentIP,
                TotalCountedValue = 0,
                ValueDifference = 0,
            };

            await _context.CefTransactions.AddAsync(cefTransaction);

            await _context.SaveChangesAsync();

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
    }
}