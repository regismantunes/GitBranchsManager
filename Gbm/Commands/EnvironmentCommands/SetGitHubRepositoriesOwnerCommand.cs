using Gbm.Environment;

namespace Gbm.Commands.EnvironmentCommands
{
    public class SetGitHubRepositoriesOwnerCommand : ISetEnvironmentCommand
    {
        public int Execute(string value)
        {
            EnvironmentVariable.GitHubRepositoriesOwner.SetValue(value);
            MyConsole.WriteSucess($"✅ Repositories owner updated to '{value}' (User scope)");
            return 0;
        }
    }
}
