using VCashApp.Services;
using VCashApp.Data;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.CentroEfectivo;
using VCashApp.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace VCashApp.Services.Cef
{
    /// <summary>
    /// Implementación del servicio para la gestión de Transacciones de Centro de Efectivo.
    /// </summary>
    public class CefTransactionService : ICefTransactionService
    {
        private readonly AppDbContext _context;

        public CefTransactionService(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<CefTransactionCheckinViewModel> PrepareCheckinViewModelAsync(string serviceOrderId, string? routeId, string currentUserId, string currentIP)
        {
            // Cargar servicio y sus propiedades de navegación necesarias
            var service = await _context.AdmServicios
                                        //.Include(s => s.Cliente)
                                        .Include(s => s.Sucursal)
                                        .FirstOrDefaultAsync(s => s.OrdenServicio == serviceOrderId);

            if (service == null)
            {
                throw new InvalidOperationException($"La Orden de Servicio '{serviceOrderId}' no se encontró.");
            }

            var existingCefTransaction = await _context.CefTransactions.AnyAsync(t => t.ServiceOrderId == serviceOrderId);
            if (existingCefTransaction)
            {
                throw new InvalidOperationException($"Ya existe una transacción de Centro de Efectivo para la Orden de Servicio '{serviceOrderId}'.");
            }

            TdvRutaDiaria? route = null;
            if (!string.IsNullOrEmpty(routeId))
            {
                route = await _context.TdvRutasDiarias
                                        .Include(r => r.JT)
                                        .FirstOrDefaultAsync(r => r.Id == routeId);
            }

            // Cargar el nombre del usuario actual
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
            string userName = user?.NombreUsuario ?? "Desconocido";

            string clientName = "N/A";
            if (service.CodCliente.HasValue)
            {
                var clientEntity = await _context.AdmClientes.FirstOrDefaultAsync(c => c.CodigoCliente == service.CodCliente.Value);
                clientName = clientEntity?.NombreCliente ?? "N/A";
            }
            else if (!string.IsNullOrEmpty(service.ClienteOrigen))
            {
                clientName = service.ClienteOrigen;
            }

            var viewModel = new CefTransactionCheckinViewModel
            {
                ServiceOrderId = service.OrdenServicio,
                RouteId = route?.Id,
                SlipNumber = 0,
                Currency = "COP", 
                TransactionType = Enum.TryParse<CefTransactionTypeEnum>(service.TipoConcepto, out var type) ? type : CefTransactionTypeEnum.Collection,
                DeclaredBagCount = service.NumeroBolsasMoneda ?? 0,
                DeclaredEnvelopeCount = 0,
                DeclaredCheckCount = 0,
                DeclaredDocumentCount = 0,
                DeclaredBillValue = service.ValorBillete ?? 0,
                DeclaredCoinValue = service.ValorMoneda ?? 0,
                DeclaredDocumentValue = 0,
                TotalDeclaredValue = (service.ValorBillete ?? 0) + (service.ValorMoneda ?? 0) + (service.ValorServicio ?? 0),

                IsCustody = false,
                IsPointToPoint = false,
                InformativeIncident = service.Observaciones,

                RegistrationDate = DateTime.Now,
                RegistrationUserName = userName,
                IPAddress = currentIP,

                ClientName = clientName,
                BranchName = service.Sucursal?.NombreSucursal ?? "N/A", // Acceso a través de la propiedad de navegación
                OriginLocationName = service.PuntoOrigen ?? "N/A",
                DestinationLocationName = service.PuntoDestino ?? "N/A",
                HeadOfShiftName = route?.JT?.NombreCompleto ?? "N/A",
                VehicleCode = route?.CodVehiculo ?? "N/A"
            };

            // Rellenar SelectLists para el frontend
            viewModel.Currencies = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
            {
                new ("COP", "COP"),
                new ("USD", "USD")
            };
            viewModel.TransactionTypes = Enum.GetValues(typeof(CefTransactionTypeEnum))
                                             .Cast<CefTransactionTypeEnum>()
                                             .Select(e => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                                             {
                                                 Value = e.ToString(),
                                                 Text = e.ToString()
                                             }).ToList();

            return viewModel;
        }

        /// <inheritdoc/>
        public async Task<CefTransaction> ProcessCheckinViewModelAsync(CefTransactionCheckinViewModel viewModel, string currentUserId, string currentIP)
        {
            var existingService = await _context.AdmServicios.FirstOrDefaultAsync(s => s.OrdenServicio == viewModel.ServiceOrderId);
            if (existingService == null)
            {
                throw new InvalidOperationException($"La Orden de Servicio '{viewModel.ServiceOrderId}' no existe.");
            }
            var existingCefTransaction = await _context.CefTransactions.AnyAsync(t => t.ServiceOrderId == viewModel.ServiceOrderId);
            if (existingCefTransaction)
            {
                throw new InvalidOperationException($"Ya existe una transacción de Centro de Efectivo para la Orden de Servicio '{viewModel.ServiceOrderId}'.");
            }

            if (!string.IsNullOrEmpty(viewModel.RouteId))
            {
                var existingRoute = await _context.TdvRutasDiarias.FirstOrDefaultAsync(r => r.Id == viewModel.RouteId);
                if (existingRoute == null)
                {
                    throw new InvalidOperationException($"La Ruta Diaria '{viewModel.RouteId}' no existe.");
                }
            }

            var transaction = new CefTransaction
            {
                ServiceOrderId = viewModel.ServiceOrderId,
                RouteId = viewModel.RouteId,
                Currency = viewModel.Currency,
                TransactionType = viewModel.TransactionType.ToString(),
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
                ValueDifference = 0
            };

            await _context.CefTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        /// <inheritdoc/>
        public async Task<CefTransaction?> GetCefTransactionByIdAsync(int transactionId)
        {
            return await _context.CefTransactions
                                 .Include(t => t.Containers)
                                     .ThenInclude(c => c.ValueDetails)
                                 .Include(t => t.Containers)
                                     .ThenInclude(c => c.Incidents)
                                 .Include(t => t.Incidents)
                                 .FirstOrDefaultAsync(t => t.Id == transactionId);
        }

        /// <inheritdoc/>
        // El método GetFilteredCefTransactionsAsync ahora devuelve una tupla con CefTransactionSummaryViewModel
        public async Task<Tuple<List<CefTransactionSummaryViewModel>, int>> GetFilteredCefTransactionsAsync(
            int? branchId, DateOnly? startDate, DateOnly? endDate, CefTransactionStatusEnum? status,
            string? searchTerm, int pageNumber, int pageSize)
        {
            IQueryable<CefTransaction> query = _context.CefTransactions
                                                    .AsQueryable();

            if (branchId.HasValue)
            {
                query = query.Join(_context.AdmServicios,
                                   cefTrans => cefTrans.ServiceOrderId,
                                   admServ => admServ.OrdenServicio,
                                   (cefTrans, admServ) => new { CefTrans = cefTrans, AdmServ = admServ })
                             .Where(joined => joined.AdmServ.CodSucursal == branchId.Value)
                             .Select(joined => joined.CefTrans);
            }

            if (startDate.HasValue)
            {
                query = query.Where(t => DateOnly.FromDateTime(t.RegistrationDate) >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(t => DateOnly.FromDateTime(t.RegistrationDate) <= endDate.Value);
            }
            if (status.HasValue)
            {
                query = query.Where(t => t.TransactionStatus == status.Value.ToString());
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                string lowerSearchTerm = searchTerm.ToLower();
                query = query.Where(t => t.ServiceOrderId.ToLower().Contains(lowerSearchTerm) ||
                                         t.SlipNumber.ToString().Contains(lowerSearchTerm) ||
                                         t.Currency.ToLower().Contains(lowerSearchTerm) ||
                                         t.TransactionStatus.ToLower().Contains(lowerSearchTerm));
            }

            int totalRecords = await query.CountAsync();

            var transactionsSummary = await query
                .OrderByDescending(t => t.RegistrationDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new CefTransactionSummaryViewModel
                {
                    Id = t.Id,
                    ServiceOrderId = t.ServiceOrderId,
                    SlipNumber = t.SlipNumber,
                    Currency = t.Currency,
                    TransactionType = t.TransactionType,
                    TotalDeclaredValue = t.TotalDeclaredValue,
                    TotalCountedValue = t.TotalCountedValue,
                    ValueDifference = t.ValueDifference,
                    TransactionStatus = t.TransactionStatus,
                    RegistrationDate = t.RegistrationDate,

                    BranchName = _context.AdmServicios.Where(s => s.OrdenServicio == t.ServiceOrderId)
                                                      .Select(s => s.Sucursal.NombreSucursal)
                                                      .FirstOrDefault() ?? "N/A",
                    HeadOfShiftName = _context.TdvRutasDiarias.Where(r => r.Id == t.RouteId)
                                                             .Select(r => r.JT.NombreCompleto)
                                                             .FirstOrDefault() ?? "N/A"
                })
                .ToListAsync();

            return Tuple.Create(transactionsSummary, totalRecords);
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateTransactionStatusAsync(int transactionId, CefTransactionStatusEnum newStatus, string reviewerUserId)
        {
            var transaction = await _context.CefTransactions.FirstOrDefaultAsync(t => t.Id == transactionId);
            if (transaction == null)
            {
                return false;
            }

            if (transaction.TransactionStatus == CefTransactionStatusEnum.Approved.ToString() ||
                transaction.TransactionStatus == CefTransactionStatusEnum.Rejected.ToString() ||
                transaction.TransactionStatus == CefTransactionStatusEnum.Cancelled.ToString())
            {
                throw new InvalidOperationException($"La transacción {transactionId} ya está en un estado final y no puede ser modificada.");
            }

            transaction.TransactionStatus = newStatus.ToString();
            transaction.LastUpdateDate = DateTime.Now;
            transaction.LastUpdateUser = reviewerUserId;

            if (newStatus == CefTransactionStatusEnum.Approved || newStatus == CefTransactionStatusEnum.Rejected)
            {
                transaction.CountingEndDate = DateTime.Now;
            }

            _context.CefTransactions.Update(transaction);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc/>
        public async Task<CefTransactionReviewViewModel?> PrepareReviewViewModelAsync(int transactionId)
        {
            var transaction = await _context.CefTransactions
                                            .Include(t => t.Containers)
                                                .ThenInclude(c => c.ValueDetails)
                                                    .ThenInclude(vd => vd.Incidents)
                                            .Include(t => t.Containers)
                                                .ThenInclude(c => c.Incidents)
                                            .Include(t => t.Incidents)
                                            .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null) return null;

            var service = await _context.AdmServicios.FirstOrDefaultAsync(s => s.OrdenServicio == transaction.ServiceOrderId);
            var userRegistro = await _context.Users.FirstOrDefaultAsync(u => u.Id == transaction.RegistrationUser);
            var userRevisor = await _context.Users.FirstOrDefaultAsync(u => u.Id == transaction.ReviewerUserId);

            var viewModel = new CefTransactionReviewViewModel
            {
                Id = transaction.Id,
                ServiceOrderId = transaction.ServiceOrderId,
                SlipNumber = transaction.SlipNumber,
                TransactionType = Enum.Parse<CefTransactionTypeEnum>(transaction.TransactionType),
                Currency = transaction.Currency,
                TotalDeclaredValue = transaction.TotalDeclaredValue,
                TotalCountedValue = transaction.TotalCountedValue,
                ValueDifference = transaction.ValueDifference,
                CurrentStatus = Enum.Parse<CefTransactionStatusEnum>(transaction.TransactionStatus),
                ReviewerUserName = userRevisor?.NombreUsuario ?? userRegistro?.NombreUsuario ?? "N/A",
                ReviewDate = DateTime.Now,
                FinalObservations = transaction.InformativeIncident,

                ContainerSummaries = transaction.Containers
                    .Where(c => c.ParentContainerId == null)
                    .Select(c => new CefContainerSummaryViewModel
                    {
                        Id = c.Id,
                        ContainerType = Enum.Parse<CefContainerTypeEnum>(c.ContainerType),
                        ContainerCode = c.ContainerCode,
                        DeclaredValue = c.DeclaredValue,
                        CountedValue = c.CountedValue ?? 0,
                        ContainerStatus = Enum.Parse<CefContainerStatusEnum>(c.ContainerStatus),
                        ProcessingUserName = _context.Users.FirstOrDefault(u => u.Id == c.ProcessingUserId)?.NombreUsuario ?? "N/A",
                        IncidentCount = c.Incidents.Count + c.ValueDetails.Sum(vd => vd.Incidents.Count),
                        ValueDetailSummaries = c.ValueDetails.Select(vd => new CefValueDetailSummaryViewModel
                        {
                            Id = vd.Id,
                            ValueType = Enum.Parse<CefValueTypeEnum>(vd.ValueType),
                            DetailDescription = GetValueDetailDescription(vd),
                            CalculatedAmount = vd.CalculatedAmount ?? 0,
                            IncidentCount = vd.Incidents.Count
                        }).ToList(),
                        IncidentList = c.Incidents.Select(ni => new CefIncidentSummaryViewModel
                        {
                            Id = ni.Id,
                            IncidentType = Enum.Parse<CefIncidentTypeCategoryEnum>(_context.CefIncidentTypes.FirstOrDefault(it => it.Id == ni.IncidentTypeId)?.Code ?? "Other"),
                            Description = ni.Description,
                            AffectedAmount = ni.AffectedAmount,
                            ReportingUserName = _context.Users.FirstOrDefault(u => u.Id == ni.ReportedUserId)?.NombreUsuario ?? "N/A"
                        }).ToList(),
                        ChildContainers = transaction.Containers
                            .Where(ch => ch.ParentContainerId == c.Id)
                            .Select(ch => new CefContainerSummaryViewModel
                            {
                                Id = ch.Id,
                                ContainerType = Enum.Parse<CefContainerTypeEnum>(ch.ContainerType),
                                ContainerCode = ch.ContainerCode,
                                DeclaredValue = ch.DeclaredValue,
                                CountedValue = ch.CountedValue ?? 0,
                                ContainerStatus = Enum.Parse<CefContainerStatusEnum>(ch.ContainerStatus),
                                ProcessingUserName = _context.Users.FirstOrDefault(u => u.Id == ch.ProcessingUserId)?.NombreUsuario ?? "N/A",
                                IncidentCount = ch.Incidents.Count + ch.ValueDetails.Sum(vd => vd.Incidents.Count),
                                ValueDetailSummaries = ch.ValueDetails.Select(vd => new CefValueDetailSummaryViewModel
                                {
                                    Id = vd.Id,
                                    ValueType = Enum.Parse<CefValueTypeEnum>(vd.ValueType),
                                    DetailDescription = GetValueDetailDescription(vd),
                                    CalculatedAmount = vd.CalculatedAmount ?? 0,
                                    IncidentCount = vd.Incidents.Count
                                }).ToList(),
                                IncidentList = ch.Incidents.Select(ni => new CefIncidentSummaryViewModel
                                {
                                    Id = ni.Id,
                                    IncidentType = Enum.Parse<CefIncidentTypeCategoryEnum>(_context.CefIncidentTypes.FirstOrDefault(it => it.Id == ni.IncidentTypeId)?.Code ?? "Other"),
                                    Description = ni.Description,
                                    AffectedAmount = ni.AffectedAmount,
                                    ReportingUserName = _context.Users.FirstOrDefault(u => u.Id == ni.ReportedUserId)?.NombreUsuario ?? "N/A"
                                }).ToList(),
                            }).ToList()
                    }).ToList(),
                IncidentSummaries = transaction.Incidents.Select(ni => new CefIncidentSummaryViewModel
                {
                    Id = ni.Id,
                    IncidentType = Enum.Parse<CefIncidentTypeCategoryEnum>(_context.CefIncidentTypes.FirstOrDefault(it => it.Id == ni.IncidentTypeId)?.Code ?? "Other"),
                    Description = ni.Description,
                    AffectedAmount = ni.AffectedAmount,
                    ReportingUserName = _context.Users.FirstOrDefault(u => u.Id == ni.ReportedUserId)?.NombreUsuario ?? "N/A"
                }).ToList()
            };

            viewModel.AvailableStatuses = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
            {
                new (CefTransactionStatusEnum.Approved.ToString(), "Aprobada"),
                new (CefTransactionStatusEnum.Rejected.ToString(), "Rechazada")
            };

            return viewModel;
        }

        /// <inheritdoc/>
        public async Task<bool> ProcessReviewApprovalAsync(CefTransactionReviewViewModel viewModel, string reviewerUserId)
        {
            var transaction = await _context.CefTransactions.FirstOrDefaultAsync(t => t.Id == viewModel.Id);
            if (transaction == null) return false;

            // Validar que el estado actual sea "PendienteRevision"
            if (transaction.TransactionStatus == CefTransactionStatusEnum.PendingReview.ToString())
            {
                throw new InvalidOperationException($"La transacción {transaction.Id} no está en estado 'Pendiente de Revisión' para ser aprobada o rechazada.");
            }

            transaction.TransactionStatus = viewModel.NewStatus.ToString();
            transaction.InformativeIncident = viewModel.FinalObservations;
            transaction.ReviewerUserId = reviewerUserId;
            transaction.CountingEndDate = DateTime.Now;

            _context.CefTransactions.Update(transaction);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Helper para obtener una descripción legible de un detalle de valor.
        /// </summary>
        private string GetValueDetailDescription(CefValueDetail vd)
        {
            return vd.ValueType switch
            {
                nameof(CefValueTypeEnum.Bill) => $"{vd.Denomination?.ToString("N0")} x {vd.Quantity} ({vd.CalculatedAmount?.ToString("N0")})",
                nameof(CefValueTypeEnum.Coin) => $"{vd.Denomination?.ToString("N0")} x {vd.Quantity} ({vd.CalculatedAmount?.ToString("N0")})",
                nameof(CefValueTypeEnum.Check) => $"Cheque #{vd.IdentifierNumber} ({vd.BankName}) por {vd.CalculatedAmount?.ToString("N0")}",
                nameof(CefValueTypeEnum.Document) => $"Doc #{vd.IdentifierNumber} por {vd.CalculatedAmount?.ToString("N0")}",
                _ => "Detalle Desconocido"
            };
        }
    }
}