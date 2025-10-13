using Gbm.Environment;

namespace Gbm.Commands.EnvironmentCommands
{
    public class SetJiraUserPasswordCommand : ISetEnvironmentCommand
    {
        public int Execute(string value)
        {
            EnvironmentVariable.JiraUserPassword.SetValue(value);
            MyConsole.WriteSucess($"✅ Jira user password has updated (User scope)");
            return 0;
        }
    }
}