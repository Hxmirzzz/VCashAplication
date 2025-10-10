using VCashApp.Enums;

namespace VCashApp.Services.CentroEfectivo.Provision.Domain
{
    /// <summary>En provisión solo se permite Billete y Moneda.</summary>
    public sealed class ProvisionAllowedValueTypesPolicy : IAllowedValueTypesPolicy
    {
        public bool IsAllowed(CefValueTypeEnum vt) =>
            vt == CefValueTypeEnum.Billete || vt == CefValueTypeEnum.Moneda;
    }
}