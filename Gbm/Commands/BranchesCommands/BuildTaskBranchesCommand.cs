using Gbm.Services.Dotnet;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.BranchesCommands
{
    public class BuildTaskBranchesCommand(IDotnetTool dotnetTool)
    {
        [CommandAsyncWithArgsBuilderAsync<BranchesCommandArgsBuilder>("-build",
            Description = "Validate build of task branches",
            Example = "gbm -build <TaskId> [Repos...]",
            Group = CommandGroups.Branches,
            Order = 3)]
        public async Task<int> ExecuteAsync(string taskBranch, string[] repositories, CancellationToken cancellationToken = default)
        {
            MyConsole.WriteCommandHeader("🔨 Validating builds...");

            foreach (var repo in repositories)
            {
                if (!await dotnetTool.BuildRepositoryAsync(repo, taskBranch, cancellationToken))
                {
                    MyConsole.WriteError($"❌ Build failed in '{repo}'.");
                    return 1;
                }
            }

            MyConsole.WriteSucess("✅ All builds passed!");
            return 0;
        }
    }
}
