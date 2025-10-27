using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Infrastructure.Branches;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.CentroEfectivo.Shared;
using VCashApp.Services.Cef;
using VCashApp.Services.CentroEfectivo.Shared.Domain;

namespace VCashApp.Services.CentroEfectivo.Collection.Application
{
    public sealed class CollectionReadService : ICollectionReadService
    {
        private readonly AppDbContext _db;
        private readonly ICefContainerRepository _containers;

        public CollectionReadService(
            AppDbContext db,
            ICefContainerRepository containers)
        {
            _db = db;
            _containers = containers;
        }

        public async Task<CefTransactionCheckinViewModel> GetCheckinAsync(string? serviceOrderId, string userId, string ipAddress)
        {
            // usuario
            var userName = await _db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.NombreUsuario)
                .FirstOrDefaultAsync() ?? "N/A";

            var vm = new CefTransactionCheckinViewModel
            {
                ServiceOrderId = serviceOrderId,
                RegistrationDate = DateTime.Now,
                RegistrationUserName = userName,
                IPAddress = ipAddress,
                Currencies = new List<SelectListItem>
                {
                    new("COP","COP"), new("USD","USD"), new("EUR","EUR")
                },
                // si tu VM expone TransactionTypes y los necesitas en UI:
                TransactionTypes = Enum.GetValues(typeof(CefTransactionTypeEnum))
                    .Cast<CefTransactionTypeEnum>()
                    .Select(e => new SelectListItem { Value = e.ToString(), Text = e.ToString().Replace("_", " ") })
                    .ToList()
            };

            if (string.IsNullOrWhiteSpace(serviceOrderId))
                return vm;

            // datos del servicio (cabecera)
            var s = await _db.CgsServicios.AsNoTracking()
                .Where(x => x.ServiceOrderId == serviceOrderId)
                .Select(x => new {
                    x.ServiceOrderId,
                    x.BranchCode,
                    x.ConceptCode,
                    x.ClientCode,
                    x.OriginClientCode,
                    x.OriginPointCode,
                    x.OriginIndicatorType,
                    x.DestinationPointCode,
                    x.DestinationIndicatorType,
                    x.NumberOfCoinBags,
                    x.BillValue,
                    x.CoinValue
                })
                .FirstOrDefaultAsync()
                ?? throw new InvalidOperationException($"No existe el servicio '{serviceOrderId}'.");

            // cliente (preferir origen)
            var cliId = s.OriginClientCode != 0 ? s.OriginClientCode : s.ClientCode;
            vm.ClientName = await _db.AdmClientes.AsNoTracking()
                .Where(c => c.ClientCode == cliId)
                .Select(c => c.ClientName)
                .FirstOrDefaultAsync() ?? "N/A";

            // sucursal
            vm.BranchName = await _db.AdmSucursales.AsNoTracking()
                .Where(b => b.CodSucursal == s.BranchCode)
                .Select(b => b.NombreSucursal)
                .FirstOrDefaultAsync() ?? "N/A";

            // origen/destino
            vm.OriginLocationName = s.OriginIndicatorType == "P"
                ? (await _db.AdmPuntos.AsNoTracking().Where(p => p.PointCode == s.OriginPointCode).Select(p => p.PointName).FirstOrDefaultAsync()
                    ?? $"{s.OriginIndicatorType}-{s.OriginPointCode}")
                : (await _db.AdmFondos.AsNoTracking().Where(f => f.FundCode == s.OriginPointCode).Select(f => f.FundName).FirstOrDefaultAsync()
                    ?? $"{s.OriginIndicatorType}-{s.OriginPointCode}");

            vm.DestinationLocationName = s.DestinationIndicatorType == "P"
                ? (await _db.AdmPuntos.AsNoTracking().Where(p => p.PointCode == s.DestinationPointCode).Select(p => p.PointName).FirstOrDefaultAsync()
                    ?? $"{s.DestinationIndicatorType}-{s.DestinationPointCode}")
                : (await _db.AdmFondos.AsNoTracking().Where(f => f.FundCode == s.DestinationPointCode).Select(f => f.FundName).FirstOrDefaultAsync()
                    ?? $"{s.DestinationIndicatorType}-{s.DestinationPointCode}");

            // si ya existe transacción previa, toma sus declarados; si no, usar valores del servicio
            var t = await _db.CefTransactions.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ServiceOrderId == serviceOrderId);

