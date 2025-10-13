using Gbm.Environment;

namespace Gbm.Commands.EnvironmentCommands
{
    public class SetGitHubTokenCommand : ISetEnvironmentCommand
    {
        public int Execute(string value)
        {
            EnvironmentVariable.GitHubToken.SetValue(value);
            MyConsole.WriteSucess($"✅ GitHub Token has updated. (User scope)");
            return 0;
        }
    }
}
