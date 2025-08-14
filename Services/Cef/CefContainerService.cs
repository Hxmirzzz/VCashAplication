using Microsoft.EntityFrameworkCore;
using System.Globalization;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.CentroEfectivo;
using System.Text.Json;

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

            // Inicializa con 1 contenedor por defecto si vienes “en blanco”
            // (No tocamos tu CefContainerProcessingViewModel)
            vm.Containers = new List<CefContainerProcessingViewModel>
            {
                new CefContainerProcessingViewModel
                {
                    CefTransactionId = cefTransactionId,
                    ContainerType = CefContainerTypeEnum.Bolsa
                }
            };

            return vm;
        }

        /// <inheritdoc/>
        public async Task<CefContainer> SaveContainerAndDetailsAsync(CefContainerProcessingViewModel viewModel, string processingUserId)
        {
            var transaction = await _context.CefTransactions.FirstOrDefaultAsync(t => t.Id == viewModel.CefTransactionId);
            if (transaction == null)
            {
                throw new InvalidOperationException($"La transacción CEF con ID {viewModel.CefTransactionId} no existe.");
            }
            // Validar estado de la transacción para permitir guardar contenedores
            if (transaction.TransactionStatus != CefTransactionStatusEnum.EnqueuedForCounting.ToString() &&
                transaction.TransactionStatus != CefTransactionStatusEnum.BillCounting.ToString() &&
                transaction.TransactionStatus != CefTransactionStatusEnum.CoinCounting.ToString() &&
                transaction.TransactionStatus != CefTransactionStatusEnum.CheckCounting.ToString() &&
                transaction.TransactionStatus != CefTransactionStatusEnum.DocumentCounting.ToString())
            {
                throw new InvalidOperationException($"La transacción {transaction.Id} no está en un estado válido para procesar contenedores.");
            }

            CefContainer container;
            if (viewModel.Id == 0)
            {
                container = new CefContainer
                {
                    CefTransactionId = viewModel.CefTransactionId,
                    ParentContainerId = viewModel.ParentContainerId,
                    ContainerType = viewModel.ContainerType.ToString(),
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
            }
            else
            {
                container = await _context.CefContainers
                                          .Include(c => c.ValueDetails)
                                          .FirstOrDefaultAsync(c => c.Id == viewModel.Id) ?? throw new InvalidOperationException($"Contenedor con ID {viewModel.Id} no encontrado.");

                container.ContainerType = viewModel.ContainerType.ToString();
                container.ContainerCode = viewModel.ContainerCode;
                container.DeclaredValue = viewModel.DeclaredValue;
                container.Observations = viewModel.Observations;
                container.ClientCashierId = viewModel.ClientCashierId;
                container.ClientCashierName = viewModel.ClientCashierName;
                container.ClientEnvelopeDate = viewModel.ClientEnvelopeDate;
                _context.CefContainers.Update(container);
                _context.CefValueDetails.RemoveRange(container.ValueDetails);
            }
            await _context.SaveChangesAsync();

            foreach (var detailVm in viewModel.ValueDetails)
            {
                var detail = new CefValueDetail
                {
                    CefContainerId = container.Id,
                    ValueType = detailVm.ValueType.ToString(),
                    DenominationId = detailVm.Denomination,
                    Quantity = detailVm.Quantity,
                    BundlesCount = detailVm.BundlesCount,
                    LoosePiecesCount = detailVm.LoosePiecesCount,
                    UnitValue = detailVm.UnitValue,
                    CalculatedAmount = detailVm.CalculatedAmount,
                    IsHighDenomination = detailVm.IsHighDenomination,
                    IdentifierNumber = detailVm.IdentifierNumber,
                    BankName = detailVm.BankName,
                    IssueDate = detailVm.IssueDate,
                    Issuer = detailVm.Issuer,
                    Observations = detailVm.Observations
                };
                await _context.CefValueDetails.AddAsync(detail);
            }

            if (container.ContainerStatus == CefContainerStatusEnum.InProcess.ToString() || container.ContainerStatus == CefContainerStatusEnum.Pending.ToString())
            {
                container.ContainerStatus = CefContainerStatusEnum.Counted.ToString();
                container.ProcessingDate = DateTime.Now;
            }
            _context.CefContainers.Update(container);

            await _context.SaveChangesAsync();
            return container;
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
                .Select(d => new {
                    id = d.CodDenominacion,
                    value = d.ValorDenominacion,
                    type = d.TipoDenominacion,
                    tipoDinero = d.TipoDinero, // M = Moneda, B = Billete
                    denominacion = d.Denominacion
                })
                .OrderBy(d => d.tipoDinero)
                .ThenByDescending(d => d.value) // Ordenar por valor descendente
                .ToListAsync();

            var esCO = CultureInfo.GetCultureInfo("es-CO");

            // Mapear según TipoDinero: M = Coin, B = Bill
            var payload = new
            {
                Bill = denoms
                    .Where(d => d.tipoDinero == "B") // B = Billete
                    .Select(d => new {
                        id = d.id,
                        value = d.value,
                        label = d.denominacion ?? d.value?.ToString("C0", esCO)
                    }),
                Coin = denoms
                    .Where(d => d.tipoDinero == "M") // M = Moneda
                    .Select(d => new {
                        id = d.id,
                        value = d.value,
                        label = d.denominacion ?? d.value?.ToString("C0", esCO)
                    }),
                Document = denoms
                    .Where(d => d.tipoDinero == "D") // Documentos si existen
                    .Select(d => new {
                        id = d.id,
                        value = d.value,
                        label = d.denominacion ?? d.value?.ToString("C0", esCO)
                    }),
                Check = denoms
                    .Where(d => d.tipoDinero == "C") // Cheques si existen
                    .Select(d => new {
                        id = d.id,
                        value = d.value,
                        label = d.denominacion ?? d.value?.ToString("C0", esCO)
                    })
            };

            return JsonSerializer.Serialize(payload);
        }
    }
}