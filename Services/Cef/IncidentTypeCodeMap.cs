using VCashApp.Enums;

namespace VCashApp.Services.Cef
{
    public static class IncidentTypeCodeMap
    {
        // Enum -> Código en tu tabla (SOB, FAL, FALSO, MS, MF)
        public static string ToMasterCode(CefIncidentTypeCategoryEnum cat) => cat switch
        {
            CefIncidentTypeCategoryEnum.Sobrante => "SOB",
            CefIncidentTypeCategoryEnum.Faltante => "FAL",
            CefIncidentTypeCategoryEnum.False => "FALSO",
            CefIncidentTypeCategoryEnum.MezclaSobrante => "MS",
            CefIncidentTypeCategoryEnum.MezclaFaltante => "MF",
            _ => cat.ToString()
        };

        // Código (SOB/FAL/FALSO/MS/MF o nombres largos) -> Enum
        public static bool TryFromCode(string? code, out CefIncidentTypeCategoryEnum cat)
        {
            cat = default;
            var s = (code ?? "").Trim().ToUpperInvariant();
            switch (s)
            {
                case "SOB":
                case "SOBRANTE": cat = CefIncidentTypeCategoryEnum.Sobrante; return true;
                case "FAL":
                case "FALTANTE": cat = CefIncidentTypeCategoryEnum.Faltante; return true;
                case "FALSO":
                case "FALSE": cat = CefIncidentTypeCategoryEnum.False; return true;
                case "MS":
                case "MEZCLASOBRANTE": cat = CefIncidentTypeCategoryEnum.MezclaSobrante; return true;
                case "MF":
                case "MEZCLAFALTANTE": cat = CefIncidentTypeCategoryEnum.MezclaFaltante; return true;
                default: return false;
            }
        }
    }
}