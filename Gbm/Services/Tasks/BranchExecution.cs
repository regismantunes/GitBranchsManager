using Gbm.Services.Git;

namespace Gbm.Services.Tasks;

public static class BranchExecution
{
    public static async Task CheckoutRequiredAsync(IGitTool git, RepositoryBranchTarget target, CancellationToken cancellationToken)
    {
        await git.SetRepositoryAsync(target.Repository, cancellationToken);
        if (!await git.CheckoutAsync(target.BranchName, cancellationToken))
            throw new InvalidOperationException($"Checkout failed: {target.Repository} -> {target.BranchName}. No further operation was performed in this repository.");
    }

    public static async Task ForEachAsync(IEnumerable<RepositoryBranchTarget> targets, Func<RepositoryBranchTarget, Task> action)
    {
        var completed = new List<string>();
        foreach (var target in targets)
        {
            try
            {
                MyConsole.WriteStep($"⚙️ {target.Repository} -> {target.BranchName}");
                await action(target);
                completed.Add(target.Repository);
            }
            catch
            {
                MyConsole.WriteError($"❌ Failed in '{target.Repository}'. Completed repositories: {string.Join(", ", completed)}. Changes already performed were not rolled back.");
                throw;
            }
        }
    }
}
