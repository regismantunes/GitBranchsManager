using Gbm.Git;

namespace Gbm.Commands
{
    public class UpdateTaskCommand : ITaskCommand
    {
        public async Task<int> ExecuteAsync(GitTool gitTool, string taskBranch, string[] repositories)
        {
            gitTool.ShowGitOutput = true;
            foreach (var repo in repositories)
            {
                MyConsole.WriteHeader($"--- Processing repository: {repo} ---");
                gitTool.SetRepository(repo);
                MyConsole.WriteStep($"→ Checking out to '{taskBranch}'");
                await gitTool.CheckoutAsync(taskBranch);
                MyConsole.WriteStep($"→ Updating '{taskBranch}' from {repo}");
                await gitTool.GetMainChangesAsync();
            }

            MyConsole.WriteSucess($"✅ Branches from task {taskBranch} were updated!.");
            return 0;
        }
    }
}