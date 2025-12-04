    namespace VCashApp.Models.Dtos.Point
    {
        public sealed class PointListDto
        {
            public string CodPunto { get; init; } = string.Empty;
            public string CodPCliente { get; init; } = string.Empty;

            public int CodCliente { get; init; }
            public string? ClienteNombre { get; init; }

            public int CodClientePpal { get; init; }
            public string? ClientePpalNombre { get; init; }

            public string? NombrePunto { get; init; }
            public string? NombreCorto { get; init; }

            public int CodSuc { get; init; }
            public string? NombreSucursal { get; init; }

            public int CodCiudad { get; init; }
            public string? NombreCiudad { get; init; }

            public string? FundName { get; init; }
            public string? RouteName { get; init; }
            public string? RangeName { get; init; }

            public string? Latitude { get; init; }
            public string? Longitude { get; init; }
            public string? PointRadius { get; init; }

            public bool EstadoPunto { get; init; }
        }
    }
