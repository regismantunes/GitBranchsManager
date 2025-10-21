using Gbm.Persistence.Environment;
using Gbm.Services.Extensions;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.EnvironmentCommands
{
    public class SetBasePathCommand
    {
        [Command("-b",
            Description = "Set base path",
            Example = "gbm -b <BasePath>",
            Group = CommandGroups.Configuration,
            Order = 0)]
        public int Execute(string value)
        {
            MyConsole.WriteHeader("⚙️ Updating configuration: Base Path...");
            EnvironmentVariable.BasePath.SetValue(value);
            MyConsole.WriteSucess($"✅ Base path updated to '{value}' (User scope)");
            return 0;
        }
    }
}