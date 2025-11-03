using Gbm.Services.Git;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.BranchesCommands
{
    public class PullTaskBranchesCommand(IGitTool gitTool)
    {
        [CommandAsyncWithArgsBuilderAsync<BranchesCommandArgsBuilder>("-pull",
            Description = "Pull task branches",
            Example = "gbm -pull <TaskId> [Repos...]",
            Group = CommandGroups.Branches,
            Order = 5)]
        public async Task<int> ExecuteAsync(string taskBranch, string[] repositories, CancellationToken cancellationToken = default)
        {
            gitTool.ShowGitOutput = true;
            MyConsole.WriteCommandHeader("📥 Pulling task branches...");
            foreach (var repo in repositories)
            {
                MyConsole.WriteHeader($"--- Processing repository: {repo} ---");
                await gitTool.SetRepositoryAsync(repo, cancellationToken);

                MyConsole.WriteStep($"→ Checking out to '{taskBranch}'");
                await gitTool.CheckoutAsync(taskBranch, cancellationToken);

                MyConsole.WriteStep($"→ Pulling '{taskBranch}'");
                await gitTool.PullAsync(cancellationToken);
            }

            MyConsole.WriteSucess("✅ Branch's task pulled!");
            return 0;
        }
    }
}
