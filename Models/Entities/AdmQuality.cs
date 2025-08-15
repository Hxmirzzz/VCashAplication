using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCashApp.Models.Entities
{
    [Table("AdmCalidad")]
    public class AdmQuality
    {
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("NombreCalidad")]
        [StringLength(255)]
        public string QualityName { get; set; }

        [Required]
        [Column("TipoDinero")]
        [StringLength(1)]
        public string TypeOfMoney { get; set; } = "B";

        [Required]
        [Column("Familia")]
        [StringLength(1)]
        public string DenominationFamily { get; set; } = "T";

        [Required]
        public bool Status { get; set; } = true;
    }
}
