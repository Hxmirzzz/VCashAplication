namespace VCashApp.Models.Dtos.Point
{
    public sealed class PointPreviewDto
    {
        public string CodPunto { get; init; } = string.Empty;
        public string CodPCliente { get; init; } = string.Empty;

        public string? ClienteNombre { get; init; }
        public string? ClientePpalNombre { get; init; }

        public string? NombrePunto { get; init; }
        public string? NombreCorto { get; init; }

        public string? NombreSucursal { get; init; }
        public string? NombreCiudad { get; init; }

        // KPI principales
        public bool EstadoPunto { get; init; }
        public string? FondoAsociado { get; init; }
        public string? RutaAsociada { get; init; }
        public string? RangoAsociado { get; init; }
        public string NivelRiesgo { get; init; } = "M";
        public string CoberturaPunto { get; init; } = "U";

        public DateOnly FecIngreso { get; init; }
        public DateOnly? FecRetiro { get; init; }

        // Ubicación
        public string? LatPunto { get; init; }
        public string? LngPunto { get; init; }
        public int? RadioPunto { get; init; }

        // Flags
        public int BaseCambio { get; init; }
        public int LlavesPunto { get; init; }
        public int DocumentosPunto { get; init; }
        public int ExistenciasPunto { get; init; }
        public int PrediccionPunto { get; init; }
        public int CustodiaPunto { get; init; }
        public int OtrosValoresPunto { get; init; }
        public int LiberacionEfectivoPunto { get; init; }

        public string? Otros { get; init; }

        public string? CartaFilePath { get; init; }
    }
}
