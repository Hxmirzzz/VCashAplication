namespace VCashApp.Services.CentroEfectivo.Provision.Domain
{
    public interface ITolerancePolicy
    {
        bool IsWithinTolerance(decimal declared, decimal counted);
    }
}