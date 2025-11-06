namespace VCashApp.Models.DTOs.Range
{
    public class RangeFilterDto
    {
        public string? Search { get; set; }
        public int? ClientId { get; set; }
        public bool? RangeStatus { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
    }
}