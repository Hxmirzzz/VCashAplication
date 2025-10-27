using VCashApp.Enums;

namespace VCashApp.Services.CentroEfectivo.Shared.Incidents
{
    public static class IncidentCategoryExtensions
    {
        /// <summary>
        /// Devuelve +1 si “explica faltante” (Faltante/False/MezclaFaltante),
        /// -1 si “explica sobrante” (Sobrante/MezclaSobrante).
        /// </summary>
        public static int EffectSign(this CefIncidentTypeCategoryEnum cat) => cat switch
        {
            CefIncidentTypeCategoryEnum.Faltante => +1,
            CefIncidentTypeCategoryEnum.MezclaFaltante => +1,
            CefIncidentTypeCategoryEnum.Sobrante => -1,
            CefIncidentTypeCategoryEnum.MezclaSobrante => -1,
            CefIncidentTypeCategoryEnum.False => +1,
            _ => 0
        };
    }
}