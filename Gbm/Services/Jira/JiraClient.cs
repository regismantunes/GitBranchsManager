using Gbm.Persistence.Entities;

namespace Gbm.Services.Jira
{
    public class JiraClient : IJiraClient
    {
        private readonly string _jiraDomain;
        private readonly string? _userEmail;
        private readonly string? _userPassword;
        private readonly string? _consumerKey;
        private readonly string? _consumerSecret;
        private readonly string? _accessToken;
        private readonly string? _accessTokenSecret;

        public JiraClient(string jiraDomain, string userEmail, string userPassword)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(jiraDomain);
            ArgumentException.ThrowIfNullOrWhiteSpace(userEmail);
            ArgumentException.ThrowIfNullOrWhiteSpace(userPassword);

            _jiraDomain = jiraDomain;
            _userEmail = userEmail;
            _userPassword = userPassword;
        }

        public JiraClient(string jiraDomain, string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(jiraDomain);
            ArgumentException.ThrowIfNullOrWhiteSpace(consumerKey);
            ArgumentException.ThrowIfNullOrWhiteSpace(consumerSecret);
            ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
            ArgumentException.ThrowIfNullOrWhiteSpace(accessTokenSecret);

            _jiraDomain = jiraDomain;
            _consumerKey = consumerKey;
            _consumerSecret = consumerSecret;
            _accessToken = accessToken;
            _accessTokenSecret = accessTokenSecret;
        }

        private string GetJiraUrl() => $"https://{_jiraDomain}.atlassian.net";

        private string GetTaskUrl(string taskId) => $"{GetJiraUrl()}/browse/{taskId}";

        public async Task<TaskInfo?> GetTaskInfoFromJiraAsync(string taskId, CancellationToken cancellationToken = default)
        {
            var url = GetJiraUrl();
            var jira = _userEmail != null
                ? Atlassian.Jira.Jira.CreateRestClient(url, _userEmail, _userPassword)
                : Atlassian.Jira.Jira.CreateOAuthRestClient(url, _consumerKey, _consumerSecret, _accessToken, _accessTokenSecret);
            var issue = await jira.Issues.GetIssueAsync(taskId, cancellationToken);
            return new TaskInfo(taskId, issue.Summary, issue.Description ?? string.Empty, GetTaskUrl(taskId), []);
        }
    }
}
