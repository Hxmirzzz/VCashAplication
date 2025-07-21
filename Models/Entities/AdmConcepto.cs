using System.ComponentModel.DataAnnotations;

namespace VCashApp.Models.Entities
{
    public class AdmConcepto
    {
        [Key]
        public int CodConcepto { get; set; }

        public string? NombreConcepto { get; set; }

        public string? TipoConcepto { get; set; }
    }
}