namespace Gbm.Jira
{
    public interface IJiraClient
    {
        Task<TaskInfo?> GetTaskInfoAsync(string taskId, CancellationToken cancellationToken = default);
        string GetTaskUrl(string taskId);
    }
}
