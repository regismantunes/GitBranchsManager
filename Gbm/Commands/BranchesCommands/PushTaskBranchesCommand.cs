using Gbm.Services.Git;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.BranchesCommands
{
    public class PushTaskBranchesCommand(IGitTool gitTool)
    {
        [CommandAsyncWithArgsBuilderAsync<BranchesCommandArgsBuilder>("-p",
            Description = "Push task branches",
            Example = "gbm -p <TaskId> [Repos...]",
            Group = CommandGroups.Branches,
            Order = 4)]
        public async Task<int> ExecuteAsync(string taskBranch, string[] repositories, CancellationToken cancellationToken = default)
        {
            gitTool.ShowGitOutput = true;
            MyConsole.WriteCommandHeader("📤 Pushing task branches...");
            foreach (var repo in repositories)
            {
                MyConsole.WriteHeader($"--- Processing repository: {repo} ---");
                gitTool.SetRepository(repo);

                MyConsole.WriteStep($"→ Checking out to '{taskBranch}'");
                await gitTool.CheckoutAsync(taskBranch, cancellationToken);
                
                MyConsole.WriteStep($"→ Pushing '{taskBranch}'");
                await gitTool.PushAsync(cancellationToken);
            }

            MyConsole.WriteSucess("✅ Branch's task pushed!");
            return 0;
        }
    }
}