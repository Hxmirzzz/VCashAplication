using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCashApp.Models.Entities
{
    [Table("AdmEstados")]
    public class AdmState
    {
        [Key]
        [Column("CodigoEstado")]
        public int StateCode { get; set; }

        [Column("NombreEstado")]
        public string StateName { get; set; }
    }
}