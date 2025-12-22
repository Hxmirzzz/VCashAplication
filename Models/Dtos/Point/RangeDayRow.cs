namespace VCashApp.Models.Dtos.Point
{
    /// <summary>Fila de rangos por día.</summary>
    public sealed class RangeDayRow
    {
        /// <summary>Nombre del día.</summary>
        public string DayName { get; set; } = "";
        /// <summary>Rango uno.</summary>
        public string? RangeOne { get; set; }
        /// <summary>Rango dos.</summary>
        public string? RangeTwo { get; set; }
        /// <summary>Rango tres.</summary>
        public string? RangeThree { get; set; }
    }
}
