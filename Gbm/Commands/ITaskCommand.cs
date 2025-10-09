using Gbm.Git;

namespace Gbm.Commands
{
	public interface ITaskCommand
	{
		Task<int> ExecuteAsync(GitTool gitTool, string taskBranch, string[] repositories);
	}
}