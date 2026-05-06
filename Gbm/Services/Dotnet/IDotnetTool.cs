namespace Gbm.Services.Dotnet
{
    public interface IDotnetTool
    {
        Task<bool> BuildRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default);
    }
}
