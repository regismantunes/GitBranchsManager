using Gbm.Services.Build;
using Gbm.Services.Git;
using Gbm.Services.Tasks;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.BranchesCommands;

public class PushTaskBranchesCommand(IGitTool gitTool, IBuildTool dotnetTool)
{
    [CommandAsyncWithArgsBuilderAsync<BranchesCommandArgsBuilder>("-push", Description = "Build and push mapped task branches",
        Example = "gbm -push <TaskId> [nobuild] [Repos...]", Group = CommandGroups.Branches, Order = 4)]
    public async Task<int> ExecuteAsync(RepositoryBranchTarget[] targets, bool noBuild = false, CancellationToken cancellationToken = default)
    {
        if (!noBuild)
            foreach (var target in targets)
                if (!await dotnetTool.BuildBranchAsync(target.Repository, target.BranchName, cancellationToken))
                {
                    MyConsole.WriteError($"❌ Build failed in '{target.Repository}'. Nothing was pushed.");
                    return 1;
                }
        await BranchExecution.ForEachAsync(targets, async target =>
        {
            await BranchExecution.CheckoutRequiredAsync(gitTool, target, cancellationToken);
            await gitTool.PushAsync(cancellationToken);
        });
        MyConsole.WriteSucess("✅ Task branches pushed!");
        return 0;
    }
}
