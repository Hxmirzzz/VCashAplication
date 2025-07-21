using System.ComponentModel.DataAnnotations;

namespace VCashApp.Models.Entities
{
    public class AdmEstado
    {
        [Key]
        public int CodEstado { get; set; }

        public string? NombreEstado { get; set; }
    }
}