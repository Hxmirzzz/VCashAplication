namespace VCashApp.Services.CentroEfectivo.Provision.Domain
{
    public interface IProvisionStateMachine
    {
        /// <summary>Valida si current → next es permitida.</summary>
        void EnsureCanMove(string current, string next, int txId);
    }
}