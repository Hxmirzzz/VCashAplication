namespace VCashApp.Services.CentroEfectivo.Provision.Domain
{
    /// <summary>Tolerancia absoluta (por defecto 0).</summary>
    public sealed class ZeroTolerancePolicy : ITolerancePolicy
    {
        private readonly decimal _tol;
        public ZeroTolerancePolicy(decimal tol = 0m) { _tol = tol; }
        public bool IsWithinTolerance(decimal declared, decimal counted) =>
            Math.Abs(declared - counted) <= _tol;
    }
}