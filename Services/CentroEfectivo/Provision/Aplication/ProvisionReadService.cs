using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Models.ViewModels.CentroEfectivo;

namespace VCashApp.Services.CentroEfectivo.Provision.Application
{
    /// <summary>
    /// Read Service adaptado al VM de detalle: CefTransactionDetailViewModel.
    /// Solo consulta y mapea para la UI.
    /// </summary>
    public sealed class ProvisionReadService : IProvisionReadService
    {
        private readonly AppDbContext _db;
        public ProvisionReadService(AppDbContext db) => _db = db;

        /// <summary>
        /// ***Puente para compatibilidad con el controlador antiguo.***
        /// En vez de usar este, migra a GetDetailAsync(int).
        /// </summary>
        public async Task<CefProcessContainersPageViewModel?> GetProcessPageAsync(int txId)
        {
            var tx = await _db.CefTransactions
                .AsNoTracking()
                .Include(t => t.Service).ThenInclude(s => s.Concept)
                .Include(t => t.Branch)
                .Include(t => t.Containers).ThenInclude(c => c.ValueDetails)
                .Include(t => t.Containers).ThenInclude(c => c.ParentContainer)
                .FirstOrDefaultAsync(t => t.Id == txId);

            if (tx is null) return null;

            var vm = new CefProcessContainersPageViewModel
            {
                CefTransactionId = tx.Id,

                // 👇 Usa los tipos top-level, no anidados
                Service = new ServiceHeaderVM
                {
                    ServiceOrderId = tx.ServiceOrderId ?? tx.Service?.ServiceOrderId ?? string.Empty,
                    BranchCode = tx.Branch?.CodSucursal ?? tx.BranchCode,
                    BranchName = tx.Branch?.NombreSucursal ?? tx.Service?.CgsBranchName ?? "N/A",
                    ServiceDate = tx.Service?.ProgrammingDate,
                    ServiceTime = tx.Service?.ProgrammingTime,
                    ConceptName = tx.Service?.Concept?.NombreConcepto ?? tx.Service?.Concept.NombreConcepto ?? "N/A",
                    OriginName = tx.Service?.OriginPointCode ?? "N/A",
                    DestinationName = tx.Service?.DestinationPointCode ?? "N/A",
                    ClientName = tx.Service?.Client?.ClientName ?? tx.Service?.Client?.ClientName ?? "N/A",
                },

                Transaction = new TransactionHeaderVM
                {
                    Id = tx.Id,
                    SlipNumber = tx.SlipNumber,                  // int?
                    Currency = tx.Currency,                    // string?
                    TransactionType = tx.TransactionType,             // si existe, si no deja null
                    Status = tx.TransactionStatus ?? "N/A",
                    RegistrationDate = tx.RegistrationDate,
                    RegistrationUserName = await _db.Users
                                                 .Where(u => u.Id == tx.RegistrationUser)
                                                 .Select(u => u.NombreUsuario ?? u.UserName)
                                                 .FirstOrDefaultAsync() ?? "N/A"
                }
            };

            // ===== Contenedores -> CefContainerProcessingViewModel =====
            vm.Containers = (tx.Containers ?? Enumerable.Empty<Models.Entities.CefContainer>())
                .OrderBy(c => c.ParentContainerId.HasValue ? 1 : 0) // bolsas primero
                .ThenBy(c => c.Id)
                .Select(c => new CefContainerProcessingViewModel
                {
                    Id = c.Id,
                    CefTransactionId = tx.Id,
                    ContainerType = Enum.TryParse<CefContainerTypeEnum>(c.ContainerType, true, out var ct) ? ct : default,
                    EnvelopeSubType = Enum.TryParse<CefEnvelopeSubTypeEnum>(c.EnvelopeSubType, true, out var st) ? st : default,
                    ParentContainerId = c.ParentContainerId,
                    ContainerCode = c.ContainerCode,
                    Observations = c.Observations,
                    ClientCashierId = c.ClientCashierId,
                    ClientCashierName = c.ClientCashierName,
                    ClientEnvelopeDate = c.ClientEnvelopeDate,
                    ContainerStatus = Enum.TryParse<CefContainerStatusEnum>(c.ContainerStatus, true, out var cs) ? cs : default,

                    ValueDetails = (c.ValueDetails ?? Enumerable.Empty<Models.Entities.CefValueDetail>())
                        .OrderBy(v => v.Id)
                        .Select(v => new CefValueDetailViewModel
                        {
                            Id = v.Id,
                            ValueType = Enum.TryParse<CefValueTypeEnum>(v.ValueType, true, out var vt) ? vt : default,
                            DenominationId = v.DenominationId,
                            QualityId = v.QualityId,
                            IsHighDenomination = v.IsHighDenomination,
                            Quantity = v.Quantity,
                            BundlesCount = v.BundlesCount,
                            LoosePiecesCount = v.LoosePiecesCount,
                            UnitValue = v.UnitValue,
                            CalculatedAmount = v.CalculatedAmount ?? 0,

                            // Cheque
                            EntitieBankId = v.EntitieBankId,
                            AccountNumber = v.AccountNumber,
                            CheckNumber = v.CheckNumber,
                            IssueDate = v.IssueDate,

                            Observations = v.Observations
                        })
                        .ToList()
                })
                .ToList();

            // ===== Totales para los tiles =====
            var declaredBills = tx.DeclaredBillValue;
            var declaredCoins = tx.DeclaredCoinValue;
            var declaredCash = declaredBills + declaredCoins;

            decimal countedBills = 0m, countedCoins = 0m, docs = 0m, checks = 0m;

            foreach (var c in vm.Containers)
                foreach (var d in c.ValueDetails)
                {
                    switch (d.ValueType)
                    {
                        case CefValueTypeEnum.Billete: countedBills += d.CalculatedAmount; break;
                        case CefValueTypeEnum.Moneda: countedCoins += d.CalculatedAmount; break;
                        case CefValueTypeEnum.Documento: docs += d.UnitValue ?? 0m; break; // Doc = monto directo
                        case CefValueTypeEnum.Cheque:
                            checks += (d.UnitValue ?? 0m) * (d.Quantity ?? 0);
                            break;
                    }
                }

            var countedCash = countedBills + countedCoins;

            vm.TotalDeclaredAll = declaredCash;
            vm.TotalCountedAll = countedCash;
            vm.DifferenceAll = countedCash - declaredCash;
            vm.TotalOverallAll = countedCash + docs + checks;

            vm.CountedBillsAll = countedBills;
            vm.CountedCoinsAll = countedCoins;
            vm.CountedDocsAll = docs;
            vm.CountedChecksAll = checks;

            vm.CountedBillHighAll = 0m; // si aún no aplicas regla de alta/baja
            vm.CountedBillLowAll = 0m;

            return vm;
        }

