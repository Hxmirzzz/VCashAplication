namespace VCashApp.Models.Dtos.Point
{
    public sealed class PointFilterDto
    {
        public string? Search { get; set; }
        public int? ClientCode { get; set; }
        public int? MainClientCode { get; set; }
        public int? BranchCode { get; set; }
        public int? CityCode { get; set; }
        public string? FundCode { get; set; }
        public bool? IsActive { get; set; }
        public string? RouteCode { get; set; }
        public int? RangeCode { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
    }
}
