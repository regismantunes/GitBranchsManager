using Gbm.Persistence.Configuration;
using Gbm.Persistence.Entities;
using Gbm.Persistence.Repositories.Interfaces;
using Gbm.Services.Configuration;
using Gbm.Services.Git;
using Microsoft.Extensions.Configuration;
using RA.Console.DependencyInjection.Args;

namespace Gbm.Services.Tasks;

public sealed class TaskBranchNameFactory(IConfiguration configuration)
{
    public string Create(TaskInfo task)
    {
        var format = configuration.GetValue(ConfigurationVariable.BranchDefaultNameFormat);
        if (string.IsNullOrWhiteSpace(format))
            throw new InvalidOperationException("No branch format configured. Use -bn or -t -branch with repositories.");
        return format.Replace("{TaskId}", task.Id).Replace("{TaskSummary}", task.Summary);
    }
}

public sealed class TaskBranchCreator(IGitTool gitTool, ITaskInfoRepository repository, RepositoryCatalog catalog,
    TaskBranchNameFactory nameFactory) : ITaskBranchCreator
{
    public async Task CreateAsync(TaskInfo task, string[] repositories, string? explicitBranch = null, CancellationToken cancellationToken = default)
    {
        if (repositories.Length == 0) throw new ArgsValidationException("At least one repository is required for branch creation.");
        var repos = await catalog.ValidateAsync(repositories, cancellationToken);
        var targets = repos.Select(repo => new RepositoryBranch(repo, explicitBranch ??
            task.Repositories.Find(r => r.Repository.Equals(repo, StringComparison.OrdinalIgnoreCase))?.BranchName ?? nameFactory.Create(task))).ToArray();
        foreach (var branch in targets.Select(t => t.BranchName).Distinct(StringComparer.Ordinal))
            await gitTool.ValidateBranchNameAsync(branch, cancellationToken);
        var completed = new List<string>();
        foreach (var target in targets)
        {
            try
            {
                await gitTool.SetRepositoryAsync(target.Repository, cancellationToken);
                MyConsole.WriteStep($"🌱 {target.Repository}: creating or checking out '{target.BranchName}'.");
                if (await gitTool.BranchExistsAsync(target.BranchName, cancellationToken))
                {
                    if (!await gitTool.CheckoutAsync(target.BranchName, cancellationToken))
                        throw new InvalidOperationException("Branch disappeared before checkout.");
                }
                else if ((await gitTool.GetRemoteBranchNamesAsync(target.Repository, cancellationToken)).BranchNames.Contains(target.BranchName, StringComparer.Ordinal))
                    await gitTool.CheckoutRemoteBranchAsync(target.BranchName, cancellationToken);
                else
                {
                    await gitTool.CheckoutToMainAsync(cancellationToken);
                    await gitTool.PullAsync(cancellationToken);
                    await gitTool.CheckoutNewBranchAsync(target.BranchName, cancellationToken);
                }
                if (await gitTool.GetCurrentBranchAsync(cancellationToken) != target.BranchName)
                    throw new InvalidOperationException("Git did not confirm the requested branch after creation/checkout.");
                await repository.UpsertRepositoryBranchAsync(task.Id, target, cancellationToken);
                completed.Add(target.Repository);
            }
            catch
            {
                MyConsole.WriteError($"❌ Creation/mapping failed for {target.Repository} -> {target.BranchName}. Git may already have changed; retry to confirm the mapping. Completed mappings: {string.Join(", ", completed)}.");
                throw;
            }
        }
    }
}
