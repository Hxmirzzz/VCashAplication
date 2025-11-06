namespace VCashApp.Models.DTOs.Range
{
    public class RangeListDto
    {
        public int Id { get; set; }
        public string CodRange { get; set; } = string.Empty;
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string? RangeInformation { get; set; }
        public bool RangeStatus { get; set; }
        public string ActiveDays { get; set; } = string.Empty;
    }
}