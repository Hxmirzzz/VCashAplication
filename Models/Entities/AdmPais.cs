using System.ComponentModel.DataAnnotations;

namespace VCashApp.Models.Entities
{
    public class AdmPais
    {
        [Key]
        public string CodPais { get; set; }
        public string? NombrePais { get; set; }
        public string? Siglas { get; set; }
        public bool Estado { get; set; }
    }
}