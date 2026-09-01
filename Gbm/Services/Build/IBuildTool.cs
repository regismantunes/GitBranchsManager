namespace Gbm.Services.Build
{
    public interface IBuildTool
    {
        Task<bool> BuildBranchAsync(string repo, string branchName, CancellationToken cancellationToken = default);
        Task<bool> BuildRepositoryAsync(string repo, CancellationToken cancellationToken = default);
    }
}
