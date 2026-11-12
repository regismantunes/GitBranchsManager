namespace Gbm.Persistence.Entities
{
    public record TaskInfo(string Id, string Summary, string Description, string Url, List<RepositoryBranch> Repositories);

    public record RepositoryBranch(string Repository, string BranchName);
}
