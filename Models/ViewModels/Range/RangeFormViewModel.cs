using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VCashApp.Models.ViewModels.Range
{
    /// <summary>
    /// ViewModel para formularios de creación y edición de rangos de atencion.
    /// Contiene propiedades para los detalles del rango y horarios por día.
    /// </summary>
    public class RangeFormViewModel : IValidatableObject
    {
        /// <summary>
        /// Identificador del rango (nulo cuando es creación).
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Código único del rango.
        /// </summary>
        [Required, StringLength(50)]
        public string CodRange { get; set; } = null!;

        /// <summary>
        /// Identificador del cliente asociado.
        /// </summary>
        [Required, Range(1, int.MaxValue, ErrorMessage = "Seleccione un cliente.")]
        public int ClientId { get; set; }

        /// <summary>
        /// Información descriptiva del rango.
        /// </summary>
        [StringLength(450)]
        public string? RangeInformation { get; set; }

        // ===============================
        // Días de la semana y festivos
        // ===============================

        // Lunes
        [Required] public bool Monday { get; set; }
        [Required] public int MondayId { get; set; }
        public TimeSpan? Lr1Hi { get; set; }
        public TimeSpan? Lr1Hf { get; set; }
        public TimeSpan? Lr2Hi { get; set; }
        public TimeSpan? Lr2Hf { get; set; }
        public TimeSpan? Lr3Hi { get; set; }
        public TimeSpan? Lr3Hf { get; set; }

        // Martes
        [Required] public bool Tuesday { get; set; }
        [Required] public int TuesdayId { get; set; }
        public TimeSpan? Mr1Hi { get; set; }
        public TimeSpan? Mr1Hf { get; set; }
        public TimeSpan? Mr2Hi { get; set; }
        public TimeSpan? Mr2Hf { get; set; }
        public TimeSpan? Mr3Hi { get; set; }
        public TimeSpan? Mr3Hf { get; set; }

        // Miércoles
        [Required] public bool Wednesday { get; set; }
        [Required] public int WednesdayId { get; set; }
        public TimeSpan? Wr1Hi { get; set; }
        public TimeSpan? Wr1Hf { get; set; }
        public TimeSpan? Wr2Hi { get; set; }
        public TimeSpan? Wr2Hf { get; set; }
        public TimeSpan? Wr3Hi { get; set; }
        public TimeSpan? Wr3Hf { get; set; }

        // Jueves
        [Required] public bool Thursday { get; set; }
        [Required] public int ThursdayId { get; set; }
        public TimeSpan? Jr1Hi { get; set; }
        public TimeSpan? Jr1Hf { get; set; }
        public TimeSpan? Jr2Hi { get; set; }
        public TimeSpan? Jr2Hf { get; set; }
        public TimeSpan? Jr3Hi { get; set; }
        public TimeSpan? Jr3Hf { get; set; }

        // Viernes
        [Required] public bool Friday { get; set; }
        [Required] public int FridayId { get; set; }
        public TimeSpan? Vr1Hi { get; set; }
        public TimeSpan? Vr1Hf { get; set; }
        public TimeSpan? Vr2Hi { get; set; }
        public TimeSpan? Vr2Hf { get; set; }
        public TimeSpan? Vr3Hi { get; set; }
        public TimeSpan? Vr3Hf { get; set; }

        // Sábado
        [Required] public bool Saturday { get; set; }
        [Required] public int SaturdayId { get; set; }
        public TimeSpan? Sr1Hi { get; set; }
        public TimeSpan? Sr1Hf { get; set; }
        public TimeSpan? Sr2Hi { get; set; }
        public TimeSpan? Sr2Hf { get; set; }
        public TimeSpan? Sr3Hi { get; set; }
        public TimeSpan? Sr3Hf { get; set; }

        // Domingo
        [Required] public bool Sunday { get; set; }
        [Required] public int SundayId { get; set; }
        public TimeSpan? Dr1Hi { get; set; }
        public TimeSpan? Dr1Hf { get; set; }
        public TimeSpan? Dr2Hi { get; set; }
        public TimeSpan? Dr2Hf { get; set; }
        public TimeSpan? Dr3Hi { get; set; }
        public TimeSpan? Dr3Hf { get; set; }

        // Festivo
        [Required] public bool Holiday { get; set; }
        [Required] public int HolidayId { get; set; }
        public TimeSpan? Fr1Hi { get; set; }
        public TimeSpan? Fr1Hf { get; set; }
        public TimeSpan? Fr2Hi { get; set; }
        public TimeSpan? Fr2Hf { get; set; }
        public TimeSpan? Fr3Hi { get; set; }
        public TimeSpan? Fr3Hf { get; set; }

        /// <summary>
        /// Estado del rango (activo/inactivo).
        /// </summary>
        public bool RangeStatus { get; set; }

        /// <summary>
        /// Lista de clientes disponibles para mostrar en un dropdown.
        /// </summary>
        public List<SelectListItem> AvailableClients { get; set; } = new();

        /// <summary>
        /// Valida los horarios ingresados para cada día.
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            IEnumerable<ValidationResult> ValidateDay(
                string dayName,
                bool enabled,
                (TimeSpan? Hi, TimeSpan? Hf, string HiKey, string HfKey) r1,
                (TimeSpan? Hi, TimeSpan? Hf, string HiKey, string HfKey) r2,
                (TimeSpan? Hi, TimeSpan? Hf, string HiKey, string HfKey) r3)
            {
                var results = new List<ValidationResult>();
                if (!enabled)
                {
                    return results;
                }

                void CheckRange((TimeSpan? Hi, TimeSpan? Hf, string HiKey, string HfKey) r, int idx)
                {
                    var (hi, hf, hiKey, hfKey) = r;

                    if (hi.HasValue ^ hf.HasValue)
                    {
                        results.Add(new ValidationResult(
                            $"En {dayName}, Rango {idx}: al diligenciar una hora debe diligenciar la otra.",
                            new[] { hiKey, hfKey }));
                        return;
                    }

                    if (hi.HasValue && hf.HasValue)
                    {
                        if (hi.Value >= hf.Value)
                        {
                            results.Add(new ValidationResult(
                                $"En {dayName}, Rango {idx}: la hora inicial debe ser menor que la final.",
                                new[] { hiKey, hfKey }));
                        }
                    }
                }

                CheckRange(r1, 1);
                CheckRange(r2, 2);
                CheckRange(r3, 3);

                return results;
            }

            // Lunes
            foreach (var vr in ValidateDay("Lunes", Monday,
                (Lr1Hi, Lr1Hf, nameof(Lr1Hi), nameof(Lr1Hf)),
                (Lr2Hi, Lr2Hf, nameof(Lr2Hi), nameof(Lr2Hf)),
                (Lr3Hi, Lr3Hf, nameof(Lr3Hi), nameof(Lr3Hf)))) yield return vr;

            // Martes
            foreach (var vr in ValidateDay("Martes", Tuesday,
                (Mr1Hi, Mr1Hf, nameof(Mr1Hi), nameof(Mr1Hf)),
                (Mr2Hi, Mr2Hf, nameof(Mr2Hi), nameof(Mr2Hf)),
                (Mr3Hi, Mr3Hf, nameof(Mr3Hi), nameof(Mr3Hf)))) yield return vr;

            // Miércoles
            foreach (var vr in ValidateDay("Miércoles", Wednesday,
                (Wr1Hi, Wr1Hf, nameof(Wr1Hi), nameof(Wr1Hf)),
                (Wr2Hi, Wr2Hf, nameof(Wr2Hi), nameof(Wr2Hf)),
                (Wr3Hi, Wr3Hf, nameof(Wr3Hi), nameof(Wr3Hf)))) yield return vr;

            // Jueves
            foreach (var vr in ValidateDay("Jueves", Thursday,
                (Jr1Hi, Jr1Hf, nameof(Jr1Hi), nameof(Jr1Hf)),
                (Jr2Hi, Jr2Hf, nameof(Jr2Hi), nameof(Jr2Hf)),
                (Jr3Hi, Jr3Hf, nameof(Jr3Hi), nameof(Jr3Hf)))) yield return vr;

            // Viernes
            foreach (var vr in ValidateDay("Viernes", Friday,
                (Vr1Hi, Vr1Hf, nameof(Vr1Hi), nameof(Vr1Hf)),
                (Vr2Hi, Vr2Hf, nameof(Vr2Hi), nameof(Vr2Hf)),
                (Vr3Hi, Vr3Hf, nameof(Vr3Hi), nameof(Vr3Hf)))) yield return vr;

            // Sábado
            foreach (var vr in ValidateDay("Sábado", Saturday,
                (Sr1Hi, Sr1Hf, nameof(Sr1Hi), nameof(Sr1Hf)),
                (Sr2Hi, Sr2Hf, nameof(Sr2Hi), nameof(Sr2Hf)),
                (Sr3Hi, Sr3Hf, nameof(Sr3Hi), nameof(Sr3Hf)))) yield return vr;

            // Domingo
            foreach (var vr in ValidateDay("Domingo", Sunday,
                (Dr1Hi, Dr1Hf, nameof(Dr1Hi), nameof(Dr1Hf)),
                (Dr2Hi, Dr2Hf, nameof(Dr2Hi), nameof(Dr2Hf)),
                (Dr3Hi, Dr3Hf, nameof(Dr3Hi), nameof(Dr3Hf)))) yield return vr;

            // Festivo
            foreach (var vr in ValidateDay("Festivo", Holiday,
                (Fr1Hi, Fr1Hf, nameof(Fr1Hi), nameof(Fr1Hf)),
                (Fr2Hi, Fr2Hf, nameof(Fr2Hi), nameof(Fr2Hf)),
                (Fr3Hi, Fr3Hf, nameof(Fr3Hi), nameof(Fr3Hf)))) yield return vr;
        }
    }
}
