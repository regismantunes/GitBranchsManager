using Gbm.Services.Build;
using Gbm.Services.Git;
using Gbm.Services.Tasks;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.BranchesCommands;

public class SendTaskBranchesToDevelopCommand(IGitTool gitTool, IBuildTool dotnetTool)
{
    [CommandAsyncWithArgsBuilderAsync<BranchesCommandArgsBuilder>("-d", Description = "Merge mapped remote task branches into develop and push (skips sdk repositories)",
        Example = "gbm -d <TaskId> [nobuild] [Repos...]", Group = CommandGroups.Branches, Order = 5)]
    public async Task<int> ExecuteAsync(RepositoryBranchTarget[] targets, bool noBuild = false, CancellationToken cancellationToken = default)
    {
        await BranchExecution.ForEachAsync(targets.Where(t => !t.Repository.EndsWith("sdk", StringComparison.OrdinalIgnoreCase)), async target =>
        {
            await BranchExecution.CheckoutRequiredAsync(gitTool, target with { BranchName = "develop" }, cancellationToken);
            await gitTool.PullAsync(cancellationToken);
            await gitTool.PullOriginAsync(target.BranchName, cancellationToken);
            if (!noBuild && !await dotnetTool.BuildRepositoryAsync(target.Repository, cancellationToken))
                throw new InvalidOperationException($"Build failed in '{target.Repository}'; develop was not pushed in this repository.");
            await gitTool.PushAsync(cancellationToken);
        });
        return 0;
    }
}
