namespace Gbm.Services.Build
{
    public interface IBuildTool
    {
        Task<bool> BuildRepositoryAsync(string repo, string branchName, CancellationToken cancellationToken = default);
    }
}
