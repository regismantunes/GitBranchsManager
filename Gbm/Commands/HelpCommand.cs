namespace Gbm.Commands
{
    public class HelpCommand
    {
        public int Execute()
        {
            MyConsole.WriteInfo("Usage:");
            MyConsole.WriteInfo("  gbm -b <BasePath>                   Set base path");
            MyConsole.WriteInfo("  gbm -gt <GitHubToken>               Set GitHub Token");
            MyConsole.WriteInfo("  gbm -go <RepositoriesOwner>         Set GitHub repositories owner");
            MyConsole.WriteInfo("  gbm -jd <Domain>                    Set Jira domain");
            MyConsole.WriteInfo("  gbm -ju <UserMail>                  Set Jira user email");
            MyConsole.WriteInfo("  gbm -jp <UserMail>                  Set Jira user password");
            MyConsole.WriteInfo("  gbm -jc <ConsumerKey>               Set Jira Consumer Key");
            MyConsole.WriteInfo("  gbm -js <ConsumerSecrety>           Set Jira Consumer Secrety");
            MyConsole.WriteInfo("  gbm -ja <JiraAccessToken>           Set Jira Access Token");
            MyConsole.WriteInfo("  gbm -jt <JiraTokenSecrety>          Set Jira Token Secrety");
            MyConsole.WriteInfo("  gbm -n <TaskId> [Repos...]          Create feature branches");
            MyConsole.WriteInfo("  gbm -l <TaskId> [Repos...]          List repositories with feature branchs");
            MyConsole.WriteInfo("  gbm -s <TaskId> [Repos...]          Checkout feature branches");
            MyConsole.WriteInfo("  gbm -u <TaskId> [Repos...]          Update feature branches from base");
            MyConsole.WriteInfo("  gbm -p <TaskId> [Repos...]          Push feature branches");
            MyConsole.WriteInfo("  gbm -d <TaskId> [Repos...]          Merge feature into develop and push");
            MyConsole.WriteInfo("  gbm -pr <TaskId> [Repos...]         Create pull requests for feature branches");
            MyConsole.WriteInfo("  gbm -pri <TaskId>                   List pull requests by TaskId");
            MyConsole.WriteInfo("  gbm -r <TaskId> [Repos...]          Remove local feature branches");
            MyConsole.WriteEmptyLine();
            MyConsole.WriteInfo("Notes: Do not prefix TaskId or repos with parameter names; provide them positionally.");
            return 0;
        }
    }
}
