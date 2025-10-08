using Gbm.Git;

namespace Gbm.Commands
{
    public class UpdateTaskCommand : ITaskCommand
    {
        public int Execute(GitTool gitTool, string taskBranch, string[] repositories)
        {
            gitTool.ShowGitOutput = true;
            foreach (var repo in repositories)
            {
                MyConsole.WriteHeader($"--- Processing repository: {repo} ---");
                gitTool.SetRepository(repo);
                MyConsole.WriteStep($"→ Checking out to '{taskBranch}'");
                gitTool.Checkout(taskBranch);
                MyConsole.WriteStep($"→ Updating '{taskBranch}' from {repo}");
                gitTool.GetMainChanges();
            }

            MyConsole.WriteSucess($"✅ Branches from task {taskBranch} were updated!.");
            return 0;
        }
    }
}