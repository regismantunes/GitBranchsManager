using Gbm.Persistence.Configuration;
using Gbm.Services.Configuration;
using Microsoft.Extensions.Configuration;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.ConfigurationCommands
{
    public class SetJiraDomainCommand(IConfiguration configuration)
    {
        [Command("-jd",
            Description = "Set Jira domain",
            Example = "gbm -jd <Domain>",
            Group = CommandGroups.Configuration,
            Order = 3)]
        public int Execute(string value)
        {
            MyConsole.WriteCommandHeader("⚙️ Updating configuration: Jira Domain...");
            configuration.SetValue(ConfigurationVariable.JiraDomain, value);
            MyConsole.WriteSucess($"✅ Jira domain updated to '{value}' (User scope)");
            return 0;
        }
    }
}