            vm.DeclaredBillValue = t?.DeclaredBillValue ?? (s.BillValue ?? 0m);
            vm.DeclaredCoinValue = t?.DeclaredCoinValue ?? (s.CoinValue ?? 0m);
            vm.DeclaredDocumentValue = t?.DeclaredDocumentValue ?? 0m;
            vm.TotalDeclaredValue = vm.DeclaredBillValue + vm.DeclaredCoinValue + vm.DeclaredDocumentValue;

            vm.DeclaredBagCount = t?.DeclaredBagCount ?? (s.NumberOfCoinBags ?? 0);
            vm.DeclaredEnvelopeCount = t?.DeclaredEnvelopeCount ?? 0;
            vm.DeclaredCheckCount = t?.DeclaredCheckCount ?? 0;
            vm.DeclaredDocumentCount = t?.DeclaredDocumentCount ?? 0;

            vm.SlipNumber = t?.SlipNumber ?? 0;
            vm.Currency = t?.Currency;
            vm.TransactionType = t?.TransactionType;
            vm.IsCustody = t?.IsCustody ?? false;
            vm.IsPointToPoint = t?.IsPointToPoint ?? false;
            vm.InformativeIncident = t?.InformativeIncident;

            return vm;
        }

        public async Task<CefProcessContainersPageViewModel?> GetProcessPageAsync(int txId)
        {
            var vm = new CefProcessContainersPageViewModel { CefTransactionId = txId };

            var t = await _db.CefTransactions.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == txId)
                ?? throw new InvalidOperationException($"Transacción CEF {txId} no existe.");

            var s = await _db.CgsServicios.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ServiceOrderId == t.ServiceOrderId)
                ?? throw new InvalidOperationException($"Servicio {t.ServiceOrderId} no existe.");

            vm.Transaction = new TransactionHeaderVM
            {
                Id = t.Id,
                SlipNumber = t.SlipNumber,
                Currency = t.Currency,
                TransactionType = t.TransactionType,
                Status = t.TransactionStatus,
                RegistrationDate = t.RegistrationDate,
                RegistrationUserName = await _db.Users.AsNoTracking()
                    .Where(u => u.Id == t.RegistrationUser)
                    .Select(u => u.NombreUsuario)
                    .FirstOrDefaultAsync() ?? "N/A"
            };

            var conceptName = await _db.AdmConceptos.AsNoTracking()
                .Where(c => c.CodConcepto == s.ConceptCode)
                .Select(c => c.NombreConcepto)
                .FirstOrDefaultAsync() ?? "N/A";

            var branchName = await _db.AdmSucursales.AsNoTracking()
                .Where(b => b.CodSucursal == s.BranchCode)
                .Select(b => b.NombreSucursal)
                .FirstOrDefaultAsync() ?? "N/A";

            var clientName = await _db.AdmClientes.AsNoTracking()
                .Where(c => c.ClientCode == (s.OriginClientCode != 0 ? s.OriginClientCode : s.ClientCode))
                .Select(c => c.ClientName)
                .FirstOrDefaultAsync() ?? "N/A";

            string originName = s.OriginIndicatorType == "P"
                ? await _db.AdmPuntos.AsNoTracking().Where(p => p.PointCode == s.OriginPointCode)
                    .Select(p => p.PointName).FirstOrDefaultAsync() ?? $"{s.OriginIndicatorType}-{s.OriginPointCode}"
                : await _db.AdmFondos.AsNoTracking().Where(f => f.FundCode == s.OriginPointCode)
                    .Select(f => f.FundName).FirstOrDefaultAsync() ?? $"{s.OriginIndicatorType}-{s.OriginPointCode}";

            string destinationName = s.DestinationIndicatorType == "P"
                ? await _db.AdmPuntos.AsNoTracking().Where(p => p.PointCode == s.DestinationPointCode)
                    .Select(p => p.PointName).FirstOrDefaultAsync() ?? $"{s.DestinationIndicatorType}-{s.DestinationPointCode}"
                : await _db.AdmFondos.AsNoTracking().Where(f => f.FundCode == s.DestinationPointCode)
                    .Select(f => f.FundName).FirstOrDefaultAsync() ?? $"{s.DestinationIndicatorType}-{s.DestinationPointCode}";

            vm.Service = new ServiceHeaderVM
            {
                ServiceOrderId = s.ServiceOrderId,
                BranchCode = s.BranchCode,
                BranchName = branchName,
                ServiceDate = s.ProgrammingDate,
                ServiceTime = s.ProgrammingTime,
                ConceptName = conceptName,
                OriginName = originName,
                DestinationName = destinationName,
                ClientName = clientName
            };

            var contenedores = await _db.CefContainers
                .AsNoTracking()
                .Include(c => c.ValueDetails)
                .Where(c => c.CefTransactionId == txId)
                .ToListAsync();

            if (!contenedores.Any())
            {
                vm.Containers = new List<CefContainerProcessingViewModel>
                {
                    new CefContainerProcessingViewModel
                    {
                        CefTransactionId = txId,
                        ContainerType = CefContainerTypeEnum.Bolsa
                    }
                };
            }
            else
            {
                var parentMap = contenedores.ToDictionary(x => x.Id, x => x.ContainerCode);
                vm.Containers = contenedores.Select(c => new CefContainerProcessingViewModel
                {
                    Id = c.Id,
                    CefTransactionId = c.CefTransactionId,
                    ContainerCode = c.ContainerCode,
                    ContainerType = Enum.TryParse<CefContainerTypeEnum>(c.ContainerType, out var tipo) ? tipo : CefContainerTypeEnum.Bolsa,
                    EnvelopeSubType = Enum.TryParse<CefEnvelopeSubTypeEnum>(c.EnvelopeSubType, out var subtipo) ? subtipo : null,
                    Observations = c.Observations,
                    ClientCashierId = c.ClientCashierId,
                    ClientCashierName = c.ClientCashierName,
                    ClientEnvelopeDate = c.ClientEnvelopeDate,
                    ParentContainerId = c.ParentContainerId,
                    ParentContainerCode = c.ParentContainerId.HasValue && parentMap.TryGetValue(c.ParentContainerId.Value, out var code) ? code : null,
                    ValueDetails = c.ValueDetails?.Select(d => new CefValueDetailViewModel
                    {
                        Id = d.Id,
                        DenominationId = d.DenominationId,
                        Quantity = d.Quantity,
                        BundlesCount = d.BundlesCount,
                        LoosePiecesCount = d.LoosePiecesCount,
                        UnitValue = d.UnitValue,
                        CalculatedAmount = d.CalculatedAmount ?? 0,
                        IsHighDenomination = d.IsHighDenomination,
                        EntitieBankId = d.EntitieBankId,
                        AccountNumber = d.AccountNumber,
                        CheckNumber = d.CheckNumber,
                        IssueDate = d.IssueDate,
                        Observations = d.Observations,
                        QualityId = d.QualityId,
                        ValueType = Enum.TryParse<CefValueTypeEnum>(d.ValueType, out var tipoValor) ? tipoValor : CefValueTypeEnum.Billete,
                    }).ToList()
                }).ToList();
            }

            var totals = await _containers.GetTotalsAsync(txId);
            vm.TotalDeclaredAll = totals.DeclaredCash;
            vm.CountedBillsAll = totals.BillHigh + totals.BillLow;
            vm.CountedBillHighAll = totals.BillHigh;
            vm.CountedBillLowAll = totals.BillLow;
            vm.CountedCoinsAll = totals.CoinTotal;
            vm.CountedChecksAll = totals.CheckTotal;
            vm.CountedDocsAll = totals.DocTotal;

            vm.TotalCountedAll = vm.CountedBillsAll + vm.CountedCoinsAll;
            vm.TotalOverallAll = vm.TotalCountedAll + vm.CountedChecksAll + vm.CountedDocsAll;
            vm.DifferenceAll = vm.TotalCountedAll - vm.TotalDeclaredAll;
            return vm;
        }

        public async Task<CefTransactionDetailViewModel?> GetDetailAsync(int txId)
        {
            var tx = await _db.CefTransactions
                .AsNoTracking()
                .AsSplitQuery()
                .Include(t => t.Service).ThenInclude(s => s.Concept)
                .Include(t => t.Branch)
                .Include(t => t.Containers).ThenInclude(c => c.ValueDetails).ThenInclude(vd => vd.AdmDenominacion)
                .Include(t => t.Containers).ThenInclude(c => c.ValueDetails).ThenInclude(vd => vd.AdmQuality)
                .Include(t => t.Containers).ThenInclude(c => c.Incidents)
                .Include(t => t.Incidents)
                .FirstOrDefaultAsync(t => t.Id == txId)
                ?? throw new InvalidOperationException($"Transacción {txId} no existe.");

            var userId = new HashSet<string>();
            if (!string.IsNullOrEmpty(tx.RegistrationUser)) userId.Add(tx.RegistrationUser);
            foreach (var inc in tx.Incidents)
                if (!string.IsNullOrEmpty(inc.ReportedUserId)) userId.Add(inc.ReportedUserId);

            var user = await _db.Users
                .AsNoTracking()
                .Where(u => userId.Contains(u.Id))
                .Select(u => new { u.Id, u.NombreUsuario })
                .ToDictionaryAsync(u => u.Id, u => u.NombreUsuario);

            var typeId = tx.Incidents.Select(i => i.IncidentTypeId).Where(id => id != null).Distinct().ToList();
            var incTypes = await _db.CefIncidentTypes
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
                    var face = await _db.AdmDenominaciones
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

        public async Task<CollectionSummaryVm> GetSummaryAsync(int txId)
        {
            var t = await _db.CefTransactions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == txId)
                ?? throw new InvalidOperationException($"Transacción {txId} no existe.");

            return new CollectionSummaryVm(
                t.Id, t.ServiceOrderId ?? "N/A", t.Currency ?? "N/A",
                t.TotalDeclaredValue, t.TotalCountedValue, Enum.Parse<CefTransactionStatusEnum>(t.TransactionStatus)
            );
        }

        public async Task<CefTransactionReviewViewModel?> GetReviewAsync(int txId, string? returnUrl)
        {
            var transaction = await _db.CefTransactions
                                            .Include(t => t.Service).ThenInclude(s => s.Concept)
                                            .Include(t => t.Containers).ThenInclude(c => c.ValueDetails).ThenInclude(vd => vd.AdmDenominacion)
                                            .Include(t => t.Containers).ThenInclude(c => c.Incidents)
                                            .Include(t => t.Incidents)
                                            .FirstOrDefaultAsync(t => t.Id == txId);

            if (transaction == null) return null;

            var conceptCode = transaction.Service?.Concept?.TipoConcepto;
            var conceptName = transaction.Service?.Concept?.NombreConcepto;
            var userRegistro = await _db.Users.FirstOrDefaultAsync(u => u.Id == transaction.RegistrationUser);
            var userRevisor = await _db.Users.FirstOrDefaultAsync(u => u.Id == transaction.ReviewerUserId);

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
                FinalObservations = transaction.InformativeIncident
            };

            viewModel.ContainerSummaries = transaction.Containers
                .Where(c => c.ParentContainerId == null)
                .Select(c => new CefContainerSummaryViewModel
                {
                    Id = c.Id,
                    ContainerType = Enum.Parse<CefContainerTypeEnum>(c.ContainerType),
                    ContainerCode = c.ContainerCode,
                    CountedValue = c.CountedValue ?? 0,
                    ContainerStatus = Enum.Parse<CefContainerStatusEnum>(c.ContainerStatus),
                    ProcessingUserName = _db.Users.FirstOrDefault(u => u.Id == c.ProcessingUserId)?.NombreUsuario ?? "N/A",
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
                            _db.CefIncidentTypes.FirstOrDefault(it => it.Id == ni.IncidentTypeId)?.Code,
                            out var cat
                        ) ? cat : CefIncidentTypeCategoryEnum.Sobrante,
                        Description = ni.Description,
                        AffectedAmount = ni.AffectedAmount,
                        ReportingUserName = _db.Users.FirstOrDefault(u => u.Id == ni.ReportedUserId)?.NombreUsuario ?? "N/A"
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
                            ProcessingUserName = _db.Users.FirstOrDefault(u => u.Id == ch.ProcessingUserId)?.NombreUsuario ?? "N/A",
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
                                    _db.CefIncidentTypes.FirstOrDefault(it => it.Id == ni.IncidentTypeId)?.Code,
                                    out var cat
                                ) ? cat : CefIncidentTypeCategoryEnum.Sobrante,
                                Description = ni.Description,
                                AffectedAmount = ni.AffectedAmount,
                                ReportingUserName = _db.Users.FirstOrDefault(u => u.Id == ni.ReportedUserId)?.NombreUsuario ?? "N/A"
                            }).ToList(),
                        }).ToList()
                }).ToList();

            viewModel.IncidentSummaries = transaction.Incidents.Select(ni => new CefIncidentSummaryViewModel
            {
                Id = ni.Id,
                IncidentType = IncidentTypeCodeMap.TryFromCode(
                    _db.CefIncidentTypes.FirstOrDefault(it => it.Id == ni.IncidentTypeId)?.Code,
                    out var cat
                ) ? cat : CefIncidentTypeCategoryEnum.Sobrante,
                Description = ni.Description,
                AffectedAmount = ni.AffectedAmount,
                ReportingUserName = _db.Users.FirstOrDefault(u => u.Id == ni.ReportedUserId)?.NombreUsuario ?? "N/A"
            }).ToList();

            viewModel.AvailableStatuses = new List<SelectListItem>
            {
                new (CefTransactionStatusEnum.Aprobado.ToString(), "Aprobada"),
                new (CefTransactionStatusEnum.Rechazado.ToString(), "Rechazada")
            };

            return viewModel;

            static string GetValueDetailDescription(CefValueDetail vd)
            {
                var etiqueta = vd.AdmDenominacion?.TipoDenominacion
                               ?? (vd.DenominationId.HasValue ? vd.DenominationId.Value.ToString("N0") : "Sin denom.");
                var cantidad = vd.Quantity;
                var monto = (vd.CalculatedAmount ?? 0m).ToString("N0");
                return $"{etiqueta} x {cantidad} ({monto})";
            }
        }

        private sealed class TxTotals
        {
            public decimal DeclaredCash { get; init; }
            public decimal BillHigh { get; init; }
            public decimal BillLow { get; init; }
            public decimal BillTotal => BillHigh + BillLow;
            public decimal CoinTotal { get; init; }
            public decimal CashTotal => BillTotal + CoinTotal;
            public decimal CheckTotal { get; init; }
            public decimal DocTotal { get; init; }
            public decimal OverallTotal => CashTotal + CheckTotal + DocTotal;
            public decimal Difference => CashTotal - DeclaredCash;
        }

        private async Task<TxTotals> GetTransactionTotalsAsync(int transactionId)
        {
            var tx = await _db.CefTransactions
                .AsNoTracking()
                .FirstAsync(t => t.Id == transactionId);

            var conceptType = await (
                from s in _db.CgsServicios.AsNoTracking()
                join c in _db.AdmConceptos.AsNoTracking() on s.ConceptCode equals c.CodConcepto
                where s.ServiceOrderId == tx.ServiceOrderId
                select c.TipoConcepto
            ).FirstOrDefaultAsync();

            bool isCollection = conceptType == "RC" || conceptType == "ET";
            var declaredCash = isCollection
                ? (tx.TotalDeclaredValue)
                : (tx.DeclaredBillValue + tx.DeclaredCoinValue);

            var details = await (
                from d in _db.CefValueDetails.AsNoTracking()
                join c in _db.CefContainers.AsNoTracking() on d.CefContainerId equals c.Id
                where c.CefTransactionId == transactionId
                select new
                {
                    d.ValueType,
                    d.IsHighDenomination,
                    d.CalculatedAmount
                }
            ).ToListAsync();

            decimal sum(string vt) =>
                details.Where(x => x.ValueType == vt)
                       .Sum(x => (decimal?)(x.CalculatedAmount ?? 0m)) ?? 0m;

            decimal billHigh = details
                .Where(x => x.ValueType == "Billete" && x.IsHighDenomination)
                .Sum(x => (decimal?)(x.CalculatedAmount ?? 0m)) ?? 0m;

            decimal billLow = details
                .Where(x => x.ValueType == "Billete" && !x.IsHighDenomination)
                .Sum(x => (decimal?)(x.CalculatedAmount ?? 0m)) ?? 0m;

            var coinTotal = sum("Moneda");
            var checkTotal = sum("Cheque");
            var docTotal = sum("Documento");

            return new TxTotals
            {
                DeclaredCash = declaredCash,
                BillHigh = billHigh,
                BillLow = billLow,
                CoinTotal = coinTotal,
                CheckTotal = checkTotal,
                DocTotal = docTotal
            };
        }
    }
}