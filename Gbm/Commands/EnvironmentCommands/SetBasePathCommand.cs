using Gbm.Persistence.Environment;
using Gbm.Services.Extensions;
using RA.Console.DependecyInjection.Attributes;

namespace Gbm.Commands.EnvironmentCommands
{
    public class SetBasePathCommand
    {
        [Command("-b", Description = "Set base path", Example = "gbm -b <BasePath>")]
        public int Execute(string value)
        {
            EnvironmentVariable.BasePath.SetValue(value);
            MyConsole.WriteSucess($"✅ Base path updated to '{value}' (User scope)");
            return 0;
        }
    }
}