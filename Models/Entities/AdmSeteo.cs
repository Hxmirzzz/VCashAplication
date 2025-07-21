using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCashApp.Models.AdmEntities
{
    public class AdmSeteo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string CodSeteo { get; set; } = null!;
        public int? CodCliente { get; set; }
        public string? Denominaciones { get; set; }
        public string? TipoAtmSeteo { get; set; }
        public decimal? VlrTotalSeteo { get; set; }
        public decimal? VlrTotalGavetas { get; set; }
        public decimal? VlrTotalHopper { get; set; }
        public decimal? VlrTotalCassette { get; set; }

        public int? G1 { get; set; }
        public decimal? DG1 { get; set; }
        public string? FG1 { get; set; }
        public int? G2 { get; set; }
        public decimal? DG2 { get; set; }
        public string? FG2 { get; set; }
        public int? G3 { get; set; }
        public decimal? DG3 { get; set; }
        public string? FG3 { get; set; }
        public int? G4 { get; set; }
        public decimal? DG4 { get; set; }
        public string? FG4 { get; set; }
        public int? G5 { get; set; }
        public decimal? DG5 { get; set; }
        public string? FG5 { get; set; }
        public int? G6 { get; set; }
        public decimal? DG6 { get; set; }
        public string? FG6 { get; set; }
        public int? G7 { get; set; }
        public decimal? DG7 { get; set; }
        public string? FG7 { get; set; }
        public int? G8 { get; set; }
        public decimal? DG8 { get; set; }
        public string? FG8 { get; set; }
        public int? G9 { get; set; }
        public decimal? DG9 { get; set; }
        public string? FG9 { get; set; }
        public int? G10 { get; set; }
        public decimal? DG10 { get; set; }
        public string? FG10 { get; set; }
        public int? G11 { get; set; }
        public decimal? DG11 { get; set; }
        public string? FG11 { get; set; }
        public int? G12 { get; set; }
        public decimal? DG12 { get; set; }
        public string? FG12 { get; set; }

        public int? H1 { get; set; }
        public decimal? DH1 { get; set; }
        public string? FH1 { get; set; }
        public int? H2 { get; set; }
        public decimal? DH2 { get; set; }
        public string? FH2 { get; set; }
        public int? H3 { get; set; }
        public decimal? DH3 { get; set; }
        public string? FH3 { get; set; }
        public int? H4 { get; set; }
        public decimal? DH4 { get; set; }
        public string? FH4 { get; set; }
        public int? H5 { get; set; }
        public decimal? DH5 { get; set; }
        public string? FH5 { get; set; }
        public int? H6 { get; set; }
        public decimal? DH6 { get; set; }
        public string? FH6 { get; set; }
        public int? H7 { get; set; }
        public decimal? DH7 { get; set; }
        public string? FH7 { get; set; }
        public int? H8 { get; set; }
        public decimal? DH8 { get; set; }
        public string? FH8 { get; set; }
        public int? H9 { get; set; }
        public decimal? DH9 { get; set; }
        public string? FH9 { get; set; }

        public int? C1 { get; set; }
        public decimal? DC1 { get; set; }
        public string? FC1 { get; set; }
        public int? C2 { get; set; }
        public decimal? DC2 { get; set; }
        public string? FC2 { get; set; }
        public int? C3 { get; set; }
        public decimal? DC3 { get; set; }
        public string? FC3 { get; set; }

        public int? SeteoEstado { get; set; }
    }
}
