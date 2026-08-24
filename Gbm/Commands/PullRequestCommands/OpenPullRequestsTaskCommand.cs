using Gbm.Persistence.Repositories.Interfaces;
using Gbm.Services.Build;
using Gbm.Services.Git;
using Gbm.Services.GitHub;
using Gbm.Services.Jira;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.PullRequestCommands
{
    public class OpenPullRequestsTaskCommand(IJiraClient jiraClient, IPullRequestInfoRepository repository, IGitHubClient gitHubClient, IGitTool gitTool, IBuildTool dotnetTool)
    {
        [CommandAsyncWithArgsBuilderAsync<OpenPullRequestsTaskCommandArgsBuilder>("-pr",
            Description = "Create pull requests for task branches. It will push local changes unless you sent the 'nopush' option.",
            Example = "gbm -pr <TaskId> [nopush] [nobuild] [Repos...]",
            Group = CommandGroups.PullRequests,
            Order = 0)]
        public async Task<int> ExecuteAsync(string taskId, string[] repositories, bool pushLocalChanges = true, bool noBuild = false, CancellationToken cancellationToken = default)
        {
            MyConsole.WriteCommandHeader("🔧 Creating and updating pull requests for task...");
            try
            {
                // Create PRs for each repository
                MyConsole.WriteHeader($"--- Getting Task Info ---");
                var taskInfo = await jiraClient.GetTaskInfoAsync(taskId, cancellationToken);
                if (taskInfo == null)
                {
                    MyConsole.WriteError($"❌ Task '{taskId}' not found in Jira.");
                    return 1;
                }
                MyConsole.WriteStep($"→ Task summary: {taskInfo.Summary}");

                // Phase 1: validate builds across all repositories
                if (!noBuild)
                {
                    MyConsole.WriteHeader($"--- Validating builds ---");
                    gitTool.ShowGitOutput = false;
                    foreach (var repo in repositories)
                    {
                        if (await repository.ExistsAsync(taskId, repo, cancellationToken))
                            continue;

                        if (!await dotnetTool.BuildRepositoryAsync(repo, taskInfo.BranchName, cancellationToken))
                        {
                            MyConsole.WriteError($"❌ Build failed in '{repo}'. Aborting — no PRs were opened.");
                            return 1;
                        }
                    }
                }

                // Phase 2: push and open PRs
                MyConsole.WriteHeader($"--- Creating PRs ---");
                foreach (var repo in repositories)
                {
                    MyConsole.WriteHeader($"--- Processing repository: {repo} ---");
                    if (await repository.ExistsAsync(taskId, repo, cancellationToken))
                        continue;

                    await gitTool.SetRepositoryAsync(repo, cancellationToken);
                    gitTool.ShowGitOutput = true;

                    MyConsole.WriteStep($"→ Checking out to '{taskInfo.BranchName}'");
                    await gitTool.CheckoutAsync(taskInfo.BranchName, cancellationToken);

                    if (pushLocalChanges)
                    {
                        MyConsole.WriteStep($"→ Pushing '{taskInfo.BranchName}'");
                        await gitTool.PushAsync(cancellationToken);
                    }

                    MyConsole.WriteStep($"→ Creating PR in {repo}...");
                    var baseBranch = await gitTool.GetMainBranchAsync(cancellationToken);
                    var pr = await gitHubClient.CreatePullRequestAsync(repo, taskInfo.BranchName, baseBranch, taskInfo, cancellationToken);
                    MyConsole.BackToPreviousLine();
                    MyConsole.WriteStep($"→ PR successful created in {repo}: {pr.Url}");
                    await repository.SaveAsync(pr, cancellationToken);
                }

                // Generate related PRs text
                var relatedPRs = await repository.GetByTaskIdAsync(taskId, cancellationToken);
                var relatedPRsText = relatedPRs.Count == 0 ?
                    string.Empty :
                    string.Concat("**Related PRs:**\n", string.Join('\n', relatedPRs.Select(pr => $"- [{pr.Repository}]({pr.Url})")));

                // Update each PR with related links
                MyConsole.WriteHeader($"--- Updating PRs with related links ---");
                foreach (var pr in relatedPRs)
                {
                    MyConsole.WriteStep($"→ Updating PR in {pr.Repository} with related links");
                    await gitHubClient.UpdatePullRequestAsync(pr.Repository, pr.Number, relatedPRsText, taskInfo, cancellationToken);
                    MyConsole.BackToPreviousLine();
                    MyConsole.WriteStep($"→ PR successful updated in {pr.Repository} with related links");
                }

                MyConsole.WriteSucess("🚀 All PRs created and updated with related links.");
                MyConsole.WriteHeader($"--- Summary ---");
                MyConsole.WriteInfo($"[{taskId}] {taskInfo.Summary}");
                foreach (var pr in relatedPRs)
                {
                    MyConsole.WriteInfo($"- {pr.Repository}: {pr.Url}");
                }

                return 0;
            }
            catch (Exception ex)
            {
                MyConsole.WriteError($"❌ Error creating or updating PRs: {ex.Message}");
                return 1;
            }
        }
    }
}
