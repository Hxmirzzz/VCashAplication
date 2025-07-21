using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCashApp.Models.Entities
{
    public class AdmDepartamento
    {
        [Key]
        public string CodDepartamento { get; set; }
        public string? NombreDepartamento { get; set; }
        public string? CodPais { get; set; }
        [ForeignKey("CodPais")]
        public virtual AdmPais? Pais { get; set; }
        public bool Estado { get; set; }
    }
}