namespace VCashApp.Models.Dtos.Point
{
    /// <summary>Datos para Vista Previa de Punto (KPIs, datos extendidos).</summary>
    public sealed class PointPreviewDto
    {
        /// <summary>Código del punto.</summary>
        public string CodPunto { get; init; } = string.Empty;
        /// <summary>Código CLIENTE del punto.</summary>
        public string CodPCliente { get; init; } = string.Empty;
        /// <summary>Nombre del cliente.</summary>
        public string? ClienteNombre { get; init; }
        /// <summary>Nombre del cliente principal.</summary>
        public string? ClientePpalNombre { get; init; }
        /// <summary>Nombre del punto.</summary>
        public string? NombrePunto { get; init; }
        /// <summary>Nombre corto del punto.</summary>
        public string? NombreCorto { get; init; }
        /// <summary>Sucursal.</summary>
        public string? NombreSucursal { get; init; }
        /// <summary>Ciudad.</summary>
        public string? NombreCiudad { get; init; }

        // KPI principales
        /// <summary>Estado.</summary>
        public bool EstadoPunto { get; init; }
        /// <summary>Fondo asociado</summary>
        public string? FondoAsociado { get; init; }
        /// <summary>Ruta asociada</summary>
        public string? RutaAsociada { get; init; }
        /// <summary>Rango asociado</summary>
        public string? RangoAsociado { get; init; }
        /// <summary>Nivel de riesgo</summary>
        public string NivelRiesgo { get; init; } = "M";
        /// <summary>Cobertura del punto</summary>
        public string CoberturaPunto { get; init; } = "U";
        /// <summary>Fecha de ingreso</summary>
        public DateOnly FecIngreso { get; init; }
        /// <summary>Fecha de retiro</summary>
        public DateOnly? FecRetiro { get; init; }

        // Ubicación
        /// <summary>Latitud.</summary>
        public string? LatPunto { get; init; }
        /// <summary>Longitud.</summary>
        public string? LngPunto { get; init; }
        /// <summary>Radio del punto.</summary>
        public int? RadioPunto { get; init; }

        // Flags
        /// <summary>Base de cambio.</summary>
        public int BaseCambio { get; init; }
        /// <summary>Llaves del punto.</summary>
        public int LlavesPunto { get; init; }
        /// <summary>Documentos del punto.</summary>
        public int DocumentosPunto { get; init; }
        /// <summary>Existencias del punto.</summary>
        public int ExistenciasPunto { get; init; }
        /// <summary>Predicción del punto.</summary>
        public int PrediccionPunto { get; init; }
        /// <summary>Custodia del punto.</summary>
        public int CustodiaPunto { get; init; }
        /// <summary>Otros valores del punto.</summary>
        public int OtrosValoresPunto { get; init; }
        /// <summary>Liberación de efectivo del punto.</summary>
        public int LiberacionEfectivoPunto { get; init; }
        /// <summary>Otros.</summary>
        public string? Otros { get; init; }
        /// <summary>Archivo.</summary>
        public string? CartaFilePath { get; init; }
    }
}
