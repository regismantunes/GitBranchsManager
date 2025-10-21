using Gbm.Persistence.Configuration;
using Gbm.Services.Configuration;
using Microsoft.Extensions.Configuration;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.ConfigurationCommands
{
    public class SetBasePathCommand(IConfiguration configuration)
    {
        [Command("-b",
            Description = "Set base path",
            Example = "gbm -b <BasePath>",
            Group = CommandGroups.Configuration,
            Order = 0)]
        public int Execute(string value)
        {
            MyConsole.WriteHeader("⚙️ Updating configuration: Base Path...");
            configuration.SetValue(ConfigurationVariable.BasePath, value);
            MyConsole.WriteSucess($"✅ Base path updated to '{value}' (User scope)");
            return 0;
        }
    }
}