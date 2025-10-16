using VCashApp.Enums;
using VCashApp.Models.ViewModels.CentroEfectivo;

namespace VCashApp.Services.CentroEfectivo.Provision.Application
{
    // Commands
    public sealed record CreateProvisionCmd(
        string ServiceOrderId,
        string Currency,
        decimal DeclaredBill,
        decimal DeclaredCoin,
        string? Observations);

    public sealed record SaveProvisionContainersCmd(
        IReadOnlyList<CefContainerProcessingViewModel> Containers,
        string? Currency,
        int? SlipNumber
        //string? Observations
    );

    // Query VMs (puedes mapear con AutoMapper si ya usas)
    public sealed record ProvisionPageVm(
        int CefTransactionId,
        CefTransactionDetailViewModel Transaction,
        CefServiceCreationViewModel Service, // ajusta al VM que ya uses para cabecera
        IReadOnlyList<CefContainerProcessingViewModel> Containers,
        decimal Declared, decimal Counted, decimal Difference);

    public sealed record ProvisionSummaryVm(
        int TxId, string ServiceOrderId, string Currency,
        decimal Declared, decimal Counted, CefTransactionStatusEnum Status);
}