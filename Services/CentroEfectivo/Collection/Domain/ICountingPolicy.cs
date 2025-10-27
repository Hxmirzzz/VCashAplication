namespace VCashApp.Services.CentroEfectivo.Collection.Domain
{
    public interface ICountingPolicy
    {
        bool CanCreate(string soId, string currency, decimal declaredTotalValue);
        bool CanFinalize(dynamic tx);
        bool CanApprove(dynamic tx);
    }
}
