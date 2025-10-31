using Gbm.Services.Git;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.BranchesCommands
{
    public class UpdateTaskBranchesCommand(IGitTool gitTool)
    {
        [CommandAsyncWithArgsBuilderAsync<BranchesCommandArgsBuilder>("-u",
            Description = "Update task branches from base",
            Example = "gbm -u <TaskId> [Repos...]",
            Group = CommandGroups.Branches,
            Order = 3)]
        public async Task<int> ExecuteAsync(string taskBranch, string[] repositories, CancellationToken cancellationToken = default)
        {
            gitTool.ShowGitOutput = true;
            MyConsole.WriteCommandHeader("🔄 Updating task branches from base...");
            foreach (var repo in repositories)
            {
                MyConsole.WriteHeader($"--- Processing repository: {repo} ---");
                await gitTool.SetRepositoryAsync(repo, cancellationToken);
                MyConsole.WriteStep($"→ Checking out to '{taskBranch}'");
                await gitTool.CheckoutAsync(taskBranch, cancellationToken);
                MyConsole.WriteStep($"→ Updating '{taskBranch}' from {repo}");
                await gitTool.GetMainChangesAsync(cancellationToken);
            }

            MyConsole.WriteSucess($"✅ Branches from task {taskBranch} were updated!.");
            return 0;
        }
    }
}