using Gbm.Persistence.Configuration;
using Gbm.Services.Configuration;
using Microsoft.Extensions.Configuration;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.ConfigurationCommands
{
    public class SetListRepositoriesAfterTaskCreationCommand(IConfiguration configuration)
    {
        [Command("-tlr",
            Description = "Set list repositories after task creation",
            Example = "gbm -tlr <true|false>",
            Group = CommandGroups.Configuration,
            Order = 1)]
        public int Execute(string value)
        {
            MyConsole.WriteCommandHeader("⚙️ Updating configuration: List Repositories After Task Creation...");
            if (!bool.TryParse(value, out var boolValue))
            {
                MyConsole.WriteError("❌ Invalid value. Please provide 'true' or 'false'.");
                return 1;
            }

            configuration.SetValue(ConfigurationVariable.ListRepositoriesAfterTaskCreation, boolValue);
            MyConsole.WriteSucess($"✅ List Repositories After Task Creation updated to '{value}' (User scope)");
            return 0;
        }
    }
}
