namespace VCashApp.Models.ViewModels.Range
{
    /// <summary>
    /// Vista resumida de un rango de atención.
    /// Usado en listados y tablas para mostrar la información básica.
    /// </summary>
    public class RangeSummaryViewModel
    {
        /// <summary>
        /// Identificador único del rango.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Código del rango definido por el usuario o el sistema.
        /// </summary>
        public string CodRange { get; set; } = string.Empty;

        /// <summary>
        /// Identificador del cliente asociado al rango.
        /// </summary>
        public int ClientId { get; set; }

        /// <summary>
        /// Nombre del cliente asociado al rango.
        /// </summary>
        public string ClientName { get; set; } = string.Empty;

        /// <summary>
        /// Información descriptiva o notas del rango.
        /// </summary>
        public string? RangeInformation { get; set; }

        /// <summary>
        /// Indica si el rango está activo (<c>true</c>) o inactivo (<c>false</c>).
        /// </summary>
        public bool RangeStatus { get; set; }

        /// <summary>
        /// Texto concatenado de los días activos (ejemplo: "Lunes, Martes, Jueves").
        /// </summary>
        public string ActiveDays { get; set; } = string.Empty;
    }
}