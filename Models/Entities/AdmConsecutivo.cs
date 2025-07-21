using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Para Column

namespace VCashApp.Models.Entities
{
    public class AdmConsecutivo
    {
        [Key]
        public string TipoConsecutivo { get; set; }

        public string? NombreConsecutivo { get; set; }

        public string? LetraConsecutivo { get; set; }

        public long? Inicio { get; set; }
        public long? Fin { get; set; }
        public long? ConsecutivoActual { get; set; }
    }
}