using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCashApp.Models.Entities
{
    [Table("CgsTiposUbicaion")]
    public class CgsLocationType
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Column("NombreTipo")]
        public string TypeName { get; set; }
    }
}