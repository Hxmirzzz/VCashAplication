using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.CentroEfectivo.Shared;
using VCashApp.Services.CentroEfectivo.Provision.Domain;
using VCashApp.Services.CentroEfectivo.Shared.Domain;

namespace VCashApp.Services.CentroEfectivo.Shared.Infrastructure
{
    /// <summary>Repositorio EF para contenedores y detalles de valor.</summary>
    public sealed class CefContainerRepository : ICefContainerRepository
    {
        private readonly AppDbContext _db;

        public CefContainerRepository(AppDbContext db) => _db = db;

        public async Task<CefContainer?> GetWithDetailAsync(int containerId)
        {
            return await _db.CefContainers
                .Include(c => c.ValueDetails).ThenInclude(vd => vd.Incidents)
                .Include(c => c.Incidents)
                .Include(c => c.ChildContainers)
                .FirstOrDefaultAsync(c => c.Id == containerId);
        }

        public async Task<List<CefContainer>> GetByTransactionIdAsync(int txId)
        {
            return await _db.CefContainers
                .Where(c => c.CefTransactionId == txId)
                .Include(c => c.Incidents)
                .Include(c => c.ChildContainers)
                .OrderBy(c => c.Id)
                .ToListAsync();
        }

        public async Task<(bool sobres, bool documentos, bool cheques)> GetPointCapsAsync(string serviceOrderId)
        {
            var s = await _db.CgsServicios
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ServiceOrderId == serviceOrderId)
                ?? throw new InvalidOperationException($"Servicio {serviceOrderId} no existe.");

            AdmPunto? p = null;
            if (s.OriginIndicatorType == "P")
                p = await _db.AdmPuntos.AsNoTracking().FirstOrDefaultAsync(x => x.PointCode == s.OriginPointCode);
            else if (s.DestinationIndicatorType == "P")
                p = await _db.AdmPuntos.AsNoTracking().FirstOrDefaultAsync(x => x.PointCode == s.DestinationPointCode);

            return (p?.PointEnvelopes ?? false, p?.PointDocuments ?? false, p?.PointChecks ?? false);
        }

        public async Task<bool> DeleteAsync(int txId, int containerId)
        {
            var container = await _db.CefContainers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == containerId && c.CefTransactionId == txId);
            if (container == null)
                throw new InvalidOperationException($"Contenedor {containerId} no encontrado en la transacción {txId}.");

            bool isBag = string.Equals(container.ContainerType, CefContainerTypeEnum.Bolsa.ToString(), StringComparison.OrdinalIgnoreCase);
            if (isBag)
            {
                var bagCount = await _db.CefContainers.AsNoTracking()
                    .CountAsync(c => c.CefTransactionId == txId && c.ParentContainerId == null &&
                                     c.ContainerType == CefContainerTypeEnum.Bolsa.ToString());
                if (bagCount <= 1)
                    throw new InvalidOperationException("No se puede eliminar la única bolsa de la transacción.");
            }

            var hasChildren = await _db.CefContainers.AsNoTracking()
                .AnyAsync(c => c.ParentContainerId == containerId);
            if (hasChildren)
                throw new InvalidOperationException("No se puede eliminar un contenedor padre porque tiene sobres hijos.");

            await _db.CefValueDetails.Where(d => d.CefContainerId == containerId).ExecuteDeleteAsync();
            var rows = await _db.CefContainers
                .Where(c => c.Id == containerId && c.CefTransactionId == txId)
                .ExecuteDeleteAsync();

            if (rows == 0) return false;

