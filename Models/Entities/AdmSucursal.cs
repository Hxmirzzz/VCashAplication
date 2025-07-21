using System.ComponentModel.DataAnnotations;

namespace VCashApp.Models.Entities
{
    public class AdmSucursal
    {
        [Key]
        public int CodSucursal { get; set; }
        public string? NombreSucursal { get; set; }
        public string? LatitudSucursal { get; set; }
        public string? LongitudSucursal { get; set; }
        public string? SiglasSucursal { get; set; }
        public string? CoSucursal { get; set; }
        public int? CodBancoRepublica { get; set; }
        public bool Estado { get; set; }
    }
}