using Gbm.Git;
using Gbm.GitHub;
using Gbm.Jira;
using Gbm.Persistence.Repositories.Interfaces;

namespace Gbm.Commands.PullRequestCommands
{
    public class OpenPullRequestsTaskCommand
    {
        public async Task<int> ExecuteAsync(GitTool gitTool, string taskBranch, string[] repositories, GitHubClient gitHubClient, IJiraClient jiraClient, IPullRequestInfoRepository repository, string taskId, CancellationToken cancellationToken = default)
        {
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

                MyConsole.WriteHeader($"--- Creating PRs ---");
                foreach (var repo in repositories)
                {
                    if (await repository.ExistsAsync(taskId, repo, cancellationToken))
                        continue;

                    MyConsole.WriteStep($"→ Creating PR in {repo}");
                    gitTool.SetRepository(repo);
                    var baseBranch = await gitTool.GetMainBranchAsync();
                    var pr = await gitHubClient.CreatePullRequestAsync(repo, taskBranch, baseBranch, taskInfo, cancellationToken);
                    MyConsole.WriteStep($"→ PR successful created in {repo}: {pr.Url}");
                    await repository.SaveAsync(pr, cancellationToken);
                }

                // Generate related PRs text
                var relatedPRs = await repository.GetByTaskIdAsync(taskId, cancellationToken);
                var relatedPRsText = relatedPRs.Count == 0 ?
                    string.Empty :
                    string.Concat("**Related PRs:**\n", string.Join('\n', relatedPRs.Select(pr => $"- [{pr.Repository}]({pr.Url})")));

                // Update each PR with related links
                foreach (var pr in relatedPRs)
                {
                    MyConsole.WriteStep($"→ Updating PR in {pr.Repository} with related links");
                    await gitHubClient.UpdatePullRequestAsync(pr.Repository, pr.Number, relatedPRsText, taskInfo, cancellationToken);
                    MyConsole.WriteStep($"→ PR successful updated in {pr.Repository}");
                }

                MyConsole.WriteSucess("🚀 All PRs created and updated with related links.");
                MyConsole.WriteEmptyLine();
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
