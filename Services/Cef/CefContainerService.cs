using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.CentroEfectivo;

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
                    DeclaredValue = c.DeclaredValue,
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
                        IdentifierNumber = d.IdentifierNumber,
                        BankName = d.BankName,
                        IssueDate = d.IssueDate,
                        Issuer = d.Issuer,
                        Observations = d.Observations,
                        QualityId = d.QualityId,
                        ValueType = Enum.TryParse<CefValueTypeEnum>(d.ValueType, out var tipoValor) ? tipoValor : CefValueTypeEnum.Billete,
                    }).ToList()
                }).ToList();
            }

            return vm;
        }

        /// <inheritdoc/>
        public async Task<CefContainer> SaveContainerAndDetailsAsync(
            CefContainerProcessingViewModel viewModel,
            string processingUserId)
        {
            // 0) Cargar transacción + validar estado
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

            // 1) Validaciones de sobre/bolsa
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

            // 2) Normalizar detalles (evita NullReference) + validar duplicados
            var detailsVms = viewModel.ValueDetails ?? new List<CefValueDetailViewModel>();

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
                    DeclaredValue = viewModel.DeclaredValue,
                    ContainerStatus = CefContainerStatusEnum.InProcess.ToString(),
                    Observations = viewModel.Observations,
                    ProcessingUserId = processingUserId,
                    ProcessingDate = DateTime.Now,
                    ClientCashierId = viewModel.ClientCashierId,
                    ClientCashierName = viewModel.ClientCashierName,
                    ClientEnvelopeDate = viewModel.ClientEnvelopeDate
                };

                await _context.CefContainers.AddAsync(container);
                // Guardar aquí para obtener Id y poder asociar detalles nuevos
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
                container.DeclaredValue = viewModel.DeclaredValue;
                container.Observations = viewModel.Observations;
                container.ClientCashierId = viewModel.ClientCashierId;
                container.ClientCashierName = viewModel.ClientCashierName;
                container.ClientEnvelopeDate = viewModel.ClientEnvelopeDate;

                _context.CefContainers.Update(container);
            }

            // 4) Sincronizar detalles (delete/update/insert)
            var existingDetails = container.ValueDetails?.ToList() ?? new List<CefValueDetail>();
            var incomingDetailIds = detailsVms
                .Where(d => d.Id != 0)
                .Select(d => d.Id)
                .ToHashSet();

            // 4.1) Eliminar detalles que ya no vienen en el POST
            var detailsToDelete = existingDetails
                .Where(d => !incomingDetailIds.Contains(d.Id))
                .ToList();

            if (detailsToDelete.Count > 0)
                _context.CefValueDetails.RemoveRange(detailsToDelete);

            // 4.2) Upsert de detalles + recálculo del total contado
            decimal countedTotal = 0m;

            foreach (var detailVm in detailsVms)
            {
                if (isSobre && !SobreDetalleEsValido(viewModel.EnvelopeSubType, detailVm.ValueType))
                    throw new InvalidOperationException($"Detalle inválido para sobre {viewModel.EnvelopeSubType}: {detailVm.ValueType}.");

                if (detailVm.ValueType == CefValueTypeEnum.Billete || detailVm.ValueType == CefValueTypeEnum.Moneda)
                {
                    if (detailVm.DenominationId == null)
                        throw new InvalidOperationException("Debe seleccionar una denominación para Billete/Moneda.");
                }
                if (detailVm.ValueType == CefValueTypeEnum.Documento || detailVm.ValueType == CefValueTypeEnum.Cheque)
                {
                    if ((detailVm.UnitValue ?? 0) <= 0)
                        throw new InvalidOperationException("Para Documento/Cheque debe indicar el valor unitario.");
                }

                var existing = existingDetails.FirstOrDefault(d => d.Id == detailVm.Id);

                if (existing != null)
                {
                    existing.ValueType = detailVm.ValueType.ToString();
                    existing.DenominationId = detailVm.DenominationId;
                    existing.Quantity = detailVm.Quantity;
                    existing.BundlesCount = detailVm.BundlesCount;
                    existing.LoosePiecesCount = detailVm.LoosePiecesCount;
                    existing.UnitValue = detailVm.UnitValue;
                    existing.IsHighDenomination = detailVm.IsHighDenomination;
                    existing.IdentifierNumber = detailVm.IdentifierNumber;
                    existing.BankName = detailVm.BankName;
                    existing.IssueDate = detailVm.IssueDate;
                    existing.Issuer = detailVm.Issuer;
                    existing.Observations = detailVm.Observations;
                    existing.QualityId = detailVm.QualityId;

                    existing.CalculatedAmount = await CalcularMontoServidorAsync(existing);
                    countedTotal += existing.CalculatedAmount ?? 0;

                    _context.CefValueDetails.Update(existing);
                }
                else
                {
                    var newDetail = new CefValueDetail
                    {
                        CefContainerId = container.Id,
                        ValueType = detailVm.ValueType.ToString(),
                        DenominationId = detailVm.DenominationId,
                        Quantity = detailVm.Quantity,
                        BundlesCount = detailVm.BundlesCount,
                        LoosePiecesCount = detailVm.LoosePiecesCount,
                        UnitValue = detailVm.UnitValue,
                        IsHighDenomination = detailVm.IsHighDenomination,
                        IdentifierNumber = detailVm.IdentifierNumber,
                        BankName = detailVm.BankName,
                        IssueDate = detailVm.IssueDate,
                        Issuer = detailVm.Issuer,
                        Observations = detailVm.Observations,
                        QualityId = detailVm.QualityId
                    };

                    newDetail.CalculatedAmount = await CalcularMontoServidorAsync(newDetail);
                    countedTotal += newDetail.CalculatedAmount ?? 0;

                    await _context.CefValueDetails.AddAsync(newDetail);
                }
            }

            container.CountedValue = countedTotal;

            // Si ya se contó algo, pasar a Counted
            if ((container.ContainerStatus == CefContainerStatusEnum.InProcess.ToString() ||
                 container.ContainerStatus == CefContainerStatusEnum.Pending.ToString()) &&
                countedTotal > 0)
            {
                container.ContainerStatus = CefContainerStatusEnum.Counted.ToString();
                container.ProcessingDate = DateTime.Now;
            }

            _context.CefContainers.Update(container);
            await _context.SaveChangesAsync();

            // 5) Avanzar estado de la transacción según lo cargado
            if (transaction.TransactionStatus == CefTransactionStatusEnum.EncoladoParaConteo.ToString())
            {
                // Dependiendo de los detalles ingresados, escoge el primer estado de conteo correspondiente
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

            return container;
        }

        public async Task<bool> DeleteContainerAsync(int transactionId, int containerId)
        {
            // Evita intento de borrar un "padre" con hijos (tu FK ParentContainerId está en RESTRICT)
            var hasChildren = await _context.CefContainers
                .AnyAsync(c => c.ParentContainerId == containerId);
            if (hasChildren)
                throw new InvalidOperationException("No se puede eliminar un contenedor padre porque tiene sobres hijos.");

            // Borra el contenedor; los detalles se eliminan por CASCADE en la BD
            var rows = await _context.CefContainers
                .Where(c => c.Id == containerId && c.CefTransactionId == transactionId)
                .ExecuteDeleteAsync();

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

        public async Task<(decimal declared, decimal counted, decimal diff)> GetTransactionTotalsAsync(int transactionId)
        {
            var declared = await _context.CefContainers
                .Where(c => c.CefTransactionId == transactionId)
                .Select(c => (decimal?)c.DeclaredValue)
                .SumAsync() ?? 0m;

            var counted = await _context.CefContainers
                .Where(c => c.CefTransactionId == transactionId)
                .Select(c => (decimal?)c.CountedValue)
                .SumAsync() ?? 0m;

            var diff = counted - declared;
            return (declared, counted, diff);
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
                    bundleSize = d.CantidadUnidadAgrupamiento
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
                        family = NormFam(d.family) // <- MUY IMPORTANTE
                    }),
                Moneda = denoms
                    .Where(d => d.money == "M")
                    .Select(d => new
                    {
                        id = d.id,
                        value = d.value,
                        label = FormatLabel(d.label, d.value),
                        bundleSize = d.bundleSize ?? DefaultBundle(d.money),
                        family = NormFam(d.family) // <- MUY IMPORTANTE
                    }),
                Documento = denoms
                    .Where(d => d.money == "D")
                    .Select(d => new
                    {
                        id = d.id,
                        value = d.value,
                        label = FormatLabel(d.label, d.value),
                        bundleSize = d.bundleSize ?? DefaultBundle(d.money),
                        family = "T" // documentos no discriminan familia
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
    }
}