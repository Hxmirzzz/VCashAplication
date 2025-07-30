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
        public async Task<CefContainerProcessingViewModel> PrepareContainerProcessingViewModelAsync(int? containerId, int cefTransactionId)
        {
            var viewModel = new CefContainerProcessingViewModel
            {
                CefTransactionId = cefTransactionId,
                ValueDetails = new List<CefValueDetailViewModel>(),
                Incidents = new List<CefIncidentViewModel>()
            };

            if (containerId.HasValue && containerId.Value > 0)
            {
                var container = await GetContainerWithDetailsAsync(containerId.Value);
                if (container != null)
                {
                    viewModel.Id = container.Id; 
                    viewModel.ParentContainerId = container.ParentContainerId;
                    viewModel.ContainerType = Enum.Parse<CefContainerTypeEnum>(container.ContainerType);
                    viewModel.ContainerCode = container.ContainerCode;
                    viewModel.DeclaredValue = container.DeclaredValue;
                    viewModel.ContainerStatus = Enum.Parse<CefContainerStatusEnum>(container.ContainerStatus);
                    viewModel.Observations = container.Observations;
                    viewModel.ClientCashierId = container.ClientCashierId;
                    viewModel.ClientCashierName = container.ClientCashierName;
                    viewModel.ClientEnvelopeDate = container.ClientEnvelopeDate;
                    viewModel.CurrentCountedValue = container.CountedValue ?? 0;

                    viewModel.ValueDetails = container.ValueDetails.Select(vd => new CefValueDetailViewModel
                    {
                        Id = vd.Id,
                        ValueType = Enum.Parse<CefValueTypeEnum>(vd.ValueType),
                        Denomination = vd.Denomination,
                        Quantity = vd.Quantity,
                        BundlesCount = vd.BundlesCount,
                        LoosePiecesCount = vd.LoosePiecesCount,
                        UnitValue = vd.UnitValue,
                        CalculatedAmount = vd.CalculatedAmount ?? 0,
                        IsHighDenomination = vd.IsHighDenomination,
                        IdentifierNumber = vd.IdentifierNumber,
                        BankName = vd.BankName,
                        IssueDate = vd.IssueDate,
                        Issuer = vd.Issuer,
                        Observations = vd.Observations
                    }).ToList();

                    viewModel.Incidents = container.Incidents.Select(ni => new CefIncidentViewModel
                    {
                        Id = ni.Id,
                        CefTransactionId = ni.CefTransactionId,
                        CefContainerId = ni.CefContainerId,
                        CefValueDetailId = ni.CefValueDetailId,
                        IncidentType = Enum.Parse<CefIncidentTypeCategoryEnum>(_context.CefIncidentTypes.FirstOrDefault(it => it.Id == ni.IncidentTypeId)?.Code ?? "Otro"),
                        AffectedAmount = ni.AffectedAmount,
                        AffectedDenomination = ni.AffectedDenomination,
                        AffectedQuantity = ni.AffectedQuantity,
                        Description = ni.Description,
                        ReportDate = ni.IncidentDate,
                        ReportingUserName = _context.Users.FirstOrDefault(u => u.Id == ni.ReportedUserId)?.NombreUsuario ?? "N/A",
                        IncidentStatus = ni.IncidentStatus
                    }).ToList();
                }
            }
            return viewModel;
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
                    Denomination = detailVm.Denomination,
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
    }
}