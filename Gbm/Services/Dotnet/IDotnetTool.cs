namespace Gbm.Services.Dotnet
{
    public interface IDotnetTool
    {
        Task<bool> BuildRepositoryAsync(string repo, string branchName, CancellationToken cancellationToken = default);
    }
}
