using Gbm.Environment;

namespace Gbm.Commands.EnvironmentCommands
{
    public class SetJiraAccessTokenCommand : ISetEnvironmentCommand
    {
        public int Execute(string value)
        {
            EnvironmentVariable.JiraAccessToken.SetValue(value);
            MyConsole.WriteSucess($"✅ Jira access token was updated (User scope)");
            return 0;
        }
    }
}
