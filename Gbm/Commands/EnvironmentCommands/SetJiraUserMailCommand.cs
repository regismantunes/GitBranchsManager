using Gbm.Environment;

namespace Gbm.Commands.EnvironmentCommands
{
    public class SetJiraUserMailCommand : ISetEnvironmentCommand
    {
        public int Execute(string value)
        {
            EnvironmentVariable.JiraUserMail.SetValue(value);
            MyConsole.WriteSucess($"✅ Jira user email updated to '{value}' (User scope)");
            return 0;
        }
    }
}
