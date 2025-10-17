using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels.CentroEfectivo;
using VCashApp.Services.CentroEfectivo.Provision.Domain;

namespace VCashApp.Services.CentroEfectivo.Provision.Infrastructure
{
    /// <summary>Repositorio EF para contenedores y detalles de valor.</summary>
    public sealed class CefContainerRepository : ICefContainerRepository
    {
        private readonly AppDbContext _db;

        public CefContainerRepository(AppDbContext db) => _db = db;

        public async Task SaveContainersAndDetailsAsync(
            int txId,
            IReadOnlyList<CefContainerProcessingViewModel> containers,
            string userId,
            IAllowedValueTypesPolicy allowed)
        {
            var tx = await _db.CefTransactions
                .Include(t => t.Containers)
                    .ThenInclude(c => c.ValueDetails)
                .FirstOrDefaultAsync(t => t.Id == txId) ?? throw new InvalidOperationException("Tx no existe.");

            var existingByCode = tx.Containers.ToDictionary(c => c.ContainerCode, c => c, StringComparer.OrdinalIgnoreCase);

            foreach (var vm in containers ?? Array.Empty<CefContainerProcessingViewModel>())
            {
                if (!existingByCode.TryGetValue(vm.ContainerCode, out var entity))
                {
                    entity = new CefContainer
                    {
                        CefTransactionId = txId,
                        ContainerCode = vm.ContainerCode,
                        ContainerType = vm.ContainerType.ToString(),
                        EnvelopeSubType = vm.EnvelopeSubType?.ToString(),
                        ContainerStatus = "Procesado",
                        Observations = vm.Observations,
                        ProcessingUserId = userId,
                        ProcessingDate = DateTime.Now
                    };
                    _db.CefContainers.Add(entity);
                    existingByCode[vm.ContainerCode] = entity;
                }
                else
                {
                    entity.ContainerType = vm.ContainerType.ToString();
                    entity.EnvelopeSubType = vm.EnvelopeSubType?.ToString();
                    entity.Observations = vm.Observations;
                    entity.ProcessingUserId = userId;
                    entity.ProcessingDate = DateTime.Now;
                }

                var oldDetails = entity.ValueDetails?.ToList() ?? new List<CefValueDetail>();
                if (oldDetails.Count > 0) _db.CefValueDetails.RemoveRange(oldDetails);

                var newDetails = new List<CefValueDetail>();
                foreach (var d in vm.ValueDetails ?? Enumerable.Empty<CefValueDetailViewModel>())
                {
                    if (!allowed.IsAllowed(d.ValueType)) throw new InvalidOperationException("Tipo de valor no permitido en provisión.");

                    var amount = d.CalculatedAmount;
                    if (amount == 0 && (d.UnitValue ?? 0) > 0 && (d.Quantity ?? 0) > 0)
                        amount = (d.UnitValue ?? 0) * (d.Quantity ?? 0);

                    newDetails.Add(new CefValueDetail
                    {
                        CefContainerId = entity.Id,
                        ValueType = d.ValueType.ToString(),
                        Quantity = d.Quantity ?? 0,
                        BundlesCount = d.BundlesCount ?? 0,
                        LoosePiecesCount = d.LoosePiecesCount ?? 0,
                        UnitValue = d.UnitValue ?? 0,
                        CalculatedAmount = amount,
                        IssueDate = d.IssueDate,
                        Observations = d.Observations,
                        DenominationId = d.DenominationId,
                        QualityId = d.QualityId,
                        IsHighDenomination = d.IsHighDenomination,
                        EntitieBankId = d.EntitieBankId
                    });
                }

                entity.ValueDetails = newDetails;
                entity.CountedValue = newDetails.Sum(x => x.CalculatedAmount);
            }
        }
    }
}