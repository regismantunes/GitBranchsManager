using Gbm.Persistence.Configuration;

namespace Gbm.Services.Initialization
{
    public static class FailMessages
    {
        public static IReadOnlyDictionary<ConfigurationVariable, string> MissingConfigurationVariableMessages => new Dictionary<ConfigurationVariable, string>()
        {
            { ConfigurationVariable.BasePath, MissingBasePathMessage },
            { ConfigurationVariable.GitHubToken, MissingGitHubTokenMessage },
            { ConfigurationVariable.GitHubRepositoriesOwner, MissingGitHubRepositoriesOwnerMessage },
            { ConfigurationVariable.JiraDomain, MissingJiraDomainMessage },
            { ConfigurationVariable.JiraUserMail, MissingJiraUserMailMessage },
            { ConfigurationVariable.JiraUserPassword, MissingJiraUserPasswordMessage },
            { ConfigurationVariable.JiraConsumerKey, MissingJiraConsumerKeyMessage },
            { ConfigurationVariable.JiraConsumerSecret, MissingJiraConsumerSecretyMessage },
            { ConfigurationVariable.JiraAccessToken, MissingJiraAccessTokenMessage },
            { ConfigurationVariable.JiraTokenSecrety, MissingJiraTokenSecretyMessage }
        };

        public const string MissingBasePathMessage = """
			❌ Base path is not set.
			Use the command: gbm -b <BasePath>
			""";
        public const string MissingGitHubTokenMessage = """
			❌ GitHub Token is not set.
			Use the command: gbm -gt <GitHubToken>
			""";
        public const string MissingGitHubRepositoriesOwnerMessage = """
			❌ Repositories Owner is not set.
			Use the command: gbm -go <RepositoriesOwner>
			""";
        public const string MissingJiraAccessTokenMessage = """
			❌ Jira Access Token is not set.
			Use the command: gbm -ja <JiraAccessToken>
			""";
        public const string MissingJiraConsumerKeyMessage = """
			❌ Jira Consumer Key is not set.
			Use the command: gbm -jc <JiraConsumerKey>
			""";
        public const string MissingJiraConsumerSecretyMessage = """
			❌ Jira Consumer Secrety is not set.
			Use the command: gbm -js <JiraConsumerSecrety>
			""";
        public const string MissingJiraDomainMessage = """
			❌ Jira Domain is not set.
			Use the command: gbm -jd <Domain>
			""";
        public const string MissingJiraTokenSecretyMessage = """
			❌ Jira Token Secrety is not set.
			Use the command: gbm -jt <JiraTokenSecrety>
			""";
        public const string MissingJiraUserMailMessage = """
			❌ Jira User Mail is not set.
			Use the command: gbm -ju <UserMail>
			""";
        public const string MissingJiraUserPasswordMessage = """
			❌ Jira User Password is not set.
			Use the command: gbm -jp <UserPassword>
			""";
    }
}
