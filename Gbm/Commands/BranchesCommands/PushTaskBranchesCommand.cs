using Gbm.Services.Dotnet;
using Gbm.Services.Git;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.BranchesCommands
{
    public class PushTaskBranchesCommand(IGitTool gitTool, IDotnetTool dotnetTool)
    {
        [CommandAsyncWithArgsBuilderAsync<BranchesCommandArgsBuilder>("-push",
            Description = "Push task branches",
            Example = "gbm -push <TaskId> [Repos...]",
            Group = CommandGroups.Branches,
            Order = 4)]
        public async Task<int> ExecuteAsync(string taskBranch, string[] repositories, CancellationToken cancellationToken = default)
        {
            MyConsole.WriteCommandHeader("📤 Pushing task branches...");

            // Phase 1: validate builds across all repositories
            MyConsole.WriteHeader($"--- Validating builds ---");
            gitTool.ShowGitOutput = false;
            foreach (var repo in repositories)
            {
                await gitTool.SetRepositoryAsync(repo, cancellationToken);

                MyConsole.WriteStep($"→ Checking out '{taskBranch}' in '{repo}'");
                if (!await gitTool.CheckoutAsync(taskBranch, cancellationToken))
                {
                    MyConsole.WriteError($"❌ Branch '{taskBranch}' not found in '{repo}'. Aborting.");
                    return 1;
                }

                MyConsole.WriteStep($"→ Building '{repo}'...");
                if (!await dotnetTool.BuildRepositoryAsync(gitTool.WorkingDirectory!, cancellationToken))
                {
                    MyConsole.WriteError($"❌ Build failed in '{repo}'. Aborting — nothing was pushed.");
                    return 1;
                }

                MyConsole.WriteStep($"→ '{repo}' build passed ✔");
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