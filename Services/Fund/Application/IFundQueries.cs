using VCashApp.Models.Dtos.Fund;

namespace VCashApp.Services.Fund.Application
{
    public interface IFundQueries
    {
        Task<(IReadOnlyList<FundListDto> Items, int Total, int Page, int PageSize)>
            GetPagedAsync(FundFilterDto filter);
        Task<FundUpsertDto?> GetForEditAsync(string fundCode);
        Task<FundUpsertDto?> GetForPreviewAsync(string fundCode);
        Task<IReadOnlyList<FundExportDto>> ExportAsync(FundFilterDto filter);
        Task<FundLookupDto> GetLookupsAsync();
    }
}