        /// <summary>
        /// Carga la página de proceso/detalle de la provisión (VM nuevo).
        /// </summary>
        public async Task<CefTransactionDetailViewModel> GetDetailAsync(int txId)
        {
            var tx = await _db.CefTransactions
                .AsNoTracking()
                .Include(t => t.Service).ThenInclude(s => s.Concept)
                .Include(t => t.Branch)
                .Include(t => t.Containers).ThenInclude(c => c.ValueDetails)
                .Include(t => t.Containers).ThenInclude(c => c.ParentContainer)
                .Include(t => t.Incidents).ThenInclude(i => i.IncidentType)
                .FirstOrDefaultAsync(t => t.Id == txId);

            if (tx is null) return default!;

            // Usuario registrador
            var registeredBy = await GetUserDisplayAsync(tx.RegistrationUser) ?? string.Empty;

            var header = new CefTransactionDetailViewModel
            {
                CefTransactionId = tx.Id,
                SlipNumber = tx.SlipNumber.ToString(), // ToString() no es null
                ServiceOrderId = tx.ServiceOrderId ?? tx.Service?.ServiceOrderId ?? string.Empty,
                ServiceConcept = tx.Service?.Concept?.CodConcepto.ToString()
                                   ?? tx.Service?.ConceptCode.ToString()
                                   ?? string.Empty,
                BranchName = tx.Branch?.CodSucursal.ToString()
                                   ?? tx.Service?.CgsBranchName
                                   ?? tx.BranchCode.ToString(),
                CurrencyCode = tx.Currency ?? string.Empty,
                CurrentStatus = tx.TransactionStatus ?? string.Empty,
                RegistrationDate = tx.RegistrationDate,
                RegisteredByName = registeredBy
            };

            // Contenedores y valores
            foreach (var c in (tx.Containers ?? Enumerable.Empty<Models.Entities.CefContainer>()).OrderBy(x => x.Id))
            {
                var containerVm = new DetailContainerVM
                {
                    Id = c.Id,
                    ContainerType = c.ContainerType ?? string.Empty,
                    ContainerCode = c.ContainerCode ?? string.Empty,
                    ParentContainerId = c.ParentContainerId,
                    ParentLabel = c.ParentContainer is null
                        ? string.Empty
                        : $"Bolsa {c.ParentContainer.Id} — {c.ParentContainer.ContainerCode}",
                    Subtotal = c.CountedValue ?? 0m
                };

                foreach (var v in (c.ValueDetails ?? Enumerable.Empty<Models.Entities.CefValueDetail>()))
                {
                    var vType = SafeParseEnum<CefValueTypeEnum>(v.ValueType);

                    if (vType == CefValueTypeEnum.Cheque)
                    {
                        containerVm.Checks.Add(new DetailCheckRowVM
                        {
                            BankName = v.AdmBankEntitie?.Name ?? string.Empty,
                            AccountNumber = string.Empty, // no hay campo en entidad
                            CheckNumber = string.Empty, // no hay campo en entidad
                            IssueDate = ToDateTimeOrNull(v.IssueDate),
                            Amount = v.CalculatedAmount ?? ((v.UnitValue ?? 0m) * (decimal)v.Quantity)
                        });
                    }
                    else
                    {
                        var denomName = v.AdmDenominacion?.ValorDenominacion?.ToString("N0") ?? string.Empty;
                        var qualityName = v.AdmQuality?.QualityName ?? string.Empty;

                        containerVm.Values.Add(new DetailValueRowVM
                        {
                            ValueType = vType.ToString(), // Billete/Moneda/Documento
                            DenominationName = denomName,
                            QualityName = qualityName,
                            IsHighDenomination = null, // sin regla aún
                            Quantity = v.Quantity ?? 0,
                            Bundles = v.BundlesCount ?? 0,
                            Loose = v.LoosePiecesCount ?? 0,
                            UnitValue = v.UnitValue ?? 0m,
                            CalculatedAmount = v.CalculatedAmount ?? ((v.UnitValue ?? 0m) * (decimal)v.Quantity)
                        });
                    }
                }

                header.Containers.Add(containerVm);
            }

            // Incidencias
            foreach (var i in (tx.Incidents ?? Enumerable.Empty<Models.Entities.CefIncident>()).OrderBy(x => x.Id))
            {
                var c = i.CefContainer;
                var label = c is null ? string.Empty : $"{(c.ContainerType ?? "Bolsa")} {c.Id} — {c.ContainerCode}";

                header.Incidents.Add(new DetailIncidentVM
                {
                    Id = i.Id,
                    Code = i.IncidentType?.Code ?? string.Empty,
                    Description = i.Description ?? string.Empty,
                    Status = i.IncidentStatus ?? string.Empty,
                    ReportedBy = await GetUserDisplayAsync(i.ReportedUserId) ?? string.Empty,
                    ReportedAt = i.IncidentDate,
                    ContainerId = i.CefContainerId,
                    ContainerLabel = label,
                    AffectedAmount = i.AffectedAmount,
                    AffectedDenomination = i.AffectedDenomination,
                    AffectedQuantity = i.AffectedQuantity
                });
            }

            // Totales
            header.TotalDeclared = tx.DeclaredBillValue + tx.DeclaredCoinValue;
            header.TotalCounted = tx.TotalCountedValue is decimal tcv ? tcv : SumCounted(header);
            header.NetDiff = header.TotalCounted - header.TotalDeclared;

            header.CoinsTotal = SumByType(header, CefValueTypeEnum.Moneda);
            var bills = SumByType(header, CefValueTypeEnum.Billete);
            header.BillsHigh = 0m; // sin regla aún
            header.BillsLow = 0m; // sin regla aún
            header.DocsTotal = SumByType(header, CefValueTypeEnum.Documento);
            header.ChecksTotal = header.Containers.SelectMany(x => x.Checks).Sum(x => x.Amount);
            header.TotalOverall = header.TotalCounted + header.DocsTotal + header.ChecksTotal;

            return header;
        }

        public async Task<ProvisionSummaryVm> GetSummaryAsync(int txId)
        {
            var t = await _db.CefTransactions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == txId);
            if (t is null) return default!;

            var declared = t.DeclaredBillValue + t.DeclaredCoinValue;
            var counted = t.TotalCountedValue;

            return new ProvisionSummaryVm(
                t.Id,
                t.ServiceOrderId,
                t.Currency,
                declared,
                counted,
                SafeParseEnum<CefTransactionStatusEnum>(t.TransactionStatus)
            );
        }

        // ===== Helpers =====

        private static TEnum SafeParseEnum<TEnum>(string? value) where TEnum : struct
            => (!string.IsNullOrWhiteSpace(value) && Enum.TryParse<TEnum>(value, true, out var e)) ? e : default;

        private async Task<string?> GetUserDisplayAsync(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;
            var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
            return u?.NombreUsuario ?? u?.UserName;
        }

        private static DateTime? ToDateTimeOrNull(DateOnly? d)
            => d.HasValue ? d.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null;

        private static decimal SumCounted(CefTransactionDetailViewModel vm)
            => vm.Containers.Sum(c =>
                   c.Values.Where(v => v.ValueType == nameof(CefValueTypeEnum.Billete)
                                    || v.ValueType == nameof(CefValueTypeEnum.Moneda))
                           .Sum(v => v.CalculatedAmount));

        private static decimal SumByType(CefTransactionDetailViewModel vm, CefValueTypeEnum type)
            => vm.Containers.Sum(c =>
                   c.Values.Where(v => string.Equals(v.ValueType, type.ToString(), StringComparison.OrdinalIgnoreCase))
                           .Sum(v => v.CalculatedAmount));
    }
}