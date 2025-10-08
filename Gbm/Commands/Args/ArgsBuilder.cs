using Gbm.Git;

namespace Gbm.Commands.Args
{
	public static class ArgsBuilder
	{
		public static ArgsContext Build(string[] args)
		{
			if (args == null ||
				args.Length == 0 ||
				args[0] == "-h" ||
				args[0] == "--help")
				return new ArgsContext("-h");

			var action = args[0];
			if (action == "-b")
			{
				if (args.Length < 2) throw new ArgsValidationException("Missing BasePath. Example: gbm -b D:\\Source\\BB");
				var basePathOnly = args[1];
				return new ArgsContext(action, basePathOnly);
			}

			if (!ProgramCommandBuilder.IsValidAction(action))
				throw new ArgsValidationException($"Unknown action '{action}'. Use -h for help.");

            if (args.Length < 2) throw new ArgsValidationException("Missing TaskId. Example: gbm -d SSM-903 [Repo1 Repo2 ...]");
			if (action == "-n" && args.Length < 3) throw new ArgsValidationException("Missing repositories. Example: gbm -n SSM-903 Repo1 Repo2");

			var taskBranch = $"feature/{args[1]}";
			var reposArg = args.Length > 2 ? args[2..] : [];
			var envVarName = EnvironmentVariables.BasePath;
            var basePath = Environment.GetEnvironmentVariable(envVarName, EnvironmentVariableTarget.User)
						?? Environment.GetEnvironmentVariable(envVarName, EnvironmentVariableTarget.Machine);
			if (string.IsNullOrWhiteSpace(basePath)) throw new ArgsValidationException($"Base path not set. Use: gbm -b <BasePath>");
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
                        return gitTool.BranchExists(taskBranch);
					})
					.ToArray()!;

				if (reposArg.Length == 0)
					throw new ArgsValidationException($"No repositories found with branch '{taskBranch}'.");
            }

			return new ArgsContext(action, basePath, gitTool, taskBranch, reposArg);
		}
	}
}
