namespace VCashApp.Utils
{
    public record CefCaps(
        bool CanCountBills,
        bool CanCountCoins,
        bool CanIncCreateEdit,
        bool CanIncApprove,
        bool CanFinalize
    );
}