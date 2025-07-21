using System.ComponentModel.DataAnnotations;

namespace VCashApp.Models.Entities
{
    public class AdmVista
    {
        [Key]
        public string CodVista { get; set; }
        public string? NombreVista { get; set; }
        public string? RolAsociado { get; set; } // Si tu columna 'rol' es para algo
    }
}