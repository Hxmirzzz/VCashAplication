using VCashApp.Enums;

namespace VCashApp.Services.CentroEfectivo.Provision.Domain
{
    /// <summary>Sobres permitidos sólo si son sub-tipo Efectivo.</summary>
    public sealed class ProvisionEnvelopePolicy : IEnvelopePolicy
    {
        public bool AllowEnvelopes { get; init; } = true;

        public bool IsValidEnvelope(CefEnvelopeSubTypeEnum? subType, CefValueTypeEnum vt) =>
            AllowEnvelopes
            && subType == CefEnvelopeSubTypeEnum.Efectivo
            && (vt == CefValueTypeEnum.Billete || vt == CefValueTypeEnum.Moneda);
    }
}