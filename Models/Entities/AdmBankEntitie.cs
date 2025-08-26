using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCashApp.Models.Entities
{
    [Table("AdmEntidadesBancarias")]
    public class AdmBankEntitie
    {
        [Key]
        [Column("Id")]
        [StringLength(10)]
        public string Id { get; set; }

        [Required]
        [StringLength(255)]
        [Column("Nombre")]
        public string Name { get; set; }
    }
}
