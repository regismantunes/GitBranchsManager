using Gbm.Persistence.Configuration;
using Gbm.Services.Configuration;
using Microsoft.Extensions.Configuration;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.ConfigurationCommands
{
    public class SetJiraTokenSecretyCommand(IConfiguration configuration)
    {
        [Command("-jt",
            Description = "Set Jira Token Secrety",
            Example = "gbm -jt <JiraTokenSecrety>",
            Group = CommandGroups.Configuration,
            Order = 9)]
        public int Execute(string value)
        {
            MyConsole.WriteCommandHeader("⚙️ Updating configuration: Jira Token Secrety...");
            configuration.SetValue(ConfigurationVariable.JiraTokenSecrety, value);
            MyConsole.WriteSucess($"✅ Jira Token Secrety has updated. (User scope)");
            return 0;
        }
    }
}
