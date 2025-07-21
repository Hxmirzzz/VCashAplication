using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCashApp.Models.Entities
{
    public class AdmCargo
    {
        [Key]
        public int CodCargo { get; set; }
        public string? NombreCargo { get; set; }
        public string? CodUnidad { get; set; }
        [ForeignKey("CodUnidad")]
        public virtual AdmUnidad? Unidad { get; set; }
        public TimeSpan? Jornada { get; set; }
        public TimeSpan? TiempoBreak { get; set; }
        public bool Adicional { get; set; }
        public bool Recargos { get; set; }
        public bool Extras { get; set; }
        public bool CentroCosto { get; set; }
    }
}