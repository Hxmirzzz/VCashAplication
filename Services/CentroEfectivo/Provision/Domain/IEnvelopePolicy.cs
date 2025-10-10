using VCashApp.Enums;

namespace VCashApp.Services.CentroEfectivo.Provision.Domain
{
    public interface IEnvelopePolicy
    {
        bool AllowEnvelopes { get; }
        bool IsValidEnvelope(CefEnvelopeSubTypeEnum? subType, CefValueTypeEnum valueType);
    }
}