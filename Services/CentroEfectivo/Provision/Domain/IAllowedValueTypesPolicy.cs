using VCashApp.Enums;

namespace VCashApp.Services.CentroEfectivo.Provision.Domain
{
    public interface IAllowedValueTypesPolicy
    {
        bool IsAllowed(CefValueTypeEnum valueType);
    }
}