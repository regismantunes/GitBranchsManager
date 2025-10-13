namespace Gbm.Jira
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
            ArgumentNullException.ThrowIfNull(jiraDomain);
            ArgumentNullException.ThrowIfNull(userEmail);
            ArgumentNullException.ThrowIfNull(userPassword);

            _jiraDomain = jiraDomain;
            _userEmail = userEmail;
            _userPassword = userPassword;
        }

        public JiraClient(string jiraDomain, string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret)
        {
            ArgumentNullException.ThrowIfNull(jiraDomain);
            ArgumentNullException.ThrowIfNull(consumerKey);
            ArgumentNullException.ThrowIfNull(consumerSecret);
            ArgumentNullException.ThrowIfNull(accessToken);
            ArgumentNullException.ThrowIfNull(accessTokenSecret);

            _jiraDomain = jiraDomain;
            _consumerKey = consumerKey;
            _consumerSecret = consumerSecret;
            _accessToken = accessToken;
            _accessTokenSecret = accessTokenSecret;
        }

        private string GetJiraUrl() => $"https://{_jiraDomain}.atlassian.net";

        public string GetTaskUrl(string taskId) => $"{GetJiraUrl()}/browse/{taskId}";

        public async Task<TaskInfo?> GetTaskInfoAsync(string taskId, CancellationToken cancellationToken = default)
        {
            var url = GetJiraUrl();
            var jira = _userEmail != null ?
                Atlassian.Jira.Jira.CreateRestClient(url, _userEmail, _userPassword) :
                Atlassian.Jira.Jira.CreateOAuthRestClient(url, _consumerKey, _consumerSecret, _accessToken, _accessTokenSecret);

            var issue = await jira.Issues.GetIssueAsync(taskId, cancellationToken);

            return new TaskInfo(taskId,
                                issue.Summary,
                                issue.Description ?? string.Empty,
                                GetTaskUrl(taskId));
        }
    }
}
