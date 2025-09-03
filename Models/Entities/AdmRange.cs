using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VCashApp.Models.Entities;

namespace VCashApp.Models.AdmEntities
{
    [Table("AdmRangos")]
    public class AdmRange
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        [Column("CodRango")]
        public string CodRange { get; set; } = null!;

        [Required]
        [Column("CodCliente")]
        [ForeignKey(nameof(Client))]
        public int ClientId { get; set; }
        public virtual AdmCliente Client { get; set; }

        [StringLength(450)]
        [Column("InformacionRango")]
        public string? RangeInformation { get; set; }

        // ===========================
        // LUNES
        // ===========================
        [Required]
        [Column("Lunes")]
        public bool Monday { get; set; }

        [Required]
        [Column("CodLunes")]
        public int MondayId { get; set; }

        public TimeSpan? Lr1Hi { get; set; }
        public TimeSpan? Lr1Hf { get; set; }
        public TimeSpan? Lr2Hi { get; set; }
        public TimeSpan? Lr2Hf { get; set; }
        public TimeSpan? Lr3Hi { get; set; }
        public TimeSpan? Lr3Hf { get; set; }

        // ===========================
        // MARTES
        // ===========================
        [Required]
        [Column("Martes")]
        public bool Tuesday { get; set; }

        [Required]
        [Column("CodMartes")]
        public int TuesdayId { get; set; }

        public TimeSpan? Mr1Hi { get; set; }
        public TimeSpan? Mr1Hf { get; set; }
        public TimeSpan? Mr2Hi { get; set; }
        public TimeSpan? Mr2Hf { get; set; }
        public TimeSpan? Mr3Hi { get; set; }
        public TimeSpan? Mr3Hf { get; set; }

        // ===========================
        // MIÉRCOLES
        // ===========================
        [Required]
        [Column("Miercoles")]
        public bool Wednesday { get; set; }

        [Required]
        [Column("CodMiercoles")]
        public int WednesdayId { get; set; }

        public TimeSpan? Wr1Hi { get; set; }
        public TimeSpan? Wr1Hf { get; set; }
        public TimeSpan? Wr2Hi { get; set; }
        public TimeSpan? Wr2Hf { get; set; }
        public TimeSpan? Wr3Hi { get; set; }
        public TimeSpan? Wr3Hf { get; set; }

        // ===========================
        // JUEVES
        // ===========================
        [Required]
        [Column("Jueves")]
        public bool Thursday { get; set; }

        [Required]
        [Column("CodJueves")]
        public int ThursdayId { get; set; }

        public TimeSpan? Jr1Hi { get; set; }
        public TimeSpan? Jr1Hf { get; set; }
        public TimeSpan? Jr2Hi { get; set; }
        public TimeSpan? Jr2Hf { get; set; }
        public TimeSpan? Jr3Hi { get; set; }
        public TimeSpan? Jr3Hf { get; set; }

        // ===========================
        // VIERNES
        // ===========================
        [Required]
        [Column("Viernes")]
        public bool Friday { get; set; }

        [Required]
        [Column("CodViernes")]
        public int FridayId { get; set; }

        public TimeSpan? Vr1Hi { get; set; }
        public TimeSpan? Vr1Hf { get; set; }
        public TimeSpan? Vr2Hi { get; set; }
        public TimeSpan? Vr2Hf { get; set; }
        public TimeSpan? Vr3Hi { get; set; }
        public TimeSpan? Vr3Hf { get; set; }

        // ===========================
        // SÁBADO
        // ===========================
        [Required]
        [Column("Sabado")]
        public bool Saturday { get; set; }

        [Required]
        [Column("CodSabado")]
        public int SaturdayId { get; set; }

        public TimeSpan? Sr1Hi { get; set; }
        public TimeSpan? Sr1Hf { get; set; }
        public TimeSpan? Sr2Hi { get; set; }
        public TimeSpan? Sr2Hf { get; set; }
        public TimeSpan? Sr3Hi { get; set; }
        public TimeSpan? Sr3Hf { get; set; }

        // ===========================
        // DOMINGO
        // ===========================
        [Required]
        [Column("Domingo")]
        public bool Sunday { get; set; }

        [Required]
        [Column("CodDomingo")]
        public int SundayId { get; set; }

        public TimeSpan? Dr1Hi { get; set; }
        public TimeSpan? Dr1Hf { get; set; }
        public TimeSpan? Dr2Hi { get; set; }
        public TimeSpan? Dr2Hf { get; set; }
        public TimeSpan? Dr3Hi { get; set; }
        public TimeSpan? Dr3Hf { get; set; }

        // ===========================
        // FESTIVO
        // ===========================
        [Required]
        [Column("Festivo")]
        public bool Holiday { get; set; }

        [Required]
        [Column("CodFestivo")]
        public int HolidayId { get; set; }

        public TimeSpan? Fr1Hi { get; set; }
        public TimeSpan? Fr1Hf { get; set; }
        public TimeSpan? Fr2Hi { get; set; }
        public TimeSpan? Fr2Hf { get; set; }
        public TimeSpan? Fr3Hi { get; set; }
        public TimeSpan? Fr3Hf { get; set; }

        public bool RangeStatus { get; set; }
    }
}
