using Gbm.Environment;

namespace Gbm.Commands.EnvironmentCommands
{
    public class SetJiraTokenSecretyCommand : ISetEnvironmentCommand
    {
        public int Execute(string value)
        {
            EnvironmentVariable.JiraTokenSecrety.SetValue(value);
            MyConsole.WriteSucess($"✅ Jira Token Secrety has updated. (User scope)");
            return 0;
        }
    }
}
