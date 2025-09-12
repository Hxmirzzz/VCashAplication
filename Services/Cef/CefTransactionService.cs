using DocumentFormat.OpenXml.Math;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.CentroEfectivo;
using VCashApp.Utils;

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
        public async Task<CefTransactionCheckinViewModel> PrepareCheckinViewModelAsync(string serviceOrderId, string currentUserId, string currentIP)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUserId);
            string userName = user?.NombreUsuario ?? "Desconocido";

            var vm = new CefTransactionCheckinViewModel
            {
                ServiceOrderId = serviceOrderId,
                RegistrationDate = DateTime.Now,
                RegistrationUserName = userName,
                IPAddress = currentIP,

                Currencies = await GetCurrenciesForDropdownAsync(),
            };

            // === Traer servicio (datos de cabecera) ===
            var s = await _context.CgsServicios
                .AsNoTracking()
                .Where(x => x.ServiceOrderId == serviceOrderId)
                .Select(x => new
                {
                    x.ServiceOrderId,
                    x.BranchCode,
                    x.ConceptCode,
                    x.ClientCode,
                    x.OriginClientCode,
                    x.OriginPointCode,
                    x.OriginIndicatorType,
                    x.DestinationClientCode,
                    x.DestinationPointCode,
                    x.DestinationIndicatorType,
                    x.RequestDate,
                    x.RequestTime,
                    x.NumberOfCoinBags,
                    x.BillValue,
                    x.CoinValue
                })
                .FirstOrDefaultAsync();

            if (s == null)
                throw new InvalidOperationException($"No existe el servicio '{serviceOrderId}'.");

            // Sucursal
            vm.BranchName = await _context.AdmSucursales.AsNoTracking()
                .Where(b => b.CodSucursal == s.BranchCode)
                .Select(b => b.NombreSucursal)
                .FirstOrDefaultAsync() ?? "N/A";

            // Concepto
            var concept = await _context.AdmConceptos.AsNoTracking()
                .Where(c => c.CodConcepto == s.ConceptCode)
                .Select(c => new { c.NombreConcepto, c.TipoConcepto })
                .FirstOrDefaultAsync();

            // Cliente (prefiere origen si lo tienes, si no, usa CodCliente)
            var clientIdRef = s.OriginClientCode != 0 ? s.OriginClientCode : s.ClientCode;
            vm.ClientName = await _context.AdmClientes.AsNoTracking()
                .Where(c => c.ClientCode == clientIdRef)
                .Select(c => c.ClientName)
                .FirstOrDefaultAsync() ?? "N/A";

            // Origen/Destino (nombre según indicador P/F; si no existe, muestra el código)
            vm.OriginLocationName = s.OriginIndicatorType == "P"
                ? (await _context.AdmPuntos.AsNoTracking()
                    .Where(p => p.PointCode == s.OriginPointCode)
                    .Select(p => p.PointName)
                    .FirstOrDefaultAsync() ?? $"{s.OriginIndicatorType}-{s.OriginPointCode}")
                : (await _context.AdmFondos.AsNoTracking()
                    .Where(f => f.FundCode == s.OriginPointCode)
                    .Select(f => f.FundName)
                    .FirstOrDefaultAsync() ?? $"{s.OriginIndicatorType}-{s.OriginPointCode}");

            vm.DestinationLocationName = s.DestinationIndicatorType == "P"
                ? (await _context.AdmPuntos.AsNoTracking()
                    .Where(p => p.PointCode == s.DestinationPointCode)
                    .Select(p => p.PointName)
                    .FirstOrDefaultAsync() ?? $"{s.DestinationIndicatorType}-{s.DestinationPointCode}")
                : (await _context.AdmFondos.AsNoTracking()
                    .Where(f => f.FundCode == s.DestinationPointCode)
                    .Select(f => f.FundName)
                    .FirstOrDefaultAsync() ?? $"{s.DestinationIndicatorType}-{s.DestinationPointCode}");

            vm.HeadOfShiftName = "N/A"; // puedes poblarla si ya tienes relación de ruta ↔ JT

            var t = await _context.CefTransactions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ServiceOrderId == serviceOrderId);

            vm.TransactionType = t?.TransactionType
                               ?? concept?.TipoConcepto
                               ?? "N/A";

            // Declarados: usa lo existente en CEF; si no, cae a los valores del servicio
            var declaredBills = t?.DeclaredBillValue ?? (s.BillValue ?? 0m);
            var declaredCoins = t?.DeclaredCoinValue ?? (s.CoinValue ?? 0m);
            var declaredDocs = t?.DeclaredDocumentValue ?? 0m;

            vm.DeclaredBillValue = declaredBills;
            vm.DeclaredCoinValue = declaredCoins;
            vm.DeclaredDocumentValue = declaredDocs;

            vm.DeclaredBagCount = t?.DeclaredBagCount ?? (s.NumberOfCoinBags ?? 0);
            vm.DeclaredEnvelopeCount = t?.DeclaredEnvelopeCount ?? 0;
            vm.DeclaredCheckCount = t?.DeclaredCheckCount ?? 0;
            vm.DeclaredDocumentCount = t?.DeclaredDocumentCount ?? 0;

            vm.TotalDeclaredValue = declaredBills + declaredCoins + declaredDocs;

            // Planilla / Divisa / Tipo (si ya existía la transacción)
            vm.SlipNumber = t?.SlipNumber ?? 0;
            vm.Currency = t?.Currency;

            vm.IsCustody = t?.IsCustody ?? false;
            vm.IsPointToPoint = t?.IsPointToPoint ?? false;
            vm.InformativeIncident = t?.InformativeIncident;

            return vm;
        }

        public async Task<CefTransaction> ProcessCheckinViewModelAsync(CefTransactionCheckinViewModel viewModel, string currentUserId, string currentIP)
        {
            var service = await _context.CgsServicios.FirstOrDefaultAsync(s => s.ServiceOrderId == viewModel.ServiceOrderId);
            if (service == null)
                throw new InvalidOperationException($"La Orden de Servicio '{viewModel.ServiceOrderId}' no existe.");

            if (!viewModel.SlipNumber.HasValue || viewModel.SlipNumber.Value <= 0)
                throw new InvalidOperationException("El número de planilla debe ser mayor a 0.");

            if (!string.IsNullOrEmpty(viewModel.RouteId))
            {
                var route = await _context.TdvRutasDiarias
                    .FirstOrDefaultAsync(r => r.Id == viewModel.RouteId);
                if (route == null)
                    throw new InvalidOperationException($"La Ruta Diaria '{viewModel.RouteId}' no existe.");
            }

            var tx = await _context.CefTransactions
                .FirstOrDefaultAsync(t => t.ServiceOrderId == viewModel.ServiceOrderId);

            if (tx == null)
                throw new InvalidOperationException(
                    $"No existe una transacción CEF para la Orden de Servicio '{viewModel.ServiceOrderId}'. " +
                    $"Debe haberse generado al crear el servicio."
                );

            var bills = viewModel.DeclaredBillValue;
            var coins = viewModel.DeclaredCoinValue;
            var docs = viewModel.DeclaredDocumentValue;
            var totalDeclared = viewModel.TotalDeclaredValue;
            var totalDeclaredInWords = AmountInWordsHelper.ToSpanishCurrency(totalDeclared, viewModel.Currency ?? "COP");

            tx.RouteId = viewModel.RouteId;
            tx.SlipNumber = viewModel.SlipNumber.Value;
            tx.Currency = viewModel.Currency;
            tx.TransactionType = viewModel.TransactionType;
            tx.DeclaredBagCount = viewModel.DeclaredBagCount;
            tx.DeclaredEnvelopeCount = viewModel.DeclaredEnvelopeCount;
            tx.DeclaredCheckCount = viewModel.DeclaredCheckCount;
            tx.DeclaredDocumentCount = viewModel.DeclaredDocumentCount;
            tx.DeclaredBillValue = bills;
            tx.DeclaredCoinValue = coins;
            tx.DeclaredDocumentValue = docs;
            tx.TotalDeclaredValue = totalDeclared;
            tx.TotalDeclaredValueInWords = totalDeclaredInWords;
            tx.IsCustody = viewModel.IsCustody;
            tx.IsPointToPoint = viewModel.IsPointToPoint;
            tx.InformativeIncident = viewModel.InformativeIncident;
            tx.TransactionStatus = CefTransactionStatusEnum.EncoladoParaConteo.ToString();

            // Si tienes campos de auditoría de última actualización en tu entidad, setéalos aquí:
            // tx.LastUpdatedDate = DateTime.Now;
            // tx.LastUpdatedUser = currentUserId;
            // tx.LastUpdatedIP   = currentIP;

            await _context.SaveChangesAsync();
            return tx;
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
            string? search, int page, int pageSize, bool isAdmin, IEnumerable<string>? conceptTypeCodes = null)
        {
            List<int> permittedBranches = new();
            if (!isAdmin)
            {
                permittedBranches = await _context.UserClaims
                    .Where(uc => uc.UserId == currentUserId && uc.ClaimType == "SucursalId")
                    .Select(uc => int.Parse(uc.ClaimValue))
                    .ToListAsync();
            }

            DateTime? start = startDate?.ToDateTime(TimeOnly.MinValue);
            DateTime? end = endDate?.ToDateTime(TimeOnly.MaxValue);

            var txBase = _context.CefTransactions.AsNoTracking();

            var query =
                from t in txBase
                join s in _context.CgsServicios.AsNoTracking()
                    on t.ServiceOrderId equals s.ServiceOrderId into sj
                from s in sj.DefaultIfEmpty()
                join con in _context.AdmConceptos.AsNoTracking()
                    on s.ConceptCode equals con.CodConcepto into conj
                from con in conj.DefaultIfEmpty()
                join su in _context.AdmSucursales.AsNoTracking()
                    on t.BranchCode equals su.CodSucursal into suj
                from su in suj.DefaultIfEmpty()
                join rd in _context.TdvRutasDiarias.AsNoTracking()
                    on t.RouteId equals rd.Id into rdj
                from rd in rdj.DefaultIfEmpty()
                join jt in _context.AdmEmpleados.AsNoTracking()
                    on rd.CedulaJT equals jt.CodCedula into jtj
                from jt in jtj.DefaultIfEmpty()
                select new
                {
                    T = t,
                    BranchName = su != null ? su.NombreSucursal : "",
                    HeadOfShiftName = jt != null ? jt.NombreCompleto : "",
                    ConceptType = con != null ? con.TipoConcepto : null
                };

            // 5) Filtros
            if (!isAdmin && permittedBranches.Count > 0)
                query = query.Where(x => permittedBranches.Contains(x.T.BranchCode));

            if (branchId.HasValue)
                query = query.Where(x => x.T.BranchCode == branchId.Value);

            if (start.HasValue)
                query = query.Where(x => x.T.RegistrationDate >= start.Value);

            if (end.HasValue)
                query = query.Where(x => x.T.RegistrationDate <= end.Value);

            if (status.HasValue)
                query = query.Where(x => x.T.TransactionStatus == status.Value.ToString());

            if (conceptTypeCodes != null && conceptTypeCodes.Any())
            {
                var codes = conceptTypeCodes
                    .Select(c => c.Trim().ToUpper())
                    .ToArray();

                query = query.Where(x =>
                    codes.Contains(((x.ConceptType ?? "").Trim().ToUpper()))
                    || codes.Contains(((x.T.TransactionType ?? "").Trim().ToUpper()))
                );
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                bool isNumeric = int.TryParse(term, out var slipNo);

                query = query.Where(x =>
                    x.T.ServiceOrderId.Contains(term) ||
                    ((x.T.Currency ?? "").Contains(term)) ||
                    ((x.T.TransactionType ?? "").Contains(term)) ||
                    (isNumeric && x.T.SlipNumber == slipNo)
                );
            }

            int total = await query.CountAsync();

            var rows = await query
                .OrderByDescending(x => x.T.RegistrationDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new CefTransactionSummaryViewModel
                {
                    Id = x.T.Id,
                    ServiceOrderId = x.T.ServiceOrderId ?? string.Empty,
                    SlipNumber = x.T.SlipNumber,
                    Currency = x.T.Currency ?? string.Empty,
                    TransactionType = x.T.TransactionType ?? (x.ConceptType ?? string.Empty),
                    TotalDeclaredValue = x.T.TotalDeclaredValue,
                    TotalCountedValue = x.T.TotalCountedValue,
                    ValueDifference = x.T.ValueDifference,
                    TransactionStatus = x.T.TransactionStatus ?? string.Empty,
                    RegistrationDate = x.T.RegistrationDate,
                    BranchName = x.BranchName,
                    HeadOfShiftName = x.HeadOfShiftName
                })
                .ToListAsync();

            return Tuple.Create(rows, total);
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateTransactionStatusAsync(int transactionId, CefTransactionStatusEnum newStatus, string reviewerUserId)
        {
            var transaction = await _context.CefTransactions.FirstOrDefaultAsync(t => t.Id == transactionId);
            if (transaction == null)
            {
                return false;
            }

            if (transaction.TransactionStatus == CefTransactionStatusEnum.Aprobado.ToString() ||
                transaction.TransactionStatus == CefTransactionStatusEnum.Rechazado.ToString() ||
                transaction.TransactionStatus == CefTransactionStatusEnum.Cancelado.ToString())
            {
                throw new InvalidOperationException($"La transacción {transactionId} ya está en un estado final y no puede ser modificada.");
            }

            transaction.TransactionStatus = newStatus.ToString();
            transaction.LastUpdateDate = DateTime.Now;
            transaction.LastUpdateUser = reviewerUserId;

            if (newStatus == CefTransactionStatusEnum.Aprobado || newStatus == CefTransactionStatusEnum.Rechazado)
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
                                DeclaredValue = 0m,
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

            viewModel.AvailableStatuses = new List<SelectListItem>
            {
                new (CefTransactionStatusEnum.Aprobado.ToString(), "Aprobada"),
                new (CefTransactionStatusEnum.Rechazado.ToString(), "Rechazada")
            };

            return viewModel;
        }

        /// <inheritdoc/>
        public async Task<bool> ProcessReviewApprovalAsync(CefTransactionReviewViewModel viewModel, string reviewerUserId)
        {
            var transaction = await _context.CefTransactions.FirstOrDefaultAsync(t => t.Id == viewModel.Id);
            if (transaction == null) return false;

            if (transaction.TransactionStatus == CefTransactionStatusEnum.PendienteRevision.ToString())
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
                nameof(CefValueTypeEnum.Billete) => $"{vd.DenominationId?.ToString("N0")} x {vd.Quantity} ({vd.CalculatedAmount?.ToString("N0")})",
                nameof(CefValueTypeEnum.Moneda) => $"{vd.DenominationId?.ToString("N0")} x {vd.Quantity} ({vd.CalculatedAmount?.ToString("N0")})",
                _ => "Detalle Desconocido"
            };
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
    }
}