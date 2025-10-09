using Gbm.Git;

namespace Gbm.Commands.Args
{
	public static class ArgsBuilder
	{
		private const string MissingBasePathMessage = """
			❌ Base path is not set.
			Use the command: gbm -b <BasePath>
			""";
        private const string MissingGitHubTokenMessage = """
			❌ GitHub Token is not set.
			Use the command: gbm -gt <GitHubToken>
			""";

        public static ArgsContext Build(string[] args)
		{
			if (args == null ||
				args.Length == 0 ||
				args[0] == "-h" ||
				args[0] == "--help")
				return new ArgsContext("-h");

			var action = args[0];
			if (action == "-b" ||
				action == "-gt")
			{
                if (args.Length < 2)
					throw new ArgsValidationException(action == "-b" ? 
													MissingBasePathMessage : 
													MissingGitHubTokenMessage);
                var singleArg = args[1];
                return action == "-b" ?
					new ArgsContext(action, BasePath: singleArg) : 
					new ArgsContext(action, GitHubToken: singleArg);
            }

			if (!ProgramCommandBuilder.IsValidAction(action))
				throw new ArgsValidationException($"Unknown action '{action}'. Use -h for help.");

            if (args.Length < 2) throw new ArgsValidationException($"Missing TaskId. Example: gbm {action} SSM-903 [Repo1 Repo2 ...]");
			if (action == "-n" && args.Length < 3) throw new ArgsValidationException("Missing repositories. Example: gbm -n SSM-903 Repo1 Repo2");

			var taskBranch = $"feature/{args[1]}";
			var reposArg = args.Length > 2 ? args[2..] : [];
			var basePathEnvVar = EnvironmentVariables.BasePath;
            var basePath = Environment.GetEnvironmentVariable(basePathEnvVar, EnvironmentVariableTarget.User)
						?? Environment.GetEnvironmentVariable(basePathEnvVar, EnvironmentVariableTarget.Machine);
			if (string.IsNullOrWhiteSpace(basePath)) throw new ArgsValidationException(MissingBasePathMessage);
			var gitTool = new GitTool(basePath) { ShowGitOutput = false };
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
			string? gitHubToken = null;
            if (action == "-pr")
			{
				var githubTokenEnvVar = EnvironmentVariables.GitHubToken;
				gitHubToken = Environment.GetEnvironmentVariable(githubTokenEnvVar, EnvironmentVariableTarget.User)
						?? Environment.GetEnvironmentVariable(githubTokenEnvVar, EnvironmentVariableTarget.Machine);
				if (string.IsNullOrWhiteSpace(gitHubToken)) throw new ArgsValidationException(MissingGitHubTokenMessage);
            }

			return new ArgsContext(action, basePath, gitTool, taskBranch, reposArg, gitHubToken);
		}
	}
}
