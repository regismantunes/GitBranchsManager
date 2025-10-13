using Gbm.Environment;
using Gbm.Git;
using Gbm.GitHub;
using Gbm.Jira;

namespace Gbm.Commands.Args
{
	public static class ArgsBuilder
	{
		private static IEnumerable<EnvironmentVariableCommandInfo> EnvironmentVariablesCommands => new List<EnvironmentVariableCommandInfo>()
		{
			{ new EnvironmentVariableCommandInfo { Action = "-b", ErrorMessage = ArgsFailMessages.MissingBasePathMessage, Variable = EnvironmentVariable.BasePath } },
			{ new EnvironmentVariableCommandInfo { Action = "-gt", ErrorMessage = ArgsFailMessages.MissingGitHubTokenMessage, Variable = EnvironmentVariable.GitHubToken } },
			{ new EnvironmentVariableCommandInfo { Action = "-go", ErrorMessage = ArgsFailMessages.MissingGitHubRepositoriesOwnerMessage, Variable = EnvironmentVariable.GitHubRepositoriesOwner } },
            { new EnvironmentVariableCommandInfo { Action = "-jd", ErrorMessage = ArgsFailMessages.MissingJiraDomainMessage, Variable = EnvironmentVariable.JiraDomain } },
            { new EnvironmentVariableCommandInfo { Action = "-ju", ErrorMessage = ArgsFailMessages.MissingJiraUserMailMessage, Variable = EnvironmentVariable.JiraUserMail } },
            { new EnvironmentVariableCommandInfo { Action = "-jp", ErrorMessage = ArgsFailMessages.MissingJiraUserPasswordMessage, Variable = EnvironmentVariable.JiraUserPassword } },
            { new EnvironmentVariableCommandInfo { Action = "-jc", ErrorMessage = ArgsFailMessages.MissingJiraConsumerKeyMessage, Variable = EnvironmentVariable.JiraConsumerKey } },
            { new EnvironmentVariableCommandInfo { Action = "-js", ErrorMessage = ArgsFailMessages.MissingJiraConsumerSecretyMessage, Variable = EnvironmentVariable.JiraConsumerSecret } },
            { new EnvironmentVariableCommandInfo { Action = "-ja", ErrorMessage = ArgsFailMessages.MissingJiraAccessTokenMessage, Variable = EnvironmentVariable.JiraAccessToken } },
            { new EnvironmentVariableCommandInfo { Action = "-jt", ErrorMessage = ArgsFailMessages.MissingJiraTokenSecretyMessage, Variable = EnvironmentVariable.JiraTokenSecrety } }
		};

        public static ArgsContext Build(string[] args)
		{
			if (args == null ||
				args.Length == 0 ||
				args[0] == "-h" ||
				args[0] == "--help")
				return new ArgsContext("-h");

			var action = args[0];
			if (EnvironmentVariablesCommands.Any(c => c.Action == action))
			{
                if (args.Length < 2)
					throw new ArgsValidationException(EnvironmentVariablesCommands.Single(c => c.Action == action).ErrorMessage);
                var singleArg = args[1];
                return new ArgsContext(action, Environment: singleArg);
            }

			if (!ProgramCommandBuilder.IsValidAction(action))
				throw new ArgsValidationException($"Unknown action '{action}'. Use -h for help.");

            if (args.Length < 2) throw new ArgsValidationException($"Missing TaskId. Example: gbm {action} SSM-903 [Repo1 Repo2 ...]");
			if (action == "-n" && args.Length < 3) throw new ArgsValidationException("Missing repositories. Example: gbm -n SSM-903 Repo1 Repo2");

            // Get task branch name
            var taskId = args[1];

            // Get base path from environment variable if not provided
            var basePath = GetEnvironmentVariableOrThrow(EnvironmentVariable.BasePath);

			if (action == "-t")
			{
                var jiraDomain = GetEnvironmentVariableOrThrow(EnvironmentVariable.JiraDomain);
				var fakeJiraClient = GetFakeJiraClient(basePath, jiraDomain);
				return new ArgsContext(action, JiraClient: fakeJiraClient, TaskId: taskId);
            }

            // Get repositories list (if any)
            var reposArg = args.Length > 2 ? args[2..] : [];

            // Initialize GitTool
            var gitTool = new GitTool(basePath) { ShowGitOutput = false };
			var taskBranch = gitTool.GetBranchNameFromTaskId(taskId);

            // For all except -n, preload repositories that have the feature branch if none provided
            if (action != "-n" && reposArg.Length == 0)
			{
				reposArg = Directory.GetDirectories(basePath)
					.Select(Path.GetFileName)
					.Where(name => 
					{
						if (string.IsNullOrWhiteSpace(name)) return false;
						gitTool.SetRepository(name);
                        return gitTool.BranchExistsAsync(taskBranch).GetAwaiter().GetResult();
					})
					.ToArray()!;

				if (reposArg.Length == 0)
					throw new ArgsValidationException($"No repositories found with branch '{taskBranch}'.");
            }

            // Initialize GitHubClient and JiraClient if action is -pr
            GitHubClient? gitHubClient = null;
			IJiraClient? jiraClient = null;
            if (action == "-pr")
			{
				var gitHubToken = GetEnvironmentVariableOrThrow(EnvironmentVariable.GitHubToken);
				var repositoriesOwner = GetEnvironmentVariableOrThrow(EnvironmentVariable.GitHubRepositoriesOwner);
				gitHubClient = new GitHubClient(gitHubToken, repositoriesOwner);

                var jiraDomain = GetEnvironmentVariableOrThrow(EnvironmentVariable.JiraDomain);
                var jiraConsumerKey = EnvironmentVariable.JiraConsumerKey.GetValue();
				var jiraConsumerSecrety = EnvironmentVariable.JiraConsumerSecret.GetValue();
				var jiraAcccessToken = EnvironmentVariable.JiraAccessToken.GetValue();
				var jiraTokenSecrety = EnvironmentVariable.JiraTokenSecrety.GetValue();
                var jiraUserMail = EnvironmentVariable.JiraUserMail.GetValue();
				var jiraUserPassword = EnvironmentVariable.JiraUserPassword.GetValue();
				if (jiraConsumerKey is not null &&
					jiraConsumerSecrety is not null &&
					jiraAcccessToken is not null &&
					jiraTokenSecrety is not null)
				{
					jiraClient = new JiraClient(jiraDomain, jiraConsumerKey, jiraConsumerKey, jiraAcccessToken, jiraTokenSecrety);
				}
                else if (jiraUserMail is not null &&
					jiraUserPassword is not null)
				{
					jiraClient = new JiraClient(jiraDomain, jiraUserMail, jiraUserPassword);
                }
				else
				{
					jiraClient = GetFakeJiraClient(basePath, jiraDomain);
				}
            }

			return new ArgsContext(action, gitTool, taskBranch, reposArg, gitHubClient, jiraClient);
		}

		private static FakeJiraClient GetFakeJiraClient(string basePath, string jiraDomain) =>
            new (jiraDomain, Path.Combine(basePath, "jiratasks.json"));

		private static string GetEnvironmentVariableOrThrow(EnvironmentVariable variable)
		{
			var value = variable.GetValue();
            return string.IsNullOrWhiteSpace(value) ? 
				throw new ArgsValidationException(EnvironmentVariablesCommands.Single(c => c.Variable == variable).ErrorMessage) :
				value;
        }

		private readonly struct EnvironmentVariableCommandInfo
		{
			public string Action { get; init; }
			public string ErrorMessage { get; init; }
			public EnvironmentVariable Variable { get; init; }
        }
    }
}
