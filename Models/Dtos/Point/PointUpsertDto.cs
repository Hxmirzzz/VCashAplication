using System.ComponentModel.DataAnnotations;

namespace VCashApp.Models.Dtos.Point
{
    public sealed class PointUpsertDto
    {
        [Required]
        public string CodPunto { get; set; } = string.Empty;

        [Required]
        public string? VatcoPointCode { get; set; } = string.Empty;

        [Required]
        public string CodPCliente { get; set; } = string.Empty;

        [Required]
        public int CodCliente { get; set; }

        [Required]
        public int TipoPunto { get; set; }

        public int? CodClientePpal { get; set; }

        public string? NombrePunto { get; set; }
        public string? NombreCorto { get; set; }
        public string? NombrePuntoFact { get; set; }
        public string? DirPunto { get; set; }
        public string? TelPunto { get; set; }

        public string? RespPunto { get; set; }
        public string? CargoRespPunto { get; set; }
        public string? CorreoRespPunto { get; set; }

        public int CodSuc { get; set; }
        public int CodCiudad { get; set; }

        public string? LatPunto { get; set; }
        public string? LngPunto { get; set; }
        public int? RadioPunto { get; set; }

        public int BaseCambio { get; set; }
        public int LlavesPunto { get; set; }
        public int SobresPunto { get; set; }
        public int ChequesPunto { get; set; }
        public int DocumentosPunto { get; set; }
        public int ExistenciasPunto { get; set; }
        public int PrediccionPunto { get; set; }
        public int CustodiaPunto { get; set; }
        public int OtrosValoresPunto { get; set; }
        public string? Otros { get; set; }
        public int LiberacionEfectivoPunto { get; set; }

        public int FondoPunto { get; set; }
        public string? CodFondo { get; set; }
        public string? CodRutaSuc { get; set; }
        public int? CodRango { get; set; }
        public int? BusinessType { get; set; }

        [DataType(DataType.Date)]
        public DateOnly FecIngreso { get; init; }

        [DataType(DataType.Date)]
        public DateOnly? FecRetiro { get; init; }

        public string? CodCas4u { get; set; }
        public string NivelRiesgo { get; set; } = "M";

        public string CoberturaPunto { get; set; } = "U";
        public int EscalaInterurbanos { get; set; } = 0;

        public int Consignacion { get; set; }

        public bool EstadoPunto { get; set; }

        public string? CartaFilePath { get; set; }
    }
}
