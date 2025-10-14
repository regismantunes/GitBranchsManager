using Gbm.Git;
using Gbm.GitHub;
using Gbm.Jira;
using Gbm.Persistence.Repositories.Interfaces;

namespace Gbm.Commands.Args
{
    public record ArgsContext(
        string Action,
        GitTool? GitTool = null,
        string? TaskBranch = null,
        string[]? Repositories = null,
        GitHubClient? GitHubClient = null,
        IJiraClient? JiraClient = null,
        string? Environment = null,
        string? TaskId = null,
        ITaskInfoRepository? TaskInfoRepository = null,
        IPullRequestInfoRepository? PullRequestInfoRepository = null
    );
}