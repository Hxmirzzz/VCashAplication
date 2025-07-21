using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCashApp.Models.AdmEntities
{
    public class AdmRango
    {
        [Key]
        public string CodRango { get; set; } = null!;
        public int? CodigoCliente { get; set; }
        [ForeignKey("CodigoCliente")]
        //public virtual AdmCliente? Cliente { get; set; }
        public string? InfoRangoAtencion { get; set; }

        public string? Lunes { get; set; }
        public int? CodLunes { get; set; }
        public TimeSpan? Lr1Hi { get; set; }
        public TimeSpan? Lr1Hf { get; set; }
        public TimeSpan? Lr2Hi { get; set; }
        public TimeSpan? Lr2Hf { get; set; }
        public TimeSpan? Lr3Hi { get; set; }
        public TimeSpan? Lr3Hf { get; set; }

        public string? Martes { get; set; }
        public int? CodMartes { get; set; }
        public TimeSpan? Mr1Hi { get; set; }
        public TimeSpan? Mr1Hf { get; set; }
        public TimeSpan? Mr2Hi { get; set; }
        public TimeSpan? Mr2Hf { get; set; }
        public TimeSpan? Mr3Hi { get; set; }
        public TimeSpan? Mr3Hf { get; set; }

        public string? Miercoles { get; set; }
        public int? CodMiercoles { get; set; }
        public TimeSpan? Wr1Hi { get; set; }
        public TimeSpan? Wr1Hf { get; set; }
        public TimeSpan? Wr2Hi { get; set; }
        public TimeSpan? Wr2Hf { get; set; }
        public TimeSpan? Wr3Hi { get; set; }
        public TimeSpan? Wr3Hf { get; set; }

        public string? Jueves { get; set; }
        public int? CodJueves { get; set; }
        public TimeSpan? Jr1Hi { get; set; }
        public TimeSpan? Jr1Hf { get; set; }
        public TimeSpan? Jr2Hi { get; set; }
        public TimeSpan? Jr2Hf { get; set; }
        public TimeSpan? Jr3Hi { get; set; }
        public TimeSpan? Jr3Hf { get; set; }

        public string? Viernes { get; set; }
        public int? CodViernes { get; set; }
        public TimeSpan? Vr1Hi { get; set; }
        public TimeSpan? Vr1Hf { get; set; }
        public TimeSpan? Vr2Hi { get; set; }
        public TimeSpan? Vr2Hf { get; set; }
        public TimeSpan? Vr3Hi { get; set; }
        public TimeSpan? Vr3Hf { get; set; }

        public string? Sabado { get; set; }
        public int? CodSabado { get; set; }
        public TimeSpan? Sr1Hi { get; set; }
        public TimeSpan? Sr1Hf { get; set; }
        public TimeSpan? Sr2Hi { get; set; }
        public TimeSpan? Sr2Hf { get; set; }
        public TimeSpan? Sr3Hi { get; set; }
        public TimeSpan? Sr3Hf { get; set; }

        public string? Domingo { get; set; }
        public int? CodDomingo { get; set; }
        public TimeSpan? Dr1Hi { get; set; }
        public TimeSpan? Dr1Hf { get; set; }
        public TimeSpan? Dr2Hi { get; set; }
        public TimeSpan? Dr2Hf { get; set; }
        public TimeSpan? Dr3Hi { get; set; }
        public TimeSpan? Dr3Hf { get; set; }

        public string? Festivo { get; set; }
        public int? CodFestivo { get; set; }
        public TimeSpan? Fr1Hi { get; set; }
        public TimeSpan? Fr1Hf { get; set; }
        public TimeSpan? Fr2Hi { get; set; }
        public TimeSpan? Fr2Hf { get; set; }
        public TimeSpan? Fr3Hi { get; set; }
        public TimeSpan? Fr3Hf { get; set; }

        public string? ConcatenadoRango { get; set; }
        public int? RangoEstado { get; set; }
    }
}
