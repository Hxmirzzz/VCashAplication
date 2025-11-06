namespace VCashApp.Models.DTOs.Range
{
    public class RangeUpsertDto
    {
        public int? Id { get; set; }
        public string CodRange { get; set; } = string.Empty;
        public int ClientId { get; set; }
        public string? RangeInformation { get; set; }

        public bool Monday { get; set; }
        public int MondayId { get; set; }
        public TimeSpan? Lr1Hi { get; set; }
        public TimeSpan? Lr1Hf { get; set; }
        public TimeSpan? Lr2Hi { get; set; }
        public TimeSpan? Lr2Hf { get; set; }
        public TimeSpan? Lr3Hi { get; set; }
        public TimeSpan? Lr3Hf { get; set; }

        public bool Tuesday { get; set; }
        public int TuesdayId { get; set; }
        public TimeSpan? Mr1Hi { get; set; }
        public TimeSpan? Mr1Hf { get; set; }
        public TimeSpan? Mr2Hi { get; set; }
        public TimeSpan? Mr2Hf { get; set; }
        public TimeSpan? Mr3Hi { get; set; }
        public TimeSpan? Mr3Hf { get; set; }

        public bool Wednesday { get; set; }
        public int WednesdayId { get; set; }
        public TimeSpan? Wr1Hi { get; set; }
        public TimeSpan? Wr1Hf { get; set; }
        public TimeSpan? Wr2Hi { get; set; }
        public TimeSpan? Wr2Hf { get; set; }
        public TimeSpan? Wr3Hi { get; set; }
        public TimeSpan? Wr3Hf { get; set; }

        public bool Thursday { get; set; }
        public int ThursdayId { get; set; }
        public TimeSpan? Jr1Hi { get; set; }
        public TimeSpan? Jr1Hf { get; set; }
        public TimeSpan? Jr2Hi { get; set; }
        public TimeSpan? Jr2Hf { get; set; }
        public TimeSpan? Jr3Hi { get; set; }
        public TimeSpan? Jr3Hf { get; set; }

        public bool Friday { get; set; }
        public int FridayId { get; set; }
        public TimeSpan? Vr1Hi { get; set; }
        public TimeSpan? Vr1Hf { get; set; }
        public TimeSpan? Vr2Hi { get; set; }
        public TimeSpan? Vr2Hf { get; set; }
        public TimeSpan? Vr3Hi { get; set; }
        public TimeSpan? Vr3Hf { get; set; }

        public bool Saturday { get; set; }
        public int SaturdayId { get; set; }
        public TimeSpan? Sr1Hi { get; set; }
        public TimeSpan? Sr1Hf { get; set; }
        public TimeSpan? Sr2Hi { get; set; }
        public TimeSpan? Sr2Hf { get; set; }
        public TimeSpan? Sr3Hi { get; set; }
        public TimeSpan? Sr3Hf { get; set; }

        public bool Sunday { get; set; }
        public int SundayId { get; set; }
        public TimeSpan? Dr1Hi { get; set; }
        public TimeSpan? Dr1Hf { get; set; }
        public TimeSpan? Dr2Hi { get; set; }
        public TimeSpan? Dr2Hf { get; set; }
        public TimeSpan? Dr3Hi { get; set; }
        public TimeSpan? Dr3Hf { get; set; }

        public bool Holiday { get; set; }
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