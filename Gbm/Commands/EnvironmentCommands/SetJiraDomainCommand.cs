using Gbm.Environment;

namespace Gbm.Commands.EnvironmentCommands
{
    public class SetJiraDomainCommand : ISetEnvironmentCommand
    {
        public int Execute(string value)
        {
            EnvironmentVariable.JiraDomain.SetValue(value);
            MyConsole.WriteSucess($"✅ Jira domain updated to '{value}' (User scope)");
            return 0;
        }
    }
}
