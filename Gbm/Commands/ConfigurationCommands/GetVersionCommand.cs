using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.ConfigurationCommands
{
    public class GetVersionCommand
    {
        [Command("-v",
            Description = "Get Gbm version",
            Example = "gbm -v",
            Group = CommandGroups.Configuration,
            Order = 1)]
        public int Execute()
        {
            var version = GetType().Assembly.GetName().Version?.ToString() ?? "Unknown";
            MyConsole.WriteInfo($"Gbm version: {version}");
            return 0;
        }
    }
}
