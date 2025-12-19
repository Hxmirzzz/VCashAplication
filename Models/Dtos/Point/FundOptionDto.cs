namespace VCashApp.Models.Dtos.Point
{
    /// <summary>Opción de fondo para select.</summary>
    public sealed class FundOptionDto
    {
        /// <summary>Código del fondo.</summary>
        public string Value { get; set; } = "";
        /// <summary>Texto descriptivo del fondo.</summary>
        public string Text { get; set; } = "";
    }
}