using Gbm.Environment;

namespace Gbm.Commands.EnvironmentCommands
{
    public class SetJiraConsumerKeyCommand : ISetEnvironmentCommand
    {
        public int Execute(string value)
        {
            EnvironmentVariable.JiraConsumerKey.SetValue(value);
            MyConsole.WriteSucess($"✅ Jira Consumer Key updated to '{value}' (User scope)");
            return 0;
        }
    }
}
