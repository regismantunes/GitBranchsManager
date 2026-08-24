using Gbm.Services.Build;
using Gbm.Services.Git;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.BranchesCommands
{
    public class PushTaskBranchesCommand(IGitTool gitTool, IBuildTool dotnetTool)
    {
        [CommandAsyncWithArgsBuilderAsync<BranchesCommandArgsBuilder>("-push",
            Description = "Push task branches",
            Example = "gbm -push <TaskId> [nobuild] [Repos...]",
            Group = CommandGroups.Branches,
            Order = 4)]
        public async Task<int> ExecuteAsync(string taskBranch, string[] repositories, bool noBuild = false, CancellationToken cancellationToken = default)
        {
            MyConsole.WriteCommandHeader("📤 Pushing task branches...");

            // Phase 1: validate builds across all repositories
            if (!noBuild)
            {
                MyConsole.WriteHeader($"--- Validating builds ---");
                gitTool.ShowGitOutput = false;
                foreach (var repo in repositories)
                {
                    if (!await dotnetTool.BuildRepositoryAsync(repo, taskBranch, cancellationToken))
                    {
                        MyConsole.WriteError($"❌ Build failed in '{repo}'. Aborting — nothing was pushed.");
                        return 1;
                    }
                }
            }

            // Phase 2: push all branches
            MyConsole.WriteHeader($"--- Pushing branches ---");
            gitTool.ShowGitOutput = true;
            foreach (var repo in repositories)
            {
                MyConsole.WriteHeader($"--- Processing repository: {repo} ---");
                await gitTool.SetRepositoryAsync(repo, cancellationToken);

                MyConsole.WriteStep($"→ Checking out to '{taskBranch}'");
                await gitTool.CheckoutAsync(taskBranch, cancellationToken);

                MyConsole.WriteStep($"→ Pushing '{taskBranch}'");
                await gitTool.PushAsync(cancellationToken);
            }

            MyConsole.WriteSucess("✅ Branch's task pushed!");
            return 0;
        }
    }
}