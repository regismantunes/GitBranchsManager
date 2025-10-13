using Gbm.Environment;

namespace Gbm.Commands.EnvironmentCommands
{
    public class SetJiraConsumerSecretyCommand : ISetEnvironmentCommand
    {
        public int Execute(string value)
        {
            EnvironmentVariable.JiraConsumerSecret.SetValue(value);
            MyConsole.WriteSucess($"✅ Jira consumer secrety was updated (User scope)");
            return 0;
        }
    }
}