            var totals = await GetTotalsAsync(txId);
            var tx = await _db.CefTransactions.FirstOrDefaultAsync(t => t.Id == txId);
            if (tx != null)
            {
                var billTotal = totals.BillHigh + totals.BillLow;
                var cashTotal = billTotal + totals.CoinTotal;
                var overall = cashTotal + totals.CheckTotal + totals.DocTotal;

                tx.CountedBillHighValue = totals.BillHigh;
                tx.CountedBillLowValue = totals.BillLow;
                tx.CountedBillValue = billTotal;
                tx.CountedCoinValue = totals.CoinTotal;

                tx.TotalCountedValue = cashTotal;
                tx.ValueDifference = cashTotal - totals.DeclaredCash;

                tx.CountedCheckValue = totals.CheckTotal;
                tx.CountedDocumentValue = totals.DocTotal;
                tx.OverallCountedValue = overall;

                tx.TotalDeclaredValueInWords = AmountInWords(tx.TotalDeclaredValue, tx.Currency);
                tx.TotalCountedValueInWords = AmountInWords(tx.TotalCountedValue, tx.Currency);
                tx.OverallCountedValueInWords = AmountInWords(tx.OverallCountedValue, tx.Currency);
                tx.LastUpdateDate = DateTime.Now;

                _db.CefTransactions.Update(tx);
                await _db.SaveChangesAsync();
            }

            return true;

