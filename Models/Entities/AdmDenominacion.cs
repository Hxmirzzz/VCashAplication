using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCashApp.Models.Entities
{
    public class AdmDenominacion
    {
        [Key]
        public int CodDenominacion { get; set; }

        public string? TipoDenominacion { get; set; }

        public string? Denominacion { get; set; }
        public decimal? ValorDenominacion { get; set; }

        public string? TipoDinero { get; set; }

        public string? FamiliaDenominacion { get; set; }

        public string? DivisaDenominacion { get; set; }

        public string? UnidadAgrupamiento { get; set; }
        public int? CantidadUnidadAgrupamiento { get; set; }

        public string? TeclaAsociada { get; set; }

        public string? UnidadExistencias { get; set; }
    }
}