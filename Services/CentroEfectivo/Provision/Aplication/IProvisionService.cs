namespace VCashApp.Services.CentroEfectivo.Provision.Application
{
    /// <summary>Casos de uso de Provisión (commands).</summary>
    public interface IProvisionService
    {
        Task<int> CreateProvisionAsync(CreateProvisionCmd cmd, string userId);
        Task SaveContainersAsync(int txId, SaveProvisionContainersCmd cmd, string userId);
        Task FinalizeAsync(int txId, string userId);
        Task DeliverAsync(int txId, string userId);
    }
}