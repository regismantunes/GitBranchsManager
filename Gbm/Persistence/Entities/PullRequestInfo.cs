namespace Gbm.Persistence.Entities
{
    public record PullRequestInfo(string TaskId, string Repository, int Number, string Url);
}