using Gbm.Jira;
using System.Text;
using System.Text.Json;

namespace Gbm.GitHub
{
    public class GitHubClient(string githubToken, string owner)
    {
        public string Owner { get; } = owner;

        private string GitHubToken { get; } = githubToken;
        private JsonSerializerOptions JsonOptions { get; } = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private HttpClient CreateHttpClient()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"token {GitHubToken}");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Gbm");
            httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
            return httpClient;
        }

        private string CreateLink(string text, string url)
        {
            return $"[{text}]({url})";
        }

        private string CreatePullRequestBody(TaskInfo taskInfo, string? relatedPRsText = null)
        {
            var taskLink = CreateLink(taskInfo.Id, taskInfo.Url);
            var body = $"This PR implements the {taskLink} feature.\r\r{taskInfo.Description}";
            if (!string.IsNullOrWhiteSpace(relatedPRsText))
                body += $"\r\r{relatedPRsText}";
            return body;
        }

        public async Task<PullRequestInfo> CreatePullRequestAsync(string repo, string branchName, string baseBranch, TaskInfo taskInfo)
        {
            using var httpClient = CreateHttpClient();

            var taskLink = CreateLink(taskInfo.Id, taskInfo.Url);

            var prBody = new
            {
                title = $"{taskLink} {taskInfo.Summary}",
                body = CreatePullRequestBody(taskInfo),
                head = branchName,
                @base = baseBranch
            };

            var json = JsonSerializer.Serialize(prBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"https://api.github.com/repos/{owner}/{repo}/pulls", content);

            var responseContent = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to create PR in {repo}: {response.StatusCode} - {responseContent}");
            
            var prData = JsonSerializer.Deserialize<GitHubPrResponse>(responseContent, JsonOptions)
                ?? throw new Exception($"Failed to deserialize PR response for {repo}");

            MyConsole.WriteSucess($"✅ PR created in {repo}: {prData.HtmlUrl}");
            return new PullRequestInfo(repo, prData.Number, prData.HtmlUrl);
        }

        public async Task UpdatePullRequestAsync(string repo, int prNumber, string relatedPRsText, TaskInfo taskInfo)
        {
            using var httpClient = CreateHttpClient();

            var updateBody = new
            {
                body = CreatePullRequestBody(taskInfo, relatedPRsText),
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

        private class GitHubPrResponse
        {
            public int Number { get; set; }
            public string HtmlUrl { get; set; } = string.Empty;
        }
    }
}
