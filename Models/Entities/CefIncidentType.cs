using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCashApp.Models.Entities
{
    [Table("CefTiposNovedad")]
    public class CefIncidentType
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Column("Codigo")]
        public string Code { get; set; } // Código interno para el tipo de novedad

        [Required]
        [StringLength(255)]
        [Column("Descripcion")]
        public string Description { get; set; } // Descripción legible (ej: "Sobrante", "Billetes Falsos")

        [Required]
        [StringLength(50)]
        [Column("AplicaPara")]
        public string AppliesTo { get; set; } // 'Service', 'Incident', 'Both' (Define a qué aplica esta falla/novedad)
    }
}
