using Gbm.Services.Git;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.BranchsCommands
{
    public class ListTaskBranchsCommand(IGitTool gitTool)
    {
        [CommandAsyncWithArgsBuilderAsync<BranchsCommandArgsBuilder>("-l", 
            Description = "List repositories with task branchs", 
            Example = "gbm -l <TaskId> [Repos...]", 
            Group = CommandGroups.Branchs,
            Order = 1)]
        public async Task<int> ExecuteAsync(string taskBranch, string[] repositories, CancellationToken cancellationToken = default)
        {
            gitTool.ShowGitOutput = false;
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
