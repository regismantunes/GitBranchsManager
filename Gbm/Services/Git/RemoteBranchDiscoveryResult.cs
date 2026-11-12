namespace Gbm.Services.Git;

public record RemoteBranchDiscoveryResult(bool HasOrigin, IReadOnlyList<string> BranchNames);
