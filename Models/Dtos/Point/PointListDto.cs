namespace VCashApp.Models.Dtos.Point
{
    /// <summary>DTO para lista de puntos.</summary>
    public sealed class PointListDto
    {
        /// <summary>Código del punto.</summary>
        public string CodPunto { get; init; } = string.Empty;
        /// <summary>Código Vatco del punto.</summary>
        public string CodPCliente { get; init; } = string.Empty;
        /// <summary>Codigo del cliente.</summary>
        public int CodCliente { get; init; }
        /// <summary>Nombre del cliente.</summary>
        public string? ClienteNombre { get; init; }
        /// <summary>Cliente principal.</summary>
        public int CodClientePpal { get; init; }
        /// <summary>Nombre del cliente principal.</summary>
        public string? ClientePpalNombre { get; init; }
        /// <summary>Nombre de punto.</summary>
        public string? NombrePunto { get; init; }
        /// <summary>Nombre corto del punto.</summary>
        public string? NombreCorto { get; init; }
        /// <summary>Sucursal.</summary>
        public int CodSuc { get; init; }
        /// <summary>Nombre de la sucursal.</summary>
        public string? NombreSucursal { get; init; }
        /// <summary>Ciudad.</summary>
        public int CodCiudad { get; init; }
        /// <summary>Nombre de la ciudad.</summary>
        public string? NombreCiudad { get; init; }
        /// <summary>Nombre del fondo.</summary>
        public string? FundName { get; init; }
        /// <summary>Nombre de la ruta.</summary>
        public string? RouteName { get; init; }
        /// <summary>Nombre del rango.</summary>
        public string? RangeName { get; init; }
        /// <summary>Latitud.</summary>
        public string? Latitude { get; set; }
        /// <summary>Longitud.</summary>
        public string? Longitude { get; set; }
        /// <summary>Radio del punto.</summary>
        public string? PointRadius { get; init; }
        /// <summary>Estado.</summary>
        public bool EstadoPunto { get; init; }
    }
}