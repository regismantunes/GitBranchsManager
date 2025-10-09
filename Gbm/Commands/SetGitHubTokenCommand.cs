namespace Gbm.Commands
{
    public class SetGitHubTokenCommand
    {
        public int Execute(string githubToken)
        {
            var envVar = EnvironmentVariables.GitHubToken;
            Environment.SetEnvironmentVariable(envVar, githubToken, EnvironmentVariableTarget.User);
            MyConsole.WriteSucess($"✅ GitHub Token has updated. (User scope)");
            return 0;
        }
    }
}
