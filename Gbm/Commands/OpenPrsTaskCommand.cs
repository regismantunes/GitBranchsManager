using System.Text;
using System.Text.Json;
using Gbm.Git;

namespace Gbm.Commands
{
    public class OpenPrsTaskCommand
    {
        private const string OWNER = "Betenbough-Companies";
        
        public async Task<int> ExecuteAsync(GitTool gitTool, string taskBranch, string[] repositories, string githubToken)
        {
            try
            {
                var createdPRs = new List<CreatedPr>();

                // Create PRs for each repository
                MyConsole.WriteHeader($"--- Creating PRs ---");
                foreach (var repo in repositories)
                {
                    MyConsole.WriteStep($"→ Creating PR in {repo}");
                    gitTool.SetRepository(repo);
                    var baseBranch = await gitTool.GetMainBranchAsync();
                    var pr = await CreatePullRequestAsync(githubToken, OWNER, repo, taskBranch, baseBranch);
                    createdPRs.Add(pr);
                }

                // Generate related PRs text
                var relatedPRsText = string.Join('\n', createdPRs.Select(pr => $"- Related PR: [{pr.Repo}]({pr.Url})"));

                // Update each PR with related links
                foreach (var pr in createdPRs)
                {
                    MyConsole.WriteStep($"→ Updating PR in {pr.Repo} with related links");
                    await UpdatePullRequestAsync(githubToken, OWNER, pr.Repo, pr.Number, relatedPRsText);
                }

                MyConsole.WriteSucess("🚀 All PRs created and updated with related links.");
                MyConsole.WriteEmptyLine();
                MyConsole.WriteInfo($"{taskBranch} PRs:");
                foreach (var pr in createdPRs)
                {
                    MyConsole.WriteInfo($"- {pr.Repo}: {pr.Url}");
                }

                return 0;
            }
            catch (Exception ex)
            {
                MyConsole.WriteError($"❌ Error creating or updating PRs: {ex.Message}");
                return 1;
            }
        }

        private async Task<CreatedPr> CreatePullRequestAsync(string githubToken, string owner, string repo, string branchName, string baseBranch)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"token {githubToken}");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Gbm");
            httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

            var taskId = branchName.Contains('/') ?
                branchName.Split('/')[1] :
                branchName;

            var prBody = new
            {
                title = $"[{taskId}] Pull Request",
                body = $"This PR implements the {taskId} feature.",
                head = branchName,
                @base = baseBranch
            };

            var json = JsonSerializer.Serialize(prBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"https://api.github.com/repos/{owner}/{repo}/pulls", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to create PR in {repo}: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var prData = JsonSerializer.Deserialize<GitHubPrResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new Exception($"Failed to deserialize PR response for {repo}");

            MyConsole.WriteSucess($"✅ PR created in {repo}: {prData.HtmlUrl}");
            return new CreatedPr { Repo = repo, Number = prData.Number, Url = prData.HtmlUrl };
        }

        private async Task UpdatePullRequestAsync(string githubToken, string owner, string repo, int prNumber, string relatedPRsText)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"token {githubToken}");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Gbm");
            httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

            var updateBody = new
            {
                body = $"This PR implements the feature.\n\n{relatedPRsText}"
            };

            var json = JsonSerializer.Serialize(updateBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PatchAsync($"https://api.github.com/repos/{owner}/{repo}/pulls/{prNumber}", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to update PR in {repo}: {response.StatusCode} - {errorContent}");
            }

            MyConsole.WriteSucess($"✏️ PR updated in {repo}");
        }

        private class CreatedPr
        {
            public string Repo { get; set; } = string.Empty;
            public int Number { get; set; }
            public string Url { get; set; } = string.Empty;
        }

        private class GitHubPrResponse
        {
            public int Number { get; set; }
            public string HtmlUrl { get; set; } = string.Empty;
        }
    }
}
