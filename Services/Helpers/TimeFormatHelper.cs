// VCashApp/Services/Helpers/TimeFormatHelper.cs
using System;
using System.Globalization;

namespace VCashApp.Services.Helpers
{
    public static class TimeFormatHelper
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>
        /// Normaliza a "HH:mm" para '<input type="time">,
        /// aceptando TimeOnly, TimeSpan, DateTime o string.
        /// </summary>
        public static string ToTimeInput24(object? value)
        {
            if (value is null) return string.Empty;

            switch (value)
            {
                case TimeOnly to:
                    return to.ToString("HH:mm", Inv);

                case TimeSpan ts:
                    // Solo horas:minutos
                    return new TimeOnly(ts.Hours, ts.Minutes).ToString("HH:mm", Inv);

                case DateTime dt:
                    return dt.ToString("HH:mm", Inv);

                case string s:
                    // Intentos de parseo tolerantes
                    if (TimeOnly.TryParseExact(s, "HH:mm", Inv, DateTimeStyles.None, out var toParsed))
                        return toParsed.ToString("HH:mm", Inv);

                    if (DateTime.TryParse(s, Inv, DateTimeStyles.None, out var dtParsed))
                        return dtParsed.ToString("HH:mm", Inv);

                    if (TimeSpan.TryParse(s, Inv, out var tsParsed))
                        return new TimeOnly(tsParsed.Hours, tsParsed.Minutes).ToString("HH:mm", Inv);

                    // Como último recurso, deja el string tal cual (si ya venía "HH:mm")
                    return s;

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// "yyyy-MM-dd" para <input type="date"> (nullable).
        /// </summary>
        public static string ToDateInput(DateOnly? d)
            => d.HasValue ? d.Value.ToString("yyyy-MM-dd", Inv) : string.Empty;

        /// <summary>
        /// "yyyy-MM-dd" para <input type="date"> (no nullable).
        /// </summary>
        public static string ToDateInput(DateOnly d)
            => d.ToString("yyyy-MM-dd", Inv);
    }
}
