using Gbm.Persistence.Configuration;
using Gbm.Services.Configuration;
using Microsoft.Extensions.Configuration;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.ConfigurationCommands
{
    public class SetGitHubRepositoriesOwnerCommand(IConfiguration configuration)
    {
        [Command("-go",
            Description = "Set GitHub repositories owner",
            Example = "gbm -go <RepositoriesOwner>",
            Group = CommandGroups.Configuration,
            Order = 2)]
        public int Execute(string value)
        {
            MyConsole.WriteCommandHeader("⚙️ Updating configuration: GitHub Repositories Owner...");
            configuration.SetValue(ConfigurationVariable.GitHubRepositoriesOwner, value);
            MyConsole.WriteSucess($"✅ Repositories owner updated to '{value}' (User scope)");
            return 0;
        }
    }
}
