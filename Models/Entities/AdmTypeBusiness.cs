using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCashApp.Models.Entities
{
    /// <summary>Tipo de negocio.</summary>
    public class AdmTypeBusiness
    {
        /// <summary>Identificador del tipo de negocio.</summary>
        [Key]
        public int Id { get; set; }
        /// <summary>Descripción del tipo de negocio.</summary>
        [StringLength(255)]
        [Column("Descripcion")]
        public string Description { get; set; } = null!;
    }
}