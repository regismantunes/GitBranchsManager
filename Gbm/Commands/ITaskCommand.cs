using Gbm.Git;

namespace Gbm.Commands
{
	public interface ITaskCommand
	{
		int Execute(GitTool gitTool, string taskBranch, string[] repositories);
	}
}