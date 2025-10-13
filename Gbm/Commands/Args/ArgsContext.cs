using Gbm.Git;
using Gbm.GitHub;
using Gbm.Jira;

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
        string? TaskId = null
    );
}