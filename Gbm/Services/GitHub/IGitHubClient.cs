using Gbm.Persistence.Entities;

namespace Gbm.Services.GitHub
{
    public interface IGitHubClient
    {
        string Owner { get; }

        Task<PullRequestInfo> CreatePullRequestAsync(string repo, string branchName, string baseBranch, TaskInfo taskInfo, CancellationToken cancellationToken = default);
        Task UpdatePullRequestAsync(string repo, int prNumber, string relatedPRsText, TaskInfo taskInfo, CancellationToken cancellationToken = default);
    }
}