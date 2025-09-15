namespace VCashApp.Extensions
{
    using Microsoft.AspNetCore.Http;

    public static class HttpRequestExtensions
    {
        public static bool IsAjaxRequest(this HttpRequest request)
        {
            if (request is null) return false;
            if (!request.Headers.TryGetValue("X-Requested-With", out var v)) return false;
            return string.Equals(v.ToString(), "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        }
    }
}