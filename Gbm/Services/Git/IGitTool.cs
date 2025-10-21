namespace Gbm.Services.Git
{
    public interface IGitTool
    {
        string BasePath { get; set; }
        bool ShowGitOutput { get; set; }
        string? WorkingDirectory { get; }

        Task<bool> BranchExistsAsync(string branch, CancellationToken cancellationToken = default);
        Task<bool> BranchExistsRemotelyAsync(string branch, CancellationToken cancellationToken = default);
        Task CheckoutAsync(string branch, CancellationToken cancellationToken = default);
        Task CheckoutNewBranchAsync(string branch, CancellationToken cancellationToken = default);
        Task CheckoutToMainAsync(CancellationToken cancellationToken = default);
        Task DeleteLocalBranchAsync(string branch, CancellationToken cancellationToken = default);
        Task<string> GetCurrentBranchAsync(CancellationToken cancellationToken = default);
        Task<string> GetMainBranchAsync(CancellationToken cancellationToken = default);
        Task GetMainChangesAsync(CancellationToken cancellationToken = default);
        Task<bool> HasUncommittedChangesAsync(CancellationToken cancellationToken = default);
        Task PullAsync(CancellationToken cancellationToken = default);
        Task PullOriginAsync(string branchFrom, CancellationToken cancellationToken = default);
        Task PushAsync(CancellationToken cancellationToken = default);
        void SetRepository(string repository);
        Task<IEnumerable<string>> GetRepositoriesWithBranchAsync(string branch, CancellationToken cancellationToken = default);
    }
}