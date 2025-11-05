using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace VCashApp.Models.Entities
{
    [Table("AdmRutas")]
    public class AdmRoute
    {
        [Key]
        [Column("CodRutaSuc")]
        public string BranchRouteCode { get; set; }

        [Column("CodRuta")]
        public string? RouteCode { get; set; }

        [Column("NombreRuta")]
        public string? RouteName { get; set; }

        [Column("CodSucursal")]
        public int? BranchId { get; set; }

        [ForeignKey("BranchId")]
        public virtual AdmSucursal? Branch { get; set; }

        [Column("TipoRuta")]
        public string? RouteType { get; set; }

        [Column("TipoAtencion")]
        public string? ServiceType { get; set; }

        [Column("TipoVehiculo")]
        public string? VehicleType { get; set; }

        [Column("Monto")]
        public decimal? Amount { get; set; }

        [Column("Lunes")]
        public bool Monday { get; set; }

        [Column("LunesHoraInicio")]
        public TimeSpan? MondayStartTime { get; set; }

        [Column("LunesHoraFin")]
        public TimeSpan? MondayEndTime { get; set; }

        [Column("Martes")]
        public bool Tuesday { get; set; }

        [Column("MartesHoraInicio")]
        public TimeSpan? TuesdayStartTime { get; set; }

        [Column("MartesHoraFin")]
        public TimeSpan? TuesdayEndTime { get; set; }

        [Column("Miercoles")]
        public bool Wednesday { get; set; }

        [Column("MiercolesHoraInicio")]
        public TimeSpan? WednesdayStartTime { get; set; }

        [Column("MiercolesHoraFin")]
        public TimeSpan? WednesdayEndTime { get; set; }

        [Column("Jueves")]
        public bool Thursday { get; set; }

        [Column("JuevesHoraInicio")]
        public TimeSpan? ThursdayStartTime { get; set; }

        [Column("JuevesHoraFin")]
        public TimeSpan? ThursdayEndTime { get; set; }

        [Column("Viernes")]
        public bool Friday { get; set; }

        [Column("ViernesHoraInicio")]
        public TimeSpan? FridayStartTime { get; set; }

        [Column("ViernesHoraFin")]
        public TimeSpan? FridayEndTime { get; set; }

        [Column("Sabado")]
        public bool Saturday { get; set; }

        [Column("SabadoHoraInicio")]
        public TimeSpan? SaturdayStartTime { get; set; }

        [Column("SabadoHoraFin")]
        public TimeSpan? SaturdayEndTime { get; set; }

        [Column("Domingo")]
        public bool Sunday { get; set; }

        [Column("DomingoHoraInicio")]
        public TimeSpan? SundayStartTime { get; set; }

        [Column("DomingoHoraFin")]
        public TimeSpan? SundayEndTime { get; set; }

        [Column("Festivo")]
        public bool Holiday { get; set; }

        [Column("FestivoHoraInicio")]
        public TimeSpan? HolidayStartTime { get; set; }

        [Column("FestivoHoraFin")]
        public TimeSpan? HolidayEndTime { get; set; }

        [Column("EstadoRuta")]
        public bool Status { get; set; }
    }
}