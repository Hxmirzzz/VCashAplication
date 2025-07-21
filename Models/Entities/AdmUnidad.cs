using System.ComponentModel.DataAnnotations;

namespace VCashApp.Models.Entities
{
    public class AdmUnidad
    {
        [Key]
        public string CodUnidad { get; set; }
        public string? NombreUnidad { get; set; }
        public string? TipoUnidad { get; set; }
    }
}