using DocumentFormat.OpenXml.Math;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.CentroEfectivo;
using VCashApp.Utils;

namespace VCashApp.Services.Cef
{
    /// <summary>
    /// Implementación del servicio para la gestión de Contenedores de Efectivo (Bolsas, Sobres).
    /// </summary>
    public class CefContainerService : ICefContainerService
    {
        private readonly AppDbContext _context;

        public CefContainerService(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<CefProcessContainersPageViewModel> PrepareProcessContainersPageAsync(int cefTransactionId)
        {
            var vm = new CefProcessContainersPageViewModel { CefTransactionId = cefTransactionId };

            // Transacción
            var t = await _context.CefTransactions.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == cefTransactionId)
                ?? throw new InvalidOperationException($"Transacción CEF {cefTransactionId} no existe.");

            // Servicio
            var s = await _context.CgsServicios.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ServiceOrderId == t.ServiceOrderId)
                ?? throw new InvalidOperationException($"Servicio {t.ServiceOrderId} no existe.");

            // Cabecera Transacción
            vm.Transaction = new TransactionHeaderVM
            {
                Id = t.Id,
                SlipNumber = t.SlipNumber,
                Currency = t.Currency,
                TransactionType = t.TransactionType,
                Status = t.TransactionStatus,
                RegistrationDate = t.RegistrationDate,
                RegistrationUserName = await _context.Users.AsNoTracking()
                    .Where(u => u.Id == t.RegistrationUser)
                    .Select(u => u.NombreUsuario)
                    .FirstOrDefaultAsync() ?? "N/A"
            };

            // Cabecera Servicio
            var conceptName = await _context.AdmConceptos.AsNoTracking()
                .Where(c => c.CodConcepto == s.ConceptCode)
                .Select(c => c.NombreConcepto)
                .FirstOrDefaultAsync() ?? "N/A";

            var branchName = await _context.AdmSucursales.AsNoTracking()
                .Where(b => b.CodSucursal == s.BranchCode)
                .Select(b => b.NombreSucursal)
                .FirstOrDefaultAsync() ?? "N/A";

            var clientName = await _context.AdmClientes.AsNoTracking()
                .Where(c => c.ClientCode == (s.OriginClientCode != 0 ? s.OriginClientCode : s.ClientCode))
                .Select(c => c.ClientName)
                .FirstOrDefaultAsync() ?? "N/A";

            string originName = s.OriginIndicatorType == "P"
                ? await _context.AdmPuntos.AsNoTracking()
                    .Where(p => p.PointCode == s.OriginPointCode).Select(p => p.PointName).FirstOrDefaultAsync() ?? $"{s.OriginIndicatorType}-{s.OriginPointCode}"
                : await _context.AdmFondos.AsNoTracking()
                    .Where(f => f.FundCode == s.OriginPointCode).Select(f => f.FundName).FirstOrDefaultAsync() ?? $"{s.OriginIndicatorType}-{s.OriginPointCode}";

            string destinationName = s.DestinationIndicatorType == "P"
                ? await _context.AdmPuntos.AsNoTracking()
                    .Where(p => p.PointCode == s.DestinationPointCode).Select(p => p.PointName).FirstOrDefaultAsync() ?? $"{s.DestinationIndicatorType}-{s.DestinationPointCode}"
                : await _context.AdmFondos.AsNoTracking()
                    .Where(f => f.FundCode == s.DestinationPointCode).Select(f => f.FundName).FirstOrDefaultAsync() ?? $"{s.DestinationIndicatorType}-{s.DestinationPointCode}";

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

            var contenedores = await _context.CefContainers
                .AsNoTracking()
                .Include(c => c.ValueDetails)
                .Where(c => c.CefTransactionId == cefTransactionId)
                .ToListAsync();

            // Si no hay contenedores creados aún, cargar uno vacío para Check-In
            if (!contenedores.Any())
            {
                vm.Containers = new List<CefContainerProcessingViewModel>
                {
                    new CefContainerProcessingViewModel
                    {
                        CefTransactionId = cefTransactionId,
                        ContainerType = CefContainerTypeEnum.Bolsa
                    }
                };
            }
            else
            {
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
                    ParentContainerCode = contenedores.FirstOrDefault(p => p.Id == c.ParentContainerId)?.ContainerCode,

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

            var totals = await GetTransactionTotalsAsync(cefTransactionId);

            vm.TotalDeclaredAll = totals.DeclaredCash;
            vm.TotalCountedAll = totals.CashTotal;
            vm.DifferenceAll = totals.Difference;

            vm.TotalOverallAll = totals.OverallTotal;
            vm.CountedBillsAll = totals.BillTotal;
            vm.CountedBillHighAll = totals.BillHigh;
            vm.CountedBillLowAll = totals.BillLow;
            vm.CountedCoinsAll = totals.CoinTotal;
            vm.CountedDocsAll = totals.DocTotal;
            vm.CountedChecksAll = totals.CheckTotal;

            return vm;
        }

        /// <inheritdoc/>
        public async Task<CefContainer> SaveContainerAndDetailsAsync(CefContainerProcessingViewModel viewModel, string processingUserId)
        {
            var transaction = await _context.CefTransactions
                .FirstOrDefaultAsync(t => t.Id == viewModel.CefTransactionId)
                ?? throw new InvalidOperationException($"La transacción CEF con ID {viewModel.CefTransactionId} no existe.");

            if (transaction.TransactionStatus != CefTransactionStatusEnum.EncoladoParaConteo.ToString() &&
                transaction.TransactionStatus != CefTransactionStatusEnum.ConteoBilletes.ToString() &&
                transaction.TransactionStatus != CefTransactionStatusEnum.ConteoMonedas.ToString() &&
                transaction.TransactionStatus != CefTransactionStatusEnum.ConteoCheques.ToString() &&
                transaction.TransactionStatus != CefTransactionStatusEnum.ConteoDocumentos.ToString())
            {
                throw new InvalidOperationException($"La transacción {transaction.Id} no está en un estado válido para procesar contenedores.");
            }

            // 1) Validaciones de sobre
            var isSobre = viewModel.ContainerType == CefContainerTypeEnum.Sobre;
            if (isSobre)
            {
                if (viewModel.ParentContainerId == null)
                    throw new InvalidOperationException("Los sobres deben tener una bolsa padre.");
                if (viewModel.EnvelopeSubType == null)
                    throw new InvalidOperationException("Debe seleccionar el tipo de sobre (Efectivo / Documento / Cheque).");
            }
            else
            {
                viewModel.ParentContainerId = null;
                viewModel.EnvelopeSubType = null;
            }

            var isDoc = isSobre && viewModel.EnvelopeSubType == CefEnvelopeSubTypeEnum.Documento;
            var isCheck = isSobre && viewModel.EnvelopeSubType == CefEnvelopeSubTypeEnum.Cheque;

            // Documento/Voucher: cajero requerido y 1 detalle con monto (UnitValue) > 0
            if (isDoc)
            {
                if (string.IsNullOrWhiteSpace(viewModel.ClientCashierName))
                    throw new InvalidOperationException("Debe indicar el NOMBRE del cajero que contó en el origen.");
                if (viewModel.ClientCashierId == null)
                    throw new InvalidOperationException("Debe indicar el DOCUMENTO del cajero que contó en el origen.");

                if (viewModel.ValueDetails == null || viewModel.ValueDetails.Count == 0)
                    throw new InvalidOperationException("Documento/Voucher requiere un detalle con monto.");
                if (viewModel.ValueDetails.Count > 1)
                    throw new InvalidOperationException("Documento/Voucher debe tener exactamente un detalle.");

                var only = viewModel.ValueDetails[0];
                if ((only.UnitValue ?? 0m) <= 0)
                    throw new InvalidOperationException("El monto del Documento/Voucher debe ser mayor a 0.");
                if (only.ValueType != CefValueTypeEnum.Documento)
                    throw new InvalidOperationException("El detalle de Documento/Voucher debe ser de tipo Documento.");
            }

            // 2) Normalizar detalles
            var detailsVms = viewModel.ValueDetails ?? new List<CefValueDetailViewModel>();

            // Documento/Voucher: garantizar exactamente 1 detalle (si no hay, crear; si hay >1, error)
            if (isDoc)
            {
                if (detailsVms.Count == 0)
                {
                    detailsVms = new List<CefValueDetailViewModel>
            {
                new CefValueDetailViewModel
                {
                    ValueType = CefValueTypeEnum.Documento,
                    Quantity  = 1
                }
            };
                }
                else if (detailsVms.Count > 1)
                {
                    throw new InvalidOperationException("El sobre de Documento/Voucher debe tener exactamente un detalle.");
                }
            }

            // Duplicados solo aplican a Billete/Moneda
            var dupPairs = detailsVms
                .Where(d => d.ValueType == CefValueTypeEnum.Billete || d.ValueType == CefValueTypeEnum.Moneda)
                .Where(d => d.DenominationId != null && d.QualityId != null)
                .GroupBy(d => new { d.DenominationId, d.QualityId })
                .Where(g => g.Count() > 1)
                .ToList();

            if (dupPairs.Any())
                throw new InvalidOperationException("Hay filas repetidas con la misma combinación Denominación + Calidad. Verifique los detalles.");

            // 3) Crear o cargar contenedor
            CefContainer container;
            if (viewModel.Id == 0)
            {
                container = new CefContainer
                {
                    CefTransactionId = viewModel.CefTransactionId,
                    ParentContainerId = viewModel.ParentContainerId,
                    ContainerType = viewModel.ContainerType.ToString(),
                    EnvelopeSubType = isSobre ? viewModel.EnvelopeSubType?.ToString() : null,
                    ContainerCode = viewModel.ContainerCode,
                    ContainerStatus = CefContainerStatusEnum.InProcess.ToString(),
                    Observations = viewModel.Observations,
                    ProcessingUserId = processingUserId,
                    ProcessingDate = DateTime.Now,
                    ClientCashierId = viewModel.ClientCashierId,
                    ClientCashierName = viewModel.ClientCashierName,
                    ClientEnvelopeDate = viewModel.ClientEnvelopeDate
                };

                await _context.CefContainers.AddAsync(container);
                await _context.SaveChangesAsync();
            }
            else
            {
                container = await _context.CefContainers
                    .Include(c => c.ValueDetails)
                    .FirstOrDefaultAsync(c => c.Id == viewModel.Id)
                    ?? throw new InvalidOperationException($"Contenedor con ID {viewModel.Id} no encontrado.");

                container.ContainerType = viewModel.ContainerType.ToString();
                container.EnvelopeSubType = isSobre ? viewModel.EnvelopeSubType?.ToString() : null;
                container.ContainerCode = viewModel.ContainerCode;
                container.Observations = viewModel.Observations;
                container.ClientCashierId = viewModel.ClientCashierId;
                container.ClientCashierName = viewModel.ClientCashierName;
                container.ClientEnvelopeDate = viewModel.ClientEnvelopeDate;

                _context.CefContainers.Update(container);
            }

            // 4) Sincronizar detalles
            var existingDetails = container.ValueDetails?.ToList() ?? new List<CefValueDetail>();
            var incomingDetailIds = detailsVms.Where(d => d.Id != 0).Select(d => d.Id).ToHashSet();

            var detailsToDelete = existingDetails.Where(d => !incomingDetailIds.Contains(d.Id)).ToList();
            if (detailsToDelete.Count > 0)
                _context.CefValueDetails.RemoveRange(detailsToDelete);

            decimal countedTotal = 0m;

            foreach (var detailVm in detailsVms)
            {
                if (isSobre && !SobreDetalleEsValido(viewModel.EnvelopeSubType, detailVm.ValueType))
                    throw new InvalidOperationException($"Detalle inválido para sobre {viewModel.EnvelopeSubType}: {detailVm.ValueType}.");

                // Billete/Moneda: requiere Denominación
                if (detailVm.ValueType == CefValueTypeEnum.Billete || detailVm.ValueType == CefValueTypeEnum.Moneda)
                {
                    if (detailVm.DenominationId == null)
                        throw new InvalidOperationException("Debe seleccionar una denominación para Billete/Moneda.");
                }

                switch (detailVm.ValueType)
                {
                    case CefValueTypeEnum.Documento:
                        {
                            // Documento: NO se multiplica, CalculatedAmount = UnitValue
                            decimal unitOrAmount = detailVm.UnitValue ?? 0m;
                            if (unitOrAmount <= 0m)
                                throw new InvalidOperationException("Para Documento/Voucher debe indicar el monto (> 0).");

                            int qty = detailVm.Quantity ?? 0; // respeta lo que venga; NOT NULL en DB

                            var existing = existingDetails.FirstOrDefault(d => d.Id == detailVm.Id);
                            if (existing != null)
                            {
                                existing.ValueType = CefValueTypeEnum.Documento.ToString();
                                existing.Quantity = qty;
                                existing.DenominationId = null;
                                existing.BundlesCount = null;
                                existing.LoosePiecesCount = null;
                                existing.UnitValue = unitOrAmount;
                                existing.QualityId = null;
                                existing.EntitieBankId = detailVm.EntitieBankId;
                                existing.AccountNumber = detailVm.AccountNumber;
                                existing.CheckNumber = detailVm.CheckNumber;
                                existing.IssueDate = detailVm.IssueDate;
                                existing.Observations = detailVm.Observations;
                                existing.CalculatedAmount = unitOrAmount;

                                _context.CefValueDetails.Update(existing);
                            }
                            else
                            {
                                var newDetail = new CefValueDetail
                                {
                                    CefContainerId = container.Id,
                                    ValueType = CefValueTypeEnum.Documento.ToString(),
                                    Quantity = qty,
                                    DenominationId = null,
                                    BundlesCount = null,
                                    LoosePiecesCount = null,
                                    UnitValue = unitOrAmount,
                                    QualityId = null,
                                    EntitieBankId = detailVm.EntitieBankId,
                                    AccountNumber = detailVm.AccountNumber,
                                    CheckNumber = detailVm.CheckNumber,
                                    IssueDate = detailVm.IssueDate,
                                    Observations = detailVm.Observations,
                                    CalculatedAmount = unitOrAmount
                                };
                                await _context.CefValueDetails.AddAsync(newDetail);
                            }

                            countedTotal += unitOrAmount;
                            continue;
                        }

                    case CefValueTypeEnum.Cheque:
                        {
                            // Cheque: total = UnitValue * Qty (Qty mínimo 1)
                            int qty = detailVm.Quantity ?? 1;
                            if (qty <= 0) qty = 1;

                            decimal unit = detailVm.UnitValue ?? 0m;
                            if (unit <= 0m)
                                throw new InvalidOperationException("Para Cheque debe indicar el valor (> 0).");

                            decimal amt = unit * qty;

                            var existing = existingDetails.FirstOrDefault(d => d.Id == detailVm.Id);
                            if (existing != null)
                            {
                                existing.ValueType = CefValueTypeEnum.Cheque.ToString();
                                existing.DenominationId = null;
                                existing.Quantity = qty;
                                existing.BundlesCount = null;
                                existing.LoosePiecesCount = null;
                                existing.UnitValue = unit;
                                existing.IsHighDenomination = false;
                                existing.QualityId = null;
                                existing.EntitieBankId = detailVm.EntitieBankId;
                                existing.AccountNumber = detailVm.AccountNumber;
                                existing.CheckNumber = detailVm.CheckNumber;
                                existing.IssueDate = detailVm.IssueDate;
                                existing.Observations = detailVm.Observations;
                                existing.CalculatedAmount = amt;

                                _context.CefValueDetails.Update(existing);
                            }
                            else
                            {
                                var newDetail = new CefValueDetail
                                {
                                    CefContainerId = container.Id,
                                    ValueType = CefValueTypeEnum.Cheque.ToString(),
                                    DenominationId = null,
                                    Quantity = qty,
                                    BundlesCount = null,
                                    LoosePiecesCount = null,
                                    UnitValue = unit,
                                    IsHighDenomination = false,
                                    QualityId = null,
                                    EntitieBankId = detailVm.EntitieBankId,
                                    AccountNumber = detailVm.AccountNumber,
                                    CheckNumber = detailVm.CheckNumber,
                                    IssueDate = detailVm.IssueDate,
                                    Observations = detailVm.Observations,
                                    CalculatedAmount = amt
                                };
                                await _context.CefValueDetails.AddAsync(newDetail);
                            }

                            countedTotal += amt;
                            continue;
                        }

                    default:
                        {
                            // Billete / Moneda (upsert estándar con cálculo en servidor)
                            var existingStd = existingDetails.FirstOrDefault(d => d.Id == detailVm.Id);
                            if (existingStd != null)
                            {
                                existingStd.ValueType = detailVm.ValueType.ToString();
                                existingStd.DenominationId = detailVm.DenominationId;
                                existingStd.Quantity = detailVm.Quantity ?? 0;   // NOT NULL
                                existingStd.BundlesCount = detailVm.BundlesCount;
                                existingStd.LoosePiecesCount = detailVm.LoosePiecesCount;
                                existingStd.UnitValue = detailVm.UnitValue;
                                existingStd.IsHighDenomination = detailVm.IsHighDenomination;
                                existingStd.EntitieBankId = detailVm.EntitieBankId;
                                existingStd.AccountNumber = detailVm.AccountNumber;
                                existingStd.CheckNumber = detailVm.CheckNumber;
                                existingStd.IssueDate = detailVm.IssueDate;
                                existingStd.Observations = detailVm.Observations;
                                existingStd.QualityId = detailVm.QualityId;

                                existingStd.CalculatedAmount = await CalcularMontoServidorAsync(existingStd);
                                countedTotal += existingStd.CalculatedAmount ?? 0;

                                _context.CefValueDetails.Update(existingStd);
                            }
                            else
                            {
                                var newDetailStd = new CefValueDetail
                                {
                                    CefContainerId = container.Id,
                                    ValueType = detailVm.ValueType.ToString(),
                                    DenominationId = detailVm.DenominationId,
                                    Quantity = detailVm.Quantity ?? 0,  // NOT NULL
                                    BundlesCount = detailVm.BundlesCount,
                                    LoosePiecesCount = detailVm.LoosePiecesCount,
                                    UnitValue = detailVm.UnitValue,
                                    IsHighDenomination = detailVm.IsHighDenomination,
                                    EntitieBankId = detailVm.EntitieBankId,
                                    AccountNumber = detailVm.AccountNumber,
                                    CheckNumber = detailVm.CheckNumber,
                                    IssueDate = detailVm.IssueDate,
                                    Observations = detailVm.Observations,
                                    QualityId = detailVm.QualityId
                                };

                                newDetailStd.CalculatedAmount = await CalcularMontoServidorAsync(newDetailStd);
                                countedTotal += newDetailStd.CalculatedAmount ?? 0; // si la propiedad es no-nullable

                                await _context.CefValueDetails.AddAsync(newDetailStd);
                            }

                            continue;
                        }
                }
            }


            // 5) Actualizar contenedor
            container.CountedValue = countedTotal;

            if ((container.ContainerStatus == CefContainerStatusEnum.InProcess.ToString() ||
                 container.ContainerStatus == CefContainerStatusEnum.Pending.ToString()) &&
                countedTotal > 0)
            {
                container.ContainerStatus = CefContainerStatusEnum.Counted.ToString();
                container.ProcessingDate = DateTime.Now;
            }

            _context.CefContainers.Update(container);
            await _context.SaveChangesAsync();

            // 6) Avanzar estado de la transacción
            if (transaction.TransactionStatus == CefTransactionStatusEnum.EncoladoParaConteo.ToString())
            {
                var hasBillsOrCoins = detailsVms.Any(d => d.ValueType == CefValueTypeEnum.Billete || d.ValueType == CefValueTypeEnum.Moneda);
                var hasChecks = detailsVms.Any(d => d.ValueType == CefValueTypeEnum.Cheque);
                var hasDocs = detailsVms.Any(d => d.ValueType == CefValueTypeEnum.Documento);

                if (hasBillsOrCoins)
                    transaction.TransactionStatus = CefTransactionStatusEnum.ConteoBilletes.ToString();
                else if (hasChecks)
                    transaction.TransactionStatus = CefTransactionStatusEnum.ConteoCheques.ToString();
                else if (hasDocs)
                    transaction.TransactionStatus = CefTransactionStatusEnum.ConteoDocumentos.ToString();

                if (transaction.TransactionStatus != CefTransactionStatusEnum.EncoladoParaConteo.ToString())
                {
                    _context.CefTransactions.Update(transaction);
                    await _context.SaveChangesAsync();
                }
            }

            var totals = await GetTransactionTotalsAsync(transaction.Id);

            transaction.CountedBillHighValue = totals.BillHigh;
            transaction.CountedBillLowValue = totals.BillLow;
            transaction.CountedBillValue = totals.BillTotal;
            transaction.CountedCoinValue = totals.CoinTotal;

            transaction.TotalCountedValue = totals.CashTotal;
            transaction.ValueDifference = totals.Difference;

            transaction.CountedCheckValue = totals.CheckTotal;
            transaction.CountedDocumentValue = totals.DocTotal;
            transaction.OverallCountedValue = totals.OverallTotal;

            transaction.TotalCountedValueInWords = AmountInWordsHelper.ToSpanishCurrency(transaction.TotalCountedValue, transaction.Currency);
            transaction.TotalDeclaredValueInWords = AmountInWordsHelper.ToSpanishCurrency(transaction.TotalDeclaredValue, transaction.Currency);
            transaction.OverallCountedValueInWords = AmountInWordsHelper.ToSpanishCurrency(transaction.OverallCountedValue, transaction.Currency);

            transaction.LastUpdateDate = DateTime.Now;
            transaction.LastUpdateUser = processingUserId;

            _context.CefTransactions.Update(transaction);
            await _context.SaveChangesAsync();

            return container;
        }

        public async Task<bool> DeleteContainerAsync(int transactionId, int containerId)
        {
            // Evita intento de borrar un "padre" con hijos (tu FK ParentContainerId está en RESTRICT)
            var hasChildren = await _context.CefContainers
                .AnyAsync(c => c.ParentContainerId == containerId);
            if (hasChildren)
                throw new InvalidOperationException("No se puede eliminar un contenedor padre porque tiene sobres hijos.");

            var rows = await _context.CefContainers
                .Where(c => c.Id == containerId && c.CefTransactionId == transactionId)
                .ExecuteDeleteAsync();

            if (rows > 0)
            {
                var tx = await _context.CefTransactions.FirstOrDefaultAsync(t => t.Id == transactionId);
                if (tx != null)
                {
                    var totals = await GetTransactionTotalsAsync(transactionId);

                    tx.CountedBillHighValue = totals.BillHigh;
                    tx.CountedBillLowValue = totals.BillLow;
                    tx.CountedBillValue = totals.BillTotal;
                    tx.CountedCoinValue = totals.CoinTotal;

                    tx.TotalCountedValue = totals.CashTotal;
                    tx.ValueDifference = totals.Difference;

                    tx.CountedCheckValue = totals.CheckTotal;
                    tx.CountedDocumentValue = totals.DocTotal;
                    tx.OverallCountedValue = totals.OverallTotal;

                    tx.TotalDeclaredValueInWords = AmountInWordsHelper.ToSpanishCurrency(tx.TotalCountedValue, tx.Currency);
                    tx.TotalCountedValueInWords = AmountInWordsHelper.ToSpanishCurrency(tx.TotalCountedValue, tx.Currency);
                    tx.OverallCountedValueInWords = AmountInWordsHelper.ToSpanishCurrency(tx.OverallCountedValue, tx.Currency);

                    tx.LastUpdateDate = DateTime.Now;

                    _context.CefTransactions.Update(tx);
                    await _context.SaveChangesAsync();
                }
            }

            return rows > 0;
        }

        private static bool SobreDetalleEsValido(CefEnvelopeSubTypeEnum? subType, CefValueTypeEnum valueType)
        {
            if (subType == null) return false;

            return subType switch
            {
                CefEnvelopeSubTypeEnum.Efectivo => (valueType == CefValueTypeEnum.Billete || valueType == CefValueTypeEnum.Moneda),
                CefEnvelopeSubTypeEnum.Documento => (valueType == CefValueTypeEnum.Documento),
                CefEnvelopeSubTypeEnum.Cheque => (valueType == CefValueTypeEnum.Cheque),
                _ => false
            };
        }

        private async Task<decimal> CalcularMontoServidorAsync(CefValueDetail d)
        {
            var qty = (decimal)(d.Quantity ?? 0);

            if ((d.UnitValue ?? 0) > 0)
                return qty * (d.UnitValue ?? 0);

            if (d.DenominationId is int denomId && denomId > 0)
            {
                var denomVal = await _context.AdmDenominaciones
                    .Where(a => a.CodDenominacion == denomId)
                    .Select(a => a.ValorDenominacion)
                    .FirstOrDefaultAsync();

                return (denomVal ?? 0m) * qty;
            }

            return 0m;
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
            var tx = await _context.CefTransactions
                .AsNoTracking()
                .FirstAsync(t => t.Id == transactionId);

            var conceptType = await (
                from s in _context.CgsServicios.AsNoTracking()
                join c in _context.AdmConceptos.AsNoTracking() on s.ConceptCode equals c.CodConcepto
                where s.ServiceOrderId == tx.ServiceOrderId
                select c.TipoConcepto
            ).FirstOrDefaultAsync();

            bool isCollection = conceptType == "RC" || conceptType == "ET";
            var declaredCash = isCollection
                ? (tx.TotalDeclaredValue)
                : (tx.DeclaredBillValue + tx.DeclaredCoinValue);

            var details = await (
                from d in _context.CefValueDetails.AsNoTracking()
                join c in _context.CefContainers.AsNoTracking() on d.CefContainerId equals c.Id
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

        /// <inheritdoc/>
        public async Task<CefContainer?> GetContainerWithDetailsAsync(int containerId)
        {
            return await _context.CefContainers
                                 .Include(c => c.ValueDetails)
                                    .ThenInclude(vd => vd.Incidents)
                                 .Include(c => c.Incidents)
                                 .Include(c => c.ChildContainers)
                                 .FirstOrDefaultAsync(c => c.Id == containerId);
        }

        /// <inheritdoc/>
        public async Task<List<CefContainer>> GetContainersByTransactionIdAsync(int transactionId)
        {
            return await _context.CefContainers
                                 .Where(c => c.CefTransactionId == transactionId)
                                 .Include(c => c.ValueDetails)
                                     .ThenInclude(vd => vd.Incidents)
                                 .Include(c => c.Incidents)
                                 .Include(c => c.ChildContainers)
                                 .OrderBy(c => c.Id)
                                 .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<string> BuildDenomsJsonForTransactionAsync(int cefTransactionId)
        {
            var tx = await _context.CefTransactions
                .AsNoTracking()
                .Where(t => t.Id == cefTransactionId)
                .Select(t => new { t.Currency })
                .FirstOrDefaultAsync()
                ?? throw new InvalidOperationException($"Transacción {cefTransactionId} no existe.");

            var currency = tx.Currency ?? "COP";

            var denoms = await _context.AdmDenominaciones
                .AsNoTracking()
                .Where(d => d.DivisaDenominacion == currency)
                .Select(d => new
                {
                    id = d.CodDenominacion,
                    value = d.ValorDenominacion,
                    money = d.TipoDinero,
                    family = d.FamiliaDenominacion,
                    label = d.Denominacion,
                    bundleSize = d.CantidadUnidadAgrupamiento,
                    isHigh = d.AltaDenominacion
                })
                .OrderBy(d => d.money)
                .ThenByDescending(d => d.value)
                .ToListAsync();

            var esCO = CultureInfo.GetCultureInfo("es-CO");

            // Función local para normalizar y formatear
            string FormatLabel(string lbl, decimal? val) =>
                !string.IsNullOrWhiteSpace(lbl) ? lbl : (val.HasValue ? val.Value.ToString("C0", esCO) : "");

            string NormFam(string fam) =>
                string.IsNullOrWhiteSpace(fam) ? "T" : fam.Trim().ToUpperInvariant();

            int DefaultBundle(string money) =>
                money == "B" ? 100 : (money == "M" ? 1000 : 1);

            var payload = new
            {
                Billete = denoms
                    .Where(d => d.money == "B")
                    .Select(d => new
                    {
                        id = d.id,
                        value = d.value,
                        label = FormatLabel(d.label, d.value),
                        bundleSize = d.bundleSize ?? DefaultBundle(d.money),
                        family = NormFam(d.family),
                        isHigh = d.isHigh
                    }),
                Moneda = denoms
                    .Where(d => d.money == "M")
                    .Select(d => new
                    {
                        id = d.id,
                        value = d.value,
                        label = FormatLabel(d.label, d.value),
                        bundleSize = d.bundleSize ?? DefaultBundle(d.money),
                        family = NormFam(d.family),
                        isHigh = false
                    }),
                Documento = denoms
                    .Where(d => d.money == "D")
                    .Select(d => new
                    {
                        id = d.id,
                        value = d.value,
                        label = FormatLabel(d.label, d.value),
                        bundleSize = d.bundleSize ?? DefaultBundle(d.money),
                        family = "T"
                    }),
                Cheque = denoms
                    .Where(d => d.money == "C")
                    .Select(d => new
                    {
                        id = d.id,
                        value = d.value,
                        label = FormatLabel(d.label, d.value),
                        bundleSize = d.bundleSize ?? DefaultBundle(d.money),
                        family = "T" // cheques no discriminan familia
                    })
            };

            return JsonSerializer.Serialize(payload);
        }

        /// <inheritdoc/>
        public async Task<string> BuildQualitiesJsonAsync()
        {
            var q = await _context.Set<AdmQuality>()
                .Where(c => c.Status)
                .OrderBy(c => c.TypeOfMoney).ThenBy(c => c.QualityName)
                .Select(c => new {
                    id = c.Id,
                    name = c.QualityName,
                    money = c.TypeOfMoney,
                    family = c.DenominationFamily
                })
                .ToListAsync();

            var obj = new
            {
                B = q.Where(x => x.money == "B").ToList(),
                M = q.Where(x => x.money == "M").ToList()
            };
            return JsonSerializer.Serialize(obj);
        }

        /// <inheritdoc/>
        public async Task<string> BuildBankEntitiesJsonAsync()
        {
            var data = await _context.AdmBankEntities
                .AsNoTracking()
                .OrderBy(b => b.Name)
                .Select(b => new { value = b.Id, text = b.Name })
                .ToListAsync();

            return JsonSerializer.Serialize(data);
        }

        // Puedes moverlo al final de la clase si prefieres.
        private sealed record DeclaredBreakdown(
            int BagCount,
            int EnvelopeCount,
            int CheckCount,
            int DocumentCount,
            decimal BillDeclared,
            decimal CoinDeclared,
            decimal DocDeclared
        );

        // NOTA: Este breakdown NO se debe recalcular en el flujo de proceso/conteo.
        // Úsese solo en el flujo donde el usuario declara valores (check-in).
        /*private async Task<DeclaredBreakdown> ComputeDeclaredBreakdownAsync(int transactionId)
        {
            // Conteos de contenedores
            var bagCount = await _context.CefContainers
                .CountAsync(c => c.CefTransactionId == transactionId &&
                                 c.ContainerType == CefContainerTypeEnum.Bolsa.ToString());

            var envelopeCount = await _context.CefContainers
                .CountAsync(c => c.CefTransactionId == transactionId &&
                                 c.ContainerType == CefContainerTypeEnum.Sobre.ToString());

            // Proyección de detalles por transacción (join por Container)
            var details = from vd in _context.CefValueDetails
                          join c in _context.CefContainers on vd.CefContainerId equals c.Id
                          where c.CefTransactionId == transactionId
                          select vd;

            // Cantidades (usa Quantity por si documentos/cheques traen más de 1)
            var checkCount = await details
                .Where(vd => vd.ValueType == CefValueTypeEnum.Cheque.ToString())
                .SumAsync(vd => (int?)vd.Quantity) ?? 0;

            var documentCount = await details
                .Where(vd => vd.ValueType == CefValueTypeEnum.Documento.ToString())
                .SumAsync(vd => (int?)vd.Quantity) ?? 0;

            // Valores declarados por tipo (tomados de CalculatedAmount)
            var billDeclared = await details
                .Where(vd => vd.ValueType == CefValueTypeEnum.Billete.ToString())
                .SumAsync(vd => (decimal?)(vd.CalculatedAmount ?? 0m)) ?? 0m;

            var coinDeclared = await details
                .Where(vd => vd.ValueType == CefValueTypeEnum.Moneda.ToString())
                .SumAsync(vd => (decimal?)(vd.CalculatedAmount ?? 0m)) ?? 0m;

            var docDeclared = await details
                .Where(vd => vd.ValueType == CefValueTypeEnum.Documento.ToString())
                .SumAsync(vd => (decimal?)(vd.CalculatedAmount ?? 0m)) ?? 0m;

            return new DeclaredBreakdown(
                bagCount,
                envelopeCount,
                checkCount,
                documentCount,
                billDeclared,
                coinDeclared,
                docDeclared
            );
        }*/
    }
}