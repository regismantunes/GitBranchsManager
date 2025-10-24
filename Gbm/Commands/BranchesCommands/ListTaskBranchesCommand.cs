using Gbm.Services.Git;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.BranchesCommands
{
    public class ListTaskBranchesCommand(IGitTool gitTool)
    {
        [CommandAsyncWithArgsBuilderAsync<BranchesCommandArgsBuilder>("-l", 
            Description = "List repositories with task branches", 
            Example = "gbm -l <TaskId> [Repos...]", 
            Group = CommandGroups.Branches,
            Order = 1)]
        public async Task<int> ExecuteAsync(string taskBranch, string[] repositories, CancellationToken cancellationToken = default)
        {
            gitTool.ShowGitOutput = false;
            MyConsole.WriteCommandHeader("🧾 Listing repositories with task branches...");
            foreach (var repo in repositories)
            {
                gitTool.SetRepository(repo);
                var branchExists = await gitTool.BranchExistsAsync(taskBranch, cancellationToken);
                if (branchExists)
                    MyConsole.WriteStep($"→ Branch '{taskBranch}' exists in {repo}");
            }
            
            MyConsole.WriteSucess("✅ Branch existence check completed!");
            return 0;
        }
    }
}
