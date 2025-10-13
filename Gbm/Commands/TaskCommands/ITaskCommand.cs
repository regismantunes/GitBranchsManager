using Gbm.Git;

namespace Gbm.Commands.TaskCommands
{
	public interface ITaskCommand
	{
		Task<int> ExecuteAsync(GitTool gitTool, string taskBranch, string[] repositories);
	}
}