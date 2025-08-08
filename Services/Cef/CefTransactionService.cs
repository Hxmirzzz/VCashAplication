using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.CentroEfectivo;
using VCashApp.Services;

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
        public async Task<(List<SelectListItem> Sucursales, List<SelectListItem> Estados)> GetDropdownListsAsync(string currentUserId, bool isAdmin)
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

            var statuses = Enum.GetValues(typeof(CefTransactionStatusEnum))
                               .Cast<CefTransactionStatusEnum>()
                               .Select(e => new SelectListItem { Value = e.ToString(), Text = e.ToString().Replace("_", " ") })
                               .ToList();
            statuses.Insert(0, new SelectListItem { Value = "", Text = "-- Seleccionar Estado --" });

            return (permittedBranchesList, statuses);
        }

        /// <inheritdoc/>
        public async Task<CefTransactionCheckinViewModel> PrepareCheckinViewModelAsync(string serviceOrderId, string? routeId, string currentUserId, string currentIP)
        {
            var service = await _context.CgsServicios
                                        .Include(s => s.Client)
                                        .Include(s => s.Branch)
                                        .FirstOrDefaultAsync(s => s.ServiceOrderId == serviceOrderId);

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

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
            string userName = user?.NombreUsuario ?? "Desconocido";

            string clientName = "N/A";
            if (service.ClientCode != 0)
            {
                var clientEntity = await _context.AdmClientes.FirstOrDefaultAsync(c => c.ClientCode == service.ClientCode);
                clientName = clientEntity?.ClientName ?? "N/A";
            }
            else if (service.OriginClientCode != 0)
            {
                clientName = service.OriginClient.ClientName;
            }

            var viewModel = new CefTransactionCheckinViewModel
            {
                ServiceOrderId = service.ServiceOrderId,
                RouteId = route?.Id,
                SlipNumber = 0,
                Currency = "COP", 
                TransactionType = Enum.TryParse<CefTransactionTypeEnum>(service.Concept.TipoConcepto, out var type) ? type : CefTransactionTypeEnum.Collection,
                DeclaredBagCount = service.NumberOfCoinBags ?? 0,
                DeclaredEnvelopeCount = 0,
                DeclaredCheckCount = 0,
                DeclaredDocumentCount = 0,
                DeclaredBillValue = service.BillValue ?? 0,
                DeclaredCoinValue = service.CoinValue ?? 0,
                DeclaredDocumentValue = 0,
                TotalDeclaredValue = (service.BillValue ?? 0) + (service.CoinValue ?? 0) + (service.ServiceValue ?? 0),

                IsCustody = false,
                IsPointToPoint = false,
                InformativeIncident = service.Observations,

                RegistrationDate = DateTime.Now,
                RegistrationUserName = userName,
                IPAddress = currentIP,

                ClientName = clientName,
                BranchName = service.Branch?.NombreSucursal ?? "N/A",
                OriginLocationName = service.OriginPointCode ?? "N/A",
                DestinationLocationName = service.DestinationPointCode ?? "N/A",
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
            var existingService = await _context.CgsServicios.FirstOrDefaultAsync(s => s.ServiceOrderId == viewModel.ServiceOrderId);
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
        public async Task<Tuple<List<CefTransactionSummaryViewModel>, int>> GetFilteredCefTransactionsAsync(
            string currentUserId, int? branchId, DateOnly? startDate, DateOnly? endDate, CefTransactionStatusEnum? status,
            string? search, int page, int pageSize, bool isAdmin)
        {
            var permittedBranches = new List<int>();
            if (!isAdmin)
            {
                permittedBranches = await _context.UserClaims
                    .Where(uc => uc.UserId == currentUserId && uc.ClaimType == "SucursalId")
                    .Select(uc => int.Parse(uc.ClaimValue))
                    .ToListAsync();
            }

            var tvpTable = new DataTable();
            tvpTable.Columns.Add("Value", typeof(int));
            foreach (int id in permittedBranches)
            {
                tvpTable.Rows.Add(id);
            }

            var pPermittedBranchIds = new SqlParameter("@PermittedBranchIds", tvpTable)
            {
                TypeName = "dbo.IntListType",
                SqlDbType = SqlDbType.Structured
            };

            var pBranchIdFilter = new SqlParameter("@BranchIdFilter", branchId ?? (object)DBNull.Value);
            var pStartDate = new SqlParameter("@StartDate", startDate.HasValue ? (object)startDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
            var pEndDate = new SqlParameter("@EndDate", endDate.HasValue ? (object)endDate.Value.ToDateTime(TimeOnly.MaxValue) : DBNull.Value);
            var pStatus = new SqlParameter("@Status", status.HasValue ? (object)status.Value.ToString() : DBNull.Value);
            var pSearchTerm = new SqlParameter("@SearchTerm", string.IsNullOrEmpty(search) ? (object)DBNull.Value : search);
            var pPage = new SqlParameter("@Page", page);
            var pPageSize = new SqlParameter("@PageSize", pageSize);

            var transactionsSummary = new List<CefTransactionSummaryViewModel>();
            var totalRecords = 0;

            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "dbo.GetFilteredCefTransactions";
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddRange(new[] {
            pPermittedBranchIds, pBranchIdFilter, pStartDate, pEndDate, pStatus,
            pSearchTerm, pPage, pPageSize
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
                        transactionsSummary.Add(new CefTransactionSummaryViewModel
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            ServiceOrderId = reader.IsDBNull(reader.GetOrdinal("OrdenServicio")) ? string.Empty : reader.GetString(reader.GetOrdinal("OrdenServicio")),
                            SlipNumber = reader.GetInt32(reader.GetOrdinal("NumeroPlanilla")),
                            Currency = reader.IsDBNull(reader.GetOrdinal("Divisa")) ? string.Empty : reader.GetString(reader.GetOrdinal("Divisa")),
                            TransactionType = reader.IsDBNull(reader.GetOrdinal("TipoTransaccion")) ? string.Empty : reader.GetString(reader.GetOrdinal("TipoTransaccion")),
                            TotalDeclaredValue = reader.GetDecimal(reader.GetOrdinal("ValorTotalDeclarado")),
                            TotalCountedValue = reader.GetDecimal(reader.GetOrdinal("ValorTotalContado")),
                            ValueDifference = reader.GetDecimal(reader.GetOrdinal("DiferenciaValor")),
                            TransactionStatus = reader.IsDBNull(reader.GetOrdinal("EstadoTransaccion"))
                                ? string.Empty
                                : reader.GetString(reader.GetOrdinal("EstadoTransaccion")),
                            RegistrationDate = reader.GetDateTime(reader.GetOrdinal("FechaRegistro")),
                            BranchName = reader.IsDBNull(reader.GetOrdinal("BranchName"))
                                ? string.Empty
                                : reader.GetString(reader.GetOrdinal("BranchName")),
                            HeadOfShiftName = reader.IsDBNull(reader.GetOrdinal("HeadOfShiftName"))
                                ? string.Empty
                                : reader.GetString(reader.GetOrdinal("HeadOfShiftName"))
                        });
                    }
                }
            }

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

            var service = await _context.CgsServicios.FirstOrDefaultAsync(s => s.ServiceOrderId == transaction.ServiceOrderId);
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