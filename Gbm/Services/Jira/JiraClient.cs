using Gbm.Persistence.Entities;
using Gbm.Persistence.Repositories.Interfaces;

namespace Gbm.Services.Jira
{
    public class JiraClient : IJiraClient
    {
        private readonly ITaskInfoRepository _repository;
        private readonly string _jiraDomain;
        private readonly string? _userEmail;
        private readonly string? _userPassword;
        private readonly string? _consumerKey;
        private readonly string? _consumerSecret;
        private readonly string? _accessToken;
        private readonly string? _accessTokenSecret;

        public JiraClient(ITaskInfoRepository repository, string jiraDomain, string userEmail, string userPassword)
        {
            ArgumentNullException.ThrowIfNull(repository);
            ArgumentException.ThrowIfNullOrWhiteSpace(jiraDomain);
            ArgumentException.ThrowIfNullOrWhiteSpace(userEmail);
            ArgumentException.ThrowIfNullOrWhiteSpace(userPassword);

            _repository = repository;
            _jiraDomain = jiraDomain;
            _userEmail = userEmail;
            _userPassword = userPassword;
        }

        public JiraClient(ITaskInfoRepository repository, string jiraDomain, string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret)
        {
            ArgumentNullException.ThrowIfNull(repository);
            ArgumentException.ThrowIfNullOrWhiteSpace(jiraDomain);
            ArgumentException.ThrowIfNullOrWhiteSpace(consumerKey);
            ArgumentException.ThrowIfNullOrWhiteSpace(consumerSecret);
            ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
            ArgumentException.ThrowIfNullOrWhiteSpace(accessTokenSecret);

            _repository = repository;
            _jiraDomain = jiraDomain;
            _consumerKey = consumerKey;
            _consumerSecret = consumerSecret;
            _accessToken = accessToken;
            _accessTokenSecret = accessTokenSecret;
        }

        private string GetJiraUrl() => $"https://{_jiraDomain}.atlassian.net";

        private string GetTaskUrl(string taskId) => $"{GetJiraUrl()}/browse/{taskId}";

        public async Task<TaskInfo?> GetTaskInfoAsync(string taskId, CancellationToken cancellationToken = default)
        {
            var taskInfo = await _repository.GetAsync(taskId, cancellationToken);
            if (taskInfo is not null)
                return taskInfo;

            var url = GetJiraUrl();
            var jira = _userEmail != null ?
                Atlassian.Jira.Jira.CreateRestClient(url, _userEmail, _userPassword) :
                Atlassian.Jira.Jira.CreateOAuthRestClient(url, _consumerKey, _consumerSecret, _accessToken, _accessTokenSecret);

            var issue = await jira.Issues.GetIssueAsync(taskId, cancellationToken);

            return new TaskInfo(taskId,
                                issue.Summary,
                                issue.Description ?? string.Empty,
                                GetTaskUrl(taskId),
                                $"feature/{taskId}");
        }
    }
}
