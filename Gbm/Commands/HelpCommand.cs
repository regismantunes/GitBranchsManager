namespace Gbm.Commands
{
    public class HelpCommand
    {
        public int Execute()
        {
            MyConsole.WriteInfo("Usage:");
            MyConsole.WriteInfo("  gbm -b <BasePath>                   Set base path");
            MyConsole.WriteInfo("  gbm -gt <GitHubToken>               Set GitHub Token");
            MyConsole.WriteInfo("  gbm -n <TaskId> [Repos...]          Create feature branches");
            MyConsole.WriteInfo("  gbm -l <TaskId> [Repos...]          List repositories with feature branchs");
            MyConsole.WriteInfo("  gbm -s <TaskId> [Repos...]          Checkout feature branches");
            MyConsole.WriteInfo("  gbm -u <TaskId> [Repos...]          Update feature branches from base");
            MyConsole.WriteInfo("  gbm -p <TaskId> [Repos...]          Push feature branches");
            MyConsole.WriteInfo("  gbm -d <TaskId> [Repos...]          Merge feature into develop and push");
            MyConsole.WriteInfo("  gbm -pr <TaskId> [Repos...]         Create pull requests for feature branches");
            MyConsole.WriteInfo("  gbm -r <TaskId> [Repos...]          Remove local feature branches");
            MyConsole.WriteEmptyLine();
            MyConsole.WriteInfo("Notes: Do not prefix TaskId or repos with parameter names; provide them positionally.");
            return 0;
        }
    }
}
