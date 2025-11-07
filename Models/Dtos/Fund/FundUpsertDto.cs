using System.ComponentModel.DataAnnotations;

namespace VCashApp.Models.Dtos.Fund
{
    public sealed class FundUpsertDto
    {
        [Required, StringLength(450)]
        public string FundCode { get; set; } = string.Empty;

        public int? VatcoFundCode { get; set; }

        public int? ClientCode { get; set; }

        [StringLength(255)]
        public string? FundName { get; set; }

        public int? BranchCode { get; set; }
        public int? CityCode { get; set; }

        // Para binding HTML5 date:
        [DataType(DataType.Date)]
        public DateTime? CreationDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? WithdrawalDate { get; set; }

        [StringLength(255)]
        public string? Cas4uCode { get; set; }

        [StringLength(50)]
        public string? FundCurrency { get; set; }

        public int? FundType { get; set; }

        public bool FundStatus { get; set; }
    }
}
