namespace VCashApp.Services.CentroEfectivo.Collection.Domain
{
    public interface ICollectionStateMachine
    {
        void EnsureCanMove(string current, string next, int txId);
    }
}
