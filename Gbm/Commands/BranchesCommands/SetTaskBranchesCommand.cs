using Gbm.Services.Git;
using Gbm.Services.Tasks;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.BranchesCommands;

public class SetTaskBranchesCommand(IGitTool gitTool)
{
    [CommandAsyncWithArgsBuilderAsync<BranchesCommandArgsBuilder>("-s", Description = "Checkout task branches or a literal branch and pull",
        Example = "gbm -s <TaskId/Branch> [Repos...] | gbm -s --branch <Branch> [Repos...]", Group = CommandGroups.Branches, Order = 2)]
    public async Task<int> ExecuteAsync(RepositoryBranchTarget[] targets, CancellationToken cancellationToken = default)
    {
        await BranchExecution.ForEachAsync(targets, async target =>
        {
            await gitTool.SetRepositoryAsync(target.Repository, cancellationToken);
            if (!await gitTool.CheckoutAsync(target.BranchName, cancellationToken))
            {
                var remote = await gitTool.GetRemoteBranchNamesAsync(target.Repository, cancellationToken);
                if (!remote.BranchNames.Contains(target.BranchName, StringComparer.Ordinal))
                    throw new InvalidOperationException($"Branch '{target.BranchName}' is not available in origin for '{target.Repository}'.");
                await gitTool.CheckoutRemoteBranchAsync(target.BranchName, cancellationToken);
            }
            if (await gitTool.GetCurrentBranchAsync(cancellationToken) != target.BranchName)
                throw new InvalidOperationException("Checkout did not confirm the requested branch; pull was not performed.");
            await gitTool.PullAsync(cancellationToken);
        });
        return 0;
    }
}
