using System; // Para DateOnly, TimeSpan
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic; // Para ICollection

namespace VCashApp.Models.Entities
{
    public class AdmRuta
    {
        [Key]
        public string CodRutaSuc { get; set; } // PK

        public string? CodRuta { get; set; }
        public string? NombreRuta { get; set; }

        public int? CodSucursal { get; set; } // FK a AdmSucursal
        [ForeignKey("CodSucursal")]
        public virtual AdmSucursal? Sucursal { get; set; }

        public string? TipoRuta { get; set; }
        public string? TipoAtencion { get; set; }
        public string? TipoVehiculo { get; set; }

        public decimal? Monto { get; set; }

        public bool Lunes { get; set; }
        public TimeSpan? LunesHoraInicio { get; set; }
        public TimeSpan? LunesHoraFin { get; set; }

        public bool Martes { get; set; }
        public TimeSpan? MartesHoraInicio { get; set; }
        public TimeSpan? MartesHoraFin { get; set; }

        public bool Miercoles { get; set; }
        public TimeSpan? MiercolesHoraInicio { get; set; }
        public TimeSpan? MiercolesHoraFin { get; set; }

        public bool Jueves { get; set; }
        public TimeSpan? JuevesHoraInicio { get; set; }
        public TimeSpan? JuevesHoraFin { get; set; }

        public bool Viernes { get; set; }
        public TimeSpan? ViernesHoraInicio { get; set; }
        public TimeSpan? ViernesHoraFin { get; set; }

        public bool Sabado { get; set; }
        public TimeSpan? SabadoHoraInicio { get; set; }
        public TimeSpan? SabadoHoraFin { get; set; }

        public bool Domingo { get; set; }
        public TimeSpan? DomingoHoraInicio { get; set; }
        public TimeSpan? DomingoHoraFin { get; set; }

        public bool Festivo { get; set; }
        public TimeSpan? FestivoHoraInicio { get; set; }
        public TimeSpan? FestivoHoraFin { get; set; }
        public bool EstadoRuta { get; set; }
    }
}