using Gbm.Persistence.Configuration;
using Gbm.Services.Configuration;
using Microsoft.Extensions.Configuration;
using RA.Console.DependencyInjection.Attributes;

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
            MyConsole.WriteCommandHeader("⚙️ Updating configuration: Base Path...");

            if (!Path.IsPathFullyQualified(value))
            {
                MyConsole.WriteError("❌ The provided path is not valid. Please provide a fully qualified path.");
                return 1;
            }

            configuration.SetValue(ConfigurationVariable.BasePath, value);
            MyConsole.WriteSucess($"✅ Base path updated to '{value}' (User scope)");
            return 0;
        }
    }
}