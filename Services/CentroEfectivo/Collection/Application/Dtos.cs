using VCashApp.Enums;
using VCashApp.Models.ViewModels.CentroEfectivo.Shared;

namespace VCashApp.Services.CentroEfectivo.Collection.Application
{
    // Commands
    public sealed record CreateCollectionCmd(
        int? SlipNumber,
        string ServiceOrderId,
        string Currency,
        decimal DeclaredTotalValue,
        int DeclaredBagCount,
        string? Observations);

    public sealed record SaveCollectionContainersCmd(
        IReadOnlyList<CefContainerProcessingViewModel> Containers,
        string? InformativeIncident,
        int? SlipNumber
    );

    // Query VMs
    public sealed record CollectionSummaryVm(
        int TxId, string ServiceOrderId, string Currency,
        decimal Declared, decimal Counted, CefTransactionStatusEnum Status);
}