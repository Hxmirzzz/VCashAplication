namespace VCashApp.Infrastructure.Branches
{
    public interface IBranchContext
    {
        int? CurrentBranchId { get; }
        void SetBranch(int branchId);

    }

    public sealed class BranchContext : IBranchContext
    {
        public int? CurrentBranchId { get; private set; }
        public void SetBranch(int branchId) => CurrentBranchId = branchId;
    }
}