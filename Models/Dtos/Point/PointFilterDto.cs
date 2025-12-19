namespace VCashApp.Models.Dtos.Point
{
    /// <summary>Filtros para listado de puntos.</summary>
    public sealed class PointFilterDto
    {
        /// <summary>Término de búsqueda libre (código, nombre, dirección, responsable).</summary>
        public string? Search { get; set; }
        /// <summary>Tipo de punto.</summary>
        public int? ClientCode { get; set; }
        /// <summary>Código de cliente principal.</summary>
        public int? MainClientCode { get; set; }
        /// <summary>Código de sucursal.</summary>
        public int? BranchCode { get; set; }
        /// <summary>Código de ciudad.</summary>
        public int? CityCode { get; set; }
        /// <summary>Código de fondo.</summary>
        public string? FundCode { get; set; }
        /// <summary>Estado activo/inactivo.</summary>
        public bool? IsActive { get; set; }
        /// <summary>Código de ruta.</summary>
        public string? RouteCode { get; set; }
        /// <summary>Código de rango.</summary>
        public int? RangeCode { get; set; }
        /// <summary>Número de página.</summary>
        public int Page { get; set; } = 1;
        /// <summary>Tamaño de página.</summary>
        public int PageSize { get; set; } = 15;
    }
}
