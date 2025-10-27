using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.CentroEfectivo.Shared;
using VCashApp.Utils;
using VCashApp.Services.Logging;
using VCashApp.Infrastructure.Branches;

namespace VCashApp.Services.Cef
{
    /// <summary>
    /// Implementación del servicio para la gestión de Transacciones de Centro de Efectivo.
    /// </summary>
    public class CefTransactionService : ICefTransactionService
    {
        private readonly AppDbContext _context;
        private readonly IBranchContext _branchContext;
        private readonly IAuditLogger _audit;
        private readonly ICefContainerService _cefContainerService;
        private readonly ICefIncidentService _cefIncidentService;

        public CefTransactionService(
            AppDbContext context,
            IBranchContext branchContext,
            IAuditLogger audit,
            ICefContainerService cefContainerService,
            ICefIncidentService cefIncidentService)
        {
            _context = context;
            _branchContext = branchContext;
            _audit = audit;
            _cefContainerService = cefContainerService;
            _cefIncidentService = cefIncidentService;
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
            CefTransitionPolicy.EnsureAllowedRecoleccion(tx.TransactionStatus, CefTransactionStatusEnum.EncoladoParaConteo, tx.Id);
            tx.TransactionStatus = CefTransactionStatusEnum.EncoladoParaConteo.ToString();

            // Si tienes campos de auditoría de última actualización en tu entidad, setéalos aquí:
            // tx.LastUpdatedDate = DateTime.Now;
            // tx.LastUpdatedUser = currentUserId;
            // tx.LastUpdatedIP   = currentIP;

            await _context.SaveChangesAsync();

            _audit.Info(
                action: "CEF.Checkin",
                detailMessage: $"Check-in OS {tx.ServiceOrderId}, Planilla {tx.SlipNumber}.",
                result: "EncoladoParaConteo",
                entityType: "CefTransaction",
                entityId: tx.Id.ToString(),
                urlId: tx.ServiceOrderId,
                modelId: tx.Id.ToString()
            );

            _audit.Info(
                action: "CGS.ServiceStatus",
                detailMessage: $"Servicio {service.ServiceOrderId} → Confirmado (1).",
                result: "OK",
                entityType: "CgsServicio",
                entityId: service.ServiceOrderId
            );

            if (service != null && service.StatusCode < 1)
            {
                service.StatusCode = 1; // Confirmado
                _context.CgsServicios.Update(service);
                await _context.SaveChangesAsync();
            }

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
            string? search, int page, int pageSize, bool isAdmin, IEnumerable<string>? conceptTypeCodes = null,
            IEnumerable<string>? excludeStatuses = null)
        {
            int? effectiveBranch = branchId ?? _branchContext.CurrentBranchId;

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

            if (!isAdmin && effectiveBranch.HasValue)
                query = query.Where(x => x.T.BranchCode == effectiveBranch.Value);

            if (effectiveBranch.HasValue && isAdmin)
                query = query.Where(x => x.T.BranchCode == effectiveBranch.Value);

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

            if (excludeStatuses != null && excludeStatuses.Any())
            {
                var ex = excludeStatuses.ToArray();
                query = query.Where(x => !ex.Contains(x.T.TransactionStatus));
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
                                            .Include(t => t.Service)
                                                .ThenInclude(s => s.Concept)
                                            .Include(t => t.Containers)
                                                .ThenInclude(c => c.ValueDetails)
                                                    .ThenInclude(vd => vd.AdmDenominacion)
                                            .Include(t => t.Containers)
                                                .ThenInclude(c => c.Incidents)
                                            .Include(t => t.Incidents)
                                            .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null) return null;

            var conceptCode = transaction.Service?.Concept?.TipoConcepto;
            var conceptName = transaction.Service?.Concept?.NombreConcepto;
            var service = await _context.CgsServicios.FirstOrDefaultAsync(s => s.ServiceOrderId == transaction.ServiceOrderId);
            var userRegistro = await _context.Users.FirstOrDefaultAsync(u => u.Id == transaction.RegistrationUser);
            var userRevisor = await _context.Users.FirstOrDefaultAsync(u => u.Id == transaction.ReviewerUserId);

            var viewModel = new CefTransactionReviewViewModel
            {
                Id = transaction.Id,
                ServiceOrderId = transaction.ServiceOrderId,
                SlipNumber = transaction.SlipNumber,
                TransactionTypeCode = conceptCode ?? transaction.TransactionType,
                TransactionTypeName = conceptName ?? conceptCode ?? "N/A",
                Currency = transaction.Currency ?? "N/A",
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
                            IncidentType = IncidentTypeCodeMap.TryFromCode(
                                _context.CefIncidentTypes.FirstOrDefault(it => it.Id == ni.IncidentTypeId)?.Code,
                                out var cat
                            ) ? cat : CefIncidentTypeCategoryEnum.Sobrante,
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
                                    IncidentType = IncidentTypeCodeMap.TryFromCode(
                                        _context.CefIncidentTypes.FirstOrDefault(it => it.Id == ni.IncidentTypeId)?.Code,
                                        out var cat
                                    ) ? cat : CefIncidentTypeCategoryEnum.Sobrante,
                                    Description = ni.Description,
                                    AffectedAmount = ni.AffectedAmount,
                                    ReportingUserName = _context.Users.FirstOrDefault(u => u.Id == ni.ReportedUserId)?.NombreUsuario ?? "N/A"
                                }).ToList(),
                            }).ToList()
                    }).ToList(),
                IncidentSummaries = transaction.Incidents.Select(ni => new CefIncidentSummaryViewModel
                {
                    Id = ni.Id,
                    IncidentType = IncidentTypeCodeMap.TryFromCode(
                        _context.CefIncidentTypes.FirstOrDefault(it => it.Id == ni.IncidentTypeId)?.Code,
                        out var cat
                    ) ? cat : CefIncidentTypeCategoryEnum.Sobrante,
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="viewModel"></param>
        /// <param name="reviewerUserId"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<bool> ProcessReviewApprovalAsync(CefTransactionReviewViewModel viewModel, string reviewerUserId)
        {
            var transaction = await _context.CefTransactions.FirstOrDefaultAsync(t => t.Id == viewModel.Id);
            if (transaction == null) return false;

            if (transaction.TransactionStatus != CefTransactionStatusEnum.PendienteRevision.ToString())
                throw new InvalidOperationException($"La transacción {transaction.Id} no está en estado 'Pendiente de Revisión' para ser aprobada o rechazada.");

            var typeCode = (viewModel.TransactionTypeCode ?? transaction.TransactionType)?.Trim().ToUpperInvariant();
            var isProvision = typeCode == "PV" || typeCode == "PR";

            var nextStatus = viewModel.NewStatus;
            if (isProvision && viewModel.NewStatus == CefTransactionStatusEnum.Aprobado)
                nextStatus = CefTransactionStatusEnum.ListoParaEntrega;

            if (isProvision)
                CefTransitionPolicy.EnsureAllowedProvision(transaction.TransactionStatus, nextStatus, transaction.Id);
            else
                CefTransitionPolicy.EnsureAllowedRecoleccion(transaction.TransactionStatus, nextStatus, transaction.Id);

            transaction.TransactionStatus = nextStatus.ToString();
            transaction.InformativeIncident = viewModel.FinalObservations;
            transaction.ReviewerUserId = reviewerUserId;
            transaction.CountingEndDate = DateTime.Now;

            _context.CefTransactions.Update(transaction);
            await _context.SaveChangesAsync();

            _audit.Info(
                action: "CEF.Review",
                detailMessage: $"Transacción {transaction.Id} revisada → {nextStatus}.",
                result: nextStatus.ToString(),
                entityType: "CefTransaction",
                entityId: transaction.Id.ToString(),
                urlId: transaction.ServiceOrderId
            );

            var service = await _context.CgsServicios.FirstOrDefaultAsync(s => s.ServiceOrderId == transaction.ServiceOrderId);
            if (service != null)
            {
                if (isProvision)
                {
                    switch (nextStatus)
                    {
                        case CefTransactionStatusEnum.ListoParaEntrega: service.StatusCode = 4; break;
                        case CefTransactionStatusEnum.Rechazado: service.StatusCode = 2; break;
                        case CefTransactionStatusEnum.Cancelado: service.StatusCode = 6; break;
                    }
                }
                else
                {
                    switch (nextStatus)
                    {
                        case CefTransactionStatusEnum.Aprobado: service.StatusCode = 5; break;
                        case CefTransactionStatusEnum.Rechazado: service.StatusCode = 2; break;
                        case CefTransactionStatusEnum.Cancelado: service.StatusCode = 6; break;
                    }
                }

                _context.CgsServicios.Update(service);
                await _context.SaveChangesAsync();

                _audit.Info(
                    action: "CGS.ServiceStatus",
                    detailMessage: $"Servicio {service.ServiceOrderId} → {service.StatusCode}.",
                    result: "OK",
                    entityType: "CgsServicio",
                    entityId: service.ServiceOrderId
                );
            }

            return true;
        }

        /// <summary>
        /// Helper para obtener una descripción legible de un detalle de valor.
        /// </summary>
        private string GetValueDetailDescription(CefValueDetail vd)
        {
            var etiqueta = vd.AdmDenominacion?.TipoDenominacion
                           ?? (vd.DenominationId.HasValue ? vd.DenominationId.Value.ToString("N0") : "Sin denom.");

            var cantidad = vd.Quantity;
            var monto = (vd.CalculatedAmount ?? 0m).ToString("N0");

            return $"{etiqueta} x {cantidad} ({monto})";
        }

        public async Task<bool> RecalcTotalsAndNetDiffAsync(int cefTransactionId)
        {
            var tx = await _context.CefTransactions.FirstOrDefaultAsync(t => t.Id == cefTransactionId);
            if (tx == null) return false;

            // 1) TotalContado = suma de detalles guardados (usa lo que ya tienes)
            var counted = await _cefContainerService.SumCountedValueAsync(cefTransactionId);
            tx.TotalCountedValue = counted;

            // 2) Efecto aprobado de novedades
            var effect = await _cefIncidentService.SumApprovedEffectByTransactionAsync(cefTransactionId);

            // 3) Diferencia neta
            tx.ValueDifference = (tx.TotalCountedValue - tx.TotalDeclaredValue) + effect;

            _context.CefTransactions.Update(tx);
            await _context.SaveChangesAsync();
            return true;
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
        /// Prepara un modelo detallado de transacción para revisión.
        /// </summary>
        /// <param name="transactionId">Identificador de la transacción.</param>
        /// <returns>Modelo detallado o null si no se encuentra.</returns>
        public async Task<CefTransactionDetailViewModel?> PrepareDetailViewModelAsync(int transactionId)
        {
            var tx = await _context.CefTransactions
                .AsNoTracking()
                .AsSplitQuery()
                .Include(t => t.Service)
                    .ThenInclude(s => s.Concept)
                .Include(t => t.Branch)
                .Include(t => t.Containers)
                    .ThenInclude(c => c.ValueDetails)
                        .ThenInclude(vd => vd.AdmDenominacion)
                .Include(t => t.Containers)
                    .ThenInclude(c => c.ValueDetails)
                        .ThenInclude(vd => vd.AdmQuality)
                .Include(t => t.Containers)
                    .ThenInclude(c => c.Incidents)
                .Include(t => t.Incidents)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (tx == null) return null;

            var userId = new HashSet<string>();
            if (!string.IsNullOrEmpty(tx.RegistrationUser))userId.Add(tx.RegistrationUser);
            foreach (var inc in tx.Incidents)
            {
                if (!string.IsNullOrEmpty(inc.ReportedUserId)) userId.Add(inc.ReportedUserId);
            }

            var user = await _context.Users
                .AsNoTracking()
                .Where(u => userId.Contains(u.Id))
                .Select(u => new { u.Id, u.NombreUsuario })
                .ToDictionaryAsync(u => u.Id, u => u.NombreUsuario);

            var typeId = tx.Incidents.Select(i => i.IncidentTypeId).Where(id => id != null).Distinct().ToList();
            var incTypes = await _context.CefIncidentTypes
                .AsNoTracking()
                .Where(it => typeId.Contains(it.Id))
                .Select(it => new { it.Id, it.Code })
                .ToDictionaryAsync(it => it.Id, it => it.Code);

            decimal billsHigh = 0, billsLow = 0, coins = 0, docs = 0, checks = 0;

            var byId = tx.Containers.ToDictionary(c => c.Id, c => c);
            string ParentLabel(int? pid)
            {
                if (pid == null || !byId.ContainsKey(pid.Value)) return "";
                var onlyBags = tx.Containers
                                 .Where(c => c.ContainerType == CefContainerTypeEnum.Bolsa.ToString())
                                 .OrderBy(c => c.Id)
                                 .ToList();
                var idx = onlyBags.FindIndex(c => c.Id == pid.Value);
                var code = byId[pid.Value].ContainerCode ?? "";
                return $"Bolsa {(idx >= 0 ? idx + 1 : 0)} — {code}";
            }

            var vm = new CefTransactionDetailViewModel
            {
                CefTransactionId = tx.Id,
                SlipNumber = tx.SlipNumber.ToString() ?? "N/A",
                CurrentStatus = tx.TransactionStatus ?? "N/A",
                ServiceOrderId = tx.ServiceOrderId ?? "N/A",
                ServiceConcept = tx.Service?.Concept?.NombreConcepto ?? "N/A",
                BranchName = tx.Branch?.NombreSucursal ?? "N/A",
                RegistrationDate = tx.RegistrationDate,
                RegisteredByName = (tx.RegistrationUser != null && user.TryGetValue(tx.RegistrationUser, out var uName)) ? uName : null
            };

            foreach (var c in tx.Containers.OrderBy(x => x.ParentContainerId.HasValue).ThenBy(x => x.Id))
            {
                var cvm = new DetailContainerVM
                {
                    Id = c.Id,
                    ContainerType = c.ContainerType ?? "N/A",
                    ContainerCode = c.ContainerCode ?? "N/A",
                    ParentContainerId = c.ParentContainerId,
                    ParentLabel = ParentLabel(c.ParentContainerId)
                };

                if (c.ValueDetails != null)
                {
                    foreach (var v in c.ValueDetails.OrderBy(v => v.ValueType).ThenBy(v => v.DenominationId))
                    {
                        string denomLabel =
                            v.AdmDenominacion?.Denominacion
                            ?? (v.AdmDenominacion?.ValorDenominacion > 0
                                ? v.AdmDenominacion!.ValorDenominacion?.ToString("N0")
                                : (v.UnitValue ?? 0m).ToString("N0"));

                        string qualityName = v.AdmQuality?.QualityName ?? "N/A";

                        bool? isHigh = null;
                        if (string.Equals(v.ValueType, "Billete", StringComparison.OrdinalIgnoreCase))
                            isHigh = v.IsHighDenomination;

                        int bundleSize = v.AdmDenominacion?.CantidadUnidadAgrupamiento ?? 1;

                        int bundles = v.BundlesCount ?? 0;
                        int loose = v.LoosePiecesCount ?? 0;

                        int quantity = v.Quantity ?? (bundles * bundleSize + loose);

                        decimal unitValue = v.UnitValue
                                            ?? v.AdmDenominacion?.ValorDenominacion
                                            ?? 0m;

                        decimal amount = v.CalculatedAmount ?? (quantity * unitValue);


                        var row = new DetailValueRowVM
                        {
                            ValueType = v.ValueType ?? "",
                            DenominationName = denomLabel,
                            QualityName = qualityName,
                            IsHighDenomination = isHigh,
                            Quantity = quantity,
                            Bundles = bundles,
                            Loose = loose,
                            UnitValue = unitValue,
                            CalculatedAmount = amount
                        };
                        cvm.Values.Add(row);

                        // Totales por tipo
                        if (row.ValueType.Equals("Billete", StringComparison.OrdinalIgnoreCase))
                        {
                            if (row.IsHighDenomination == true) billsHigh += amount;
                            else billsLow += amount;
                        }
                        else if (row.ValueType.Equals("Moneda", StringComparison.OrdinalIgnoreCase))
                        {
                            coins += amount;
                        }
                        else if (row.ValueType.Equals("Documento", StringComparison.OrdinalIgnoreCase))
                        {
                            docs += amount;
                        }
                        else if (row.ValueType.Equals("Cheque", StringComparison.OrdinalIgnoreCase))
                        {
                            checks += amount;
                        }

                        cvm.Subtotal += amount;
                    }
                }

                vm.Containers.Add(cvm);
            }

            foreach (var inc in tx.Incidents.OrderBy(i => i.IncidentDate))
            {
                decimal amount = inc.AffectedAmount;
                if (amount == 0m && inc.AffectedDenomination.HasValue && inc.AffectedQuantity.HasValue)
                {
                    var face = await _context.AdmDenominaciones
                        .AsNoTracking()
                        .Where(d => d.CodDenominacion == inc.AffectedDenomination.Value)
                        .Select(d => (decimal?)d.ValorDenominacion)
                        .FirstOrDefaultAsync() ?? 0m;
                    amount = face * inc.AffectedQuantity.Value;
                }

                vm.Incidents.Add(new DetailIncidentVM
                {
                    Id = inc.Id,
                    Code = (inc.IncidentTypeId != null && incTypes.TryGetValue(inc.IncidentTypeId, out var code)) ? code : "",
                    Description = inc.Description,
                    Status = inc.IncidentStatus ?? "",
                    ReportedBy = (inc.ReportedUserId != null && user.TryGetValue(inc.ReportedUserId, out var rname)) ? rname : null,
                    ReportedAt = inc.IncidentDate,
                    AffectedAmount = amount,
                    AffectedDenomination = inc.AffectedDenomination,
                    AffectedQuantity = inc.AffectedQuantity
                });
            }

            var computed = billsHigh + billsLow + coins + docs + checks;
            vm.TotalDeclared = tx.TotalDeclaredValue;
            vm.TotalCounted = (tx.TotalCountedValue != 0) ? tx.TotalCountedValue : computed;
            vm.NetDiff = vm.TotalCounted - vm.TotalDeclared;

            vm.BillsHigh = billsHigh;
            vm.BillsLow = billsLow;
            vm.CoinsTotal = coins;
            vm.DocsTotal = docs;
            vm.ChecksTotal = checks;

            return vm;
        }
    }
}