using System.ComponentModel;

namespace Gbm.Persistence.Environment
{
    public enum EnvironmentVariable
    {
        [AmbientValue("GBM_BASE_PATH")]
        BasePath,
        [AmbientValue("GBM_GITHUB_TOKEN")]
        GitHubToken,
        [AmbientValue("GBM_GITHUB_REPO_OWNER")]
        GitHubRepositoriesOwner,
        [AmbientValue("GBM_JIRA_DOMAIN")]
        JiraDomain,
        [AmbientValue("GBM_JIRA_USERMAIL")]
        JiraUserMail,
        [AmbientValue("GBM_JIRA_USERPASSOWRD")]
        JiraUserPassword,
        [AmbientValue("GBM_JIRA_CONSUMERKEY")]
        JiraConsumerKey,
        [AmbientValue("GBM_JIRA_CONSUMERSECRET")]
        JiraConsumerSecret,
        [AmbientValue("GBM_JIRA_ACCESSTOKEN")]
        JiraAccessToken,
        [AmbientValue("GBM_JIRA_TOKENSECRETY")]
        JiraTokenSecrety
    }
}