            static string AmountInWords(decimal val, string? currency)
                => Utils.AmountInWordsHelper.ToSpanishCurrency(val, currency);
        }

        public async Task<decimal> SumCountedAsync(int txId)
        {
            var detailsSum = await (
                from c in _db.CefContainers
                where c.CefTransactionId == txId
                from vd in c.ValueDetails
                select (decimal?)(vd.CalculatedAmount ?? 0m)
            ).SumAsync() ?? 0m;

            if (detailsSum > 0m) return detailsSum;

            var containersSum = await _db.CefContainers
                .Where(c => c.CefTransactionId == txId)
                .SumAsync(c => (decimal?)(c.CountedValue ?? 0m)) ?? 0m;

            return containersSum;
        }

        public async Task<(decimal DeclaredCash, decimal BillHigh, decimal BillLow, decimal CoinTotal, decimal CheckTotal, decimal DocTotal)>
            GetTotalsAsync(int txId)
        {
            var tx = await _db.CefTransactions.AsNoTracking().FirstAsync(t => t.Id == txId);

            var conceptType = await (
                from s in _db.CgsServicios.AsNoTracking()
                join c in _db.AdmConceptos.AsNoTracking() on s.ConceptCode equals c.CodConcepto
                where s.ServiceOrderId == tx.ServiceOrderId
                select c.TipoConcepto
            ).FirstOrDefaultAsync();

            bool isCollection = conceptType == "RC" || conceptType == "ET";
            var declaredCash = isCollection ? tx.TotalDeclaredValue : (tx.DeclaredBillValue + tx.DeclaredCoinValue);

            var details = await (
                from d in _db.CefValueDetails.AsNoTracking()
                join c in _db.CefContainers.AsNoTracking() on d.CefContainerId equals c.Id
                where c.CefTransactionId == txId
                select new { d.ValueType, d.IsHighDenomination, d.CalculatedAmount }
            ).ToListAsync();

            decimal sum(string vt) => details.Where(x => x.ValueType == vt)
                .Sum(x => (decimal?)(x.CalculatedAmount ?? 0m)) ?? 0m;

            var billHigh = details.Where(x => x.ValueType == "Billete" && x.IsHighDenomination)
                                  .Sum(x => (decimal?)(x.CalculatedAmount ?? 0m)) ?? 0m;
            var billLow = details.Where(x => x.ValueType == "Billete" && !x.IsHighDenomination)
                                  .Sum(x => (decimal?)(x.CalculatedAmount ?? 0m)) ?? 0m;
            var coin = sum("Moneda");
            var check = sum("Cheque");
            var doc = sum("Documento");

            return (declaredCash, billHigh, billLow, coin, check, doc);
        }

        public async Task SaveContainersAndDetailsAsync(
            int txId,
            IReadOnlyList<CefContainerProcessingViewModel> containers,
            string userId,
            IAllowedValueTypesPolicy allowed)
        {
            var tx = await _db.CefTransactions.FirstOrDefaultAsync(t => t.Id == txId)
                ?? throw new InvalidOperationException($"Transacción {txId} no encontrada.");

            var existing = await _db.CefContainers
                .Include(c => c.ValueDetails)
                .Where(c => c.CefTransactionId == txId)
                .ToListAsync();

            var byId = existing.ToDictionary(c => c.Id, c => c);

            foreach (var cvm in containers ?? Array.Empty<CefContainerProcessingViewModel>())
            {
                var isSobre = cvm.ContainerType == CefContainerTypeEnum.Sobre;
                if (isSobre && cvm.ParentContainerId == null)
                    throw new InvalidOperationException("Los sobres deben tener una bolsa padre.");

                CefContainer c;
                if (cvm.Id == 0)
                {
                    c = new CefContainer
                    {
                        CefTransactionId = txId,
                        ParentContainerId = cvm.ParentContainerId,
                        ContainerType = cvm.ContainerType.ToString(),
                        EnvelopeSubType = isSobre && cvm.EnvelopeSubType.HasValue ? cvm.EnvelopeSubType.ToString() : null,
                        ContainerCode = cvm.ContainerCode,
                        Observations = cvm.Observations,
                        ClientCashierId = cvm.ClientCashierId,
                        ClientCashierName = cvm.ClientCashierName,
                        ClientEnvelopeDate = cvm.ClientEnvelopeDate,
                        ContainerStatus = CefContainerStatusEnum.InProcess.ToString(),
                        ProcessingUserId = userId,
                        ProcessingDate = DateTime.Now
                    };
                    await _db.CefContainers.AddAsync(c);
                    await _db.SaveChangesAsync();
                    byId[c.Id] = c;
                }
                else
                {
                    if (!byId.TryGetValue(cvm.Id, out c!))
                        throw new InvalidOperationException($"Contenedor {cvm.Id} no encontrado en la transacción {txId}.");

                    c.ParentContainerId = cvm.ParentContainerId;
                    c.ContainerType = cvm.ContainerType.ToString();
                    c.EnvelopeSubType = isSobre ? cvm.EnvelopeSubType?.ToString() : null;
                    c.ContainerCode = cvm.ContainerCode;
                    c.Observations = cvm.Observations;
                    c.ClientCashierId = cvm.ClientCashierId;
                    c.ClientCashierName = cvm.ClientCashierName;
                    c.ClientEnvelopeDate = cvm.ClientEnvelopeDate;
                    _db.CefContainers.Update(c);
                }

                var incoming = cvm.ValueDetails ?? new List<CefValueDetailViewModel>();
                var existingDetails = await _db.CefValueDetails
                    .Where(d => d.CefContainerId == c.Id)
                    .ToListAsync();

                var incomingIds = incoming.Where(v => v.Id != 0).Select(v => v.Id).ToHashSet();
                var toRemove = existingDetails.Where(d => !incomingIds.Contains(d.Id)).ToList();
                if (toRemove.Count > 0)
                    _db.CefValueDetails.RemoveRange(toRemove);

                decimal subtotal = 0m;

                foreach (var dvm in incoming)
                {
                    if (!allowed.IsAllowed(dvm.ValueType))
                        throw new InvalidOperationException($"Tipo de valor '{dvm.ValueType}' no permitido.");

                    if (isSobre && !IsEnvelopeDetailValid(cvm.EnvelopeSubType, dvm.ValueType))
                        throw new InvalidOperationException($"Detalle inválido para sobre {cvm.EnvelopeSubType}: {dvm.ValueType}.");

                    CefValueDetail d;
                    if (dvm.Id == 0)
                    {
                        d = new CefValueDetail
                        {
                            CefContainerId = c.Id,
                            ValueType = dvm.ValueType.ToString(),
                            DenominationId = dvm.DenominationId,
                            QualityId = dvm.QualityId,
                            Quantity = dvm.Quantity,
                            BundlesCount = dvm.BundlesCount,
                            LoosePiecesCount = dvm.LoosePiecesCount,
                            UnitValue = dvm.UnitValue,
                            IsHighDenomination = dvm.IsHighDenomination,
                            EntitieBankId = dvm.EntitieBankId,
                            AccountNumber = dvm.AccountNumber,
                            CheckNumber = dvm.CheckNumber,
                            IssueDate = dvm.IssueDate,
                            Observations = dvm.Observations
                        };
                        d.CalculatedAmount = await CalcAmountAsync(d);
                        subtotal += d.CalculatedAmount ?? 0m;

                        await _db.CefValueDetails.AddAsync(d);
                    }
                    else
                    {
                        d = existingDetails.FirstOrDefault(ed => ed.Id == dvm.Id)
                            ?? throw new InvalidOperationException($"Detalle {dvm.Id} no encontrado en el contenedor {c.Id}.");
                        d.ValueType = dvm.ValueType.ToString();
                        d.DenominationId = dvm.DenominationId;
                        d.QualityId = dvm.QualityId;
                        d.Quantity = dvm.Quantity;
                        d.BundlesCount = dvm.BundlesCount;
                        d.LoosePiecesCount = dvm.LoosePiecesCount;
                        d.UnitValue = dvm.UnitValue;
                        d.IsHighDenomination = dvm.IsHighDenomination;
                        d.EntitieBankId = dvm.EntitieBankId;
                        d.AccountNumber = dvm.AccountNumber;
                        d.CheckNumber = dvm.CheckNumber;
                        d.IssueDate = dvm.IssueDate;
                        d.Observations = dvm.Observations;

                        d.CalculatedAmount = await CalcAmountAsync(d);
                        subtotal += d.CalculatedAmount ?? 0m;

                        _db.CefValueDetails.Update(d);
                    }
                }
                c.CountedValue = subtotal;
                if ((c.ContainerStatus == CefContainerStatusEnum.InProcess.ToString() ||
                     c.ContainerStatus == CefContainerStatusEnum.Pending.ToString()) &&
                     subtotal > 0m)
                {
                    c.ContainerStatus = CefContainerStatusEnum.Procesado.ToString();
                    c.ProcessingDate = DateTime.Now;
                }

                _db.CefContainers.Update(c);
                await _db.SaveChangesAsync();
            }
        }

        private static bool IsEnvelopeDetailValid(CefEnvelopeSubTypeEnum? sub, CefValueTypeEnum type)
        {
            if (sub == null) return true;
            return sub.Value switch
            {
                CefEnvelopeSubTypeEnum.Efectivo => type == CefValueTypeEnum.Billete || type == CefValueTypeEnum.Moneda,
                CefEnvelopeSubTypeEnum.Documento => type == CefValueTypeEnum.Documento,
                CefEnvelopeSubTypeEnum.Cheque => type == CefValueTypeEnum.Cheque,
                _ => false
            };
        }

        private async Task<decimal> CalcAmountAsync(CefValueDetail d)
        {
            if (d.ValueType.Equals("Documento", StringComparison.OrdinalIgnoreCase))
                return d.UnitValue ?? 0m;

            if (d.ValueType.Equals("Cheque", StringComparison.OrdinalIgnoreCase))
            {
                var qty = d.Quantity.HasValue && d.Quantity.Value > 0 ? d.Quantity.Value : 1;
                return (d.UnitValue ?? 0m) * qty;
            }

            decimal unit = d.UnitValue ?? 0m;
            int piezasPorFajo = 100;

            if (d.DenominationId != null)
            {
                var den = await _db.AdmDenominaciones
                    .Where(x => x.CodDenominacion == d.DenominationId.Value)
                    .Select(x => new { Valor = x.ValorDenominacion ?? 0m, Fajo = x.CantidadUnidadAgrupamiento ?? 0 })
                    .FirstOrDefaultAsync();

                if (den != null)
                {
                    if (unit <= 0m) unit = den.Valor;
                    if (den.Fajo > 0) piezasPorFajo = den.Fajo;
                }
            }

            int bundles = d.BundlesCount ?? 0;
            int loose = d.LoosePiecesCount ?? 0;
            int qtyCalc = d.Quantity ?? (bundles * piezasPorFajo + loose);

            if (qtyCalc < 0) qtyCalc = 0;
            if (unit < 0) unit = 0;

            return unit * qtyCalc;
        }
    }
}