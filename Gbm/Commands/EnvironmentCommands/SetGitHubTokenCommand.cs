using Gbm.Persistence.Environment;
using Gbm.Services.Extensions;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.EnvironmentCommands
{
    public class SetGitHubTokenCommand
    {
        [Command("-go", Description = "Set GitHub repositories owner", Example = "gbm -go <RepositoriesOwner>")]
        public int Execute(string value)
        {
            EnvironmentVariable.GitHubToken.SetValue(value);
            MyConsole.WriteSucess($"✅ GitHub Token has updated. (User scope)");
            return 0;
        }
    }
}
