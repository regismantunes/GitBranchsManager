using Gbm.Git;

namespace Gbm.Commands
{
    public class ListTaskCommand : ITaskCommand
    {
        public int Execute(GitTool gitTool, string taskBranch, string[] repositories)
        {
            gitTool.ShowGitOutput = false;
            foreach (var repo in repositories)
            {
                gitTool.SetRepository(repo);
                var branchExists = gitTool.BranchExists(taskBranch);
                if (branchExists)
                    MyConsole.WriteStep($"→ Branch '{taskBranch}' exists in {repo}");
            }
            
            MyConsole.WriteSucess("✅ Branch existence check completed!");
            return 0;
        }
    }
}
