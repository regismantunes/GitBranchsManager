using Gbm.Services.Git;
using RA.Console.DependencyInjection.Args;

namespace Gbm.Services.Tasks;

public sealed class TaskBranchTargetResolver(IGitTool gitTool, RepositoryCatalog catalog) : ITaskBranchTargetResolver
{
    public async Task<RepositoryBranchTarget[]> ResolveAsync(TaskBranchReference reference, string[] repositories, string command,
        CancellationToken cancellationToken = default, IReadOnlySet<string>? existingPrRepositories = null)
    {
        var inventory = await catalog.GetAsync(cancellationToken);
        var explicitRepos = repositories.Length > 0;
        var selected = explicitRepos ? RepositoryCatalog.Validate(repositories, inventory)
            : reference.Task is null ? inventory : reference.Task.Repositories.Select(r => r.Repository).ToArray();
        var targets = new List<RepositoryBranchTarget>();
        var skippedSdk = 0;
        foreach (var requested in selected)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (command == "-d" && requested.EndsWith("sdk", StringComparison.OrdinalIgnoreCase))
            {
                MyConsole.WriteInfo($"{requested}: skipped by the sdk exclusion rule.");
                skippedSdk++;
                continue;
            }
            var repo = inventory.FirstOrDefault(r => r.Equals(requested, StringComparison.OrdinalIgnoreCase));
            if (repo is null) { MyConsole.WriteInfo($"{requested}: mapped repository is missing; skipping."); continue; }
            var branch = reference.Task is null ? reference.Literal
                : reference.Task.Repositories.Find(r => r.Repository.Equals(repo, StringComparison.OrdinalIgnoreCase))?.BranchName;
            if (branch is null) throw new ArgsValidationException($"Task '{reference.Task?.Id}' has no mapping for '{repo}'. Use -n or -rb.");
            await gitTool.SetRepositoryAsync(repo, cancellationToken);
            if (reference.Task is null && reference.MainAlias) branch = await gitTool.GetMainBranchAsync(cancellationToken);

            var available = command == "-pr" && existingPrRepositories?.Contains(repo) == true;
            if (!available) available = await gitTool.BranchExistsAsync(branch, cancellationToken);
            if ((!available && command == "-s") || (available && command == "-d"))
                available = (await gitTool.GetRemoteBranchNamesAsync(repo, cancellationToken)).BranchNames.Contains(branch, StringComparer.Ordinal);
            if (!available)
            {
                var message = $"{repo}: branch '{branch}' is not available for {command}. Use -s for a remote-only branch, or -rb to refresh mappings.";
                if (explicitRepos) throw new ArgsValidationException(message);
                MyConsole.WriteInfo(message);
                continue;
            }
            targets.Add(new(repo, branch));
        }
        if (targets.Count == 0 && !(selected.Length > 0 && skippedSdk == selected.Length))
            throw new ArgsValidationException("No eligible repository branches were found. Use -n, -s or -rb as appropriate.");
        return targets.ToArray();
    }

    public async Task<RepositoryBranchTarget[]> WithOriginAsync(RepositoryBranchTarget[] targets, TaskBranchReference origin, CancellationToken cancellationToken = default)
    {
        var result = new List<RepositoryBranchTarget>();
        foreach (var target in targets)
        {
            var branch = origin.Task is null ? origin.Literal
                : origin.Task.Repositories.Find(r => r.Repository.Equals(target.Repository, StringComparison.OrdinalIgnoreCase))?.BranchName;
            if (branch is null) throw new ArgsValidationException($"Origin task '{origin.Task?.Id}' has no mapping for '{target.Repository}'.");
            var remote = await gitTool.GetRemoteBranchNamesAsync(target.Repository, cancellationToken);
            if (!remote.BranchNames.Contains(branch, StringComparer.Ordinal))
                throw new ArgsValidationException($"Origin branch '{branch}' is not available in origin for '{target.Repository}'.");
            result.Add(target with { OriginBranchName = branch });
        }
        return result.ToArray();
    }
}
