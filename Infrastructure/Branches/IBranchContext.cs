namespace VCashApp.Infrastructure.Branches
{
    public interface IBranchContext
    {
        int? CurrentBranchId { get; }
        bool AllBranches { get; }
        IReadOnlyList<int> PermittedBranchIds { get; }

        void SetBranch(int branchId);
        void SetAllBranches(IEnumerable<int> ids);
        void Clear();
    }

    public sealed class BranchContext : IBranchContext
    {
        public int? CurrentBranchId { get; private set; }
        public bool AllBranches { get; private set; }
        private List<int> _permitted = new();
        public IReadOnlyList<int> PermittedBranchIds => _permitted;

        public void SetBranch(int branchId)
        {
            AllBranches = false;
            CurrentBranchId = branchId;
            _permitted.Clear();
        }

        public void SetAllBranches(IEnumerable<int> ids)
        {
            CurrentBranchId = null;
            AllBranches = true;
            _permitted = ids?.Distinct().ToList() ?? new List<int>();
        }

        public void Clear()
        {
            CurrentBranchId = null;
            AllBranches = false;
            _permitted.Clear();
        }
    }
}