using System.ComponentModel.DataAnnotations;

namespace VCashApp.Models.Dtos.Fund
{
    public sealed class FundFilterDto
    {
        public int? ClientCode { get; set; }
        public int? BranchCode { get; set; }
        public int? CityCode { get; set; }
        public bool? FundStatus { get; set; }
        public int? FundType { get; set; }

        [StringLength(200)]
        public string? Search { get; set; }

        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 200)]
        public int PageSize { get; set; } = 15;
    }
}
