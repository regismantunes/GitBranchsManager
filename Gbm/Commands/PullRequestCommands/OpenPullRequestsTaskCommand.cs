using Gbm.Git;
using Gbm.GitHub;
using Gbm.Jira;

namespace Gbm.Commands.PullRequestCommands
{
    public class OpenPullRequestsTaskCommand
    {
        public async Task<int> ExecuteAsync(GitTool gitTool, string taskBranch, string[] repositories, GitHubClient gitHubClient, IJiraClient jiraClient, CancellationToken cancellationToken = default)
        {
            try
            {
                var createdPRs = new List<PullRequestInfo>();

                // Create PRs for each repository
                MyConsole.WriteHeader($"--- Getting Task Info ---");
                var taskId = gitTool.GetTaskIdFromBranch(taskBranch);
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
                    MyConsole.WriteStep($"→ Creating PR in {repo}");
                    gitTool.SetRepository(repo);
                    var baseBranch = await gitTool.GetMainBranchAsync();
                    var pr = await gitHubClient.CreatePullRequestAsync(repo, taskBranch, baseBranch, taskInfo);
                    createdPRs.Add(pr);
                }

                // Generate related PRs text
                var relatedPRsText = string.Join('\n', createdPRs.Select(pr => $"- Related PR: [{pr.Repository}]({pr.Url})"));

                // Update each PR with related links
                foreach (var pr in createdPRs)
                {
                    MyConsole.WriteStep($"→ Updating PR in {pr.Repository} with related links");
                    await gitHubClient.UpdatePullRequestAsync(pr.Repository, pr.Number, relatedPRsText, taskInfo);
                }

                MyConsole.WriteSucess("🚀 All PRs created and updated with related links.");
                MyConsole.WriteEmptyLine();
                MyConsole.WriteInfo($"{taskBranch} PRs:");
                foreach (var pr in createdPRs)
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
