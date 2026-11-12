using Gbm.Persistence.Entities;
using Gbm.Services.Git;

namespace Gbm.Services.Tasks;

public sealed class TaskBranchDiscovery(IGitTool gitTool, RepositoryCatalog catalog, IBranchSelection selection) : ITaskBranchDiscovery
{
    public async Task<List<RepositoryBranch>> DiscoverAsync(string taskId, CancellationToken cancellationToken = default)
    {
        var result = new List<RepositoryBranch>();
        foreach (var repo in await catalog.GetAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var remote = await gitTool.GetRemoteBranchNamesAsync(repo, cancellationToken);
            if (!remote.HasOrigin)
            {
                MyConsole.WriteInfo($"{repo}: no origin remote; no mapping will be retained.");
                continue;
            }
            var candidates = remote.BranchNames.Where(b => b.Contains(taskId, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            if (candidates.Length == 0)
            {
                MyConsole.WriteInfo($"{repo}: no remote branch contains '{taskId}'.");
                continue;
            }
            var branch = candidates.Length == 1 ? candidates[0] : await selection.SelectAsync(repo, candidates, cancellationToken);
            if (!candidates.Contains(branch, StringComparer.Ordinal)) throw new InvalidOperationException("Selection was not a candidate.");
            result.Add(new(repo, branch));
        }
        return result;
    }
}

public sealed class ConsoleBranchSelection : IBranchSelection
{
    public async Task<string> SelectAsync(string repository, IReadOnlyList<string> candidates, CancellationToken cancellationToken = default)
    {
        MyConsole.WriteStep($"Select the branch for '{repository}':");
        for (var i = 0; i < candidates.Count; i++) MyConsole.WriteInfo($"{i + 1} - {candidates[i]}");
        MyConsole.WriteInfo("0 - cancel the entire operation");
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var input = await MyConsole.ReadLineOrEndAsync(cancellationToken);
            if (input is null || input.Trim() == "0") throw new OperationCanceledException("Branch selection cancelled; no mappings were saved.");
            if (int.TryParse(input, out var index) && index >= 1 && index <= candidates.Count) return candidates[index - 1];
            MyConsole.WriteError("Invalid selection. Enter one of the numbers above.");
        }
    }
}
