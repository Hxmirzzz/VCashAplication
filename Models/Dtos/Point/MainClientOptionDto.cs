namespace VCashApp.Models.Dtos.Point
{
    /// <summary>Opción de cliente principal para select.</summary>
    public sealed class MainClientOptionDto
    {
        /// <summary>Código del cliente principal.</summary>
        public int Value { get; set; }
        /// <summary>Texto descriptivo del cliente principal.</summary>
        public string Text { get; set; } = "";
        /// <summary>Indica si está seleccionado.</summary>
        public bool Selected { get; set; }
        /// <summary>Indica si la opción está bloqueada para selección.</summary>
        public bool LockSelect { get; set; }
    }
}