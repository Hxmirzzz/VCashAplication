namespace VCashApp.Models.Dtos.Fund
{
    public sealed class FundExportDto
    {
        public string FundCode { get; init; } = string.Empty;
        public int? VatcoFundCode { get; init; }
        public string? FundName { get; init; }
        public string? ClientName { get; init; }
        public string? BranchName { get; init; }
        public string? CityName { get; init; }
        public string? FundCurrency { get; init; }
        public int? FundType { get; init; }
        public DateOnly? CreationDate { get; init; }
        public DateOnly? WithdrawalDate { get; init; }
        public bool FundStatus { get; init; }
    }
}
