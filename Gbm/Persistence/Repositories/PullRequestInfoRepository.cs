using Gbm.Persistence.Entities;
using Gbm.Persistence.Repositories.Interfaces;
using System.Text.Json;

namespace Gbm.Persistence.Repositories
{
    public class PullRequestInfoRepository : IPullRequestInfoRepository
    {
        private readonly string _jsonPullRequestFile;
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public PullRequestInfoRepository(string jsonPullRequestFile)
        {
            _jsonPullRequestFile = jsonPullRequestFile ?? throw new ArgumentNullException(nameof(jsonPullRequestFile));

            if (!Path.IsPathFullyQualified(_jsonPullRequestFile))
                throw new ArgumentException("Json pull request file invalid path.");
        }

        public async Task<PullRequestInfo?> GetAsync(string taskId, string repository, CancellationToken cancellationToken = default)
        {
            var taskList = await GetListAsync(cancellationToken);
            return taskList?.FirstOrDefault(t =>
                t.TaskId.Equals(taskId, StringComparison.OrdinalIgnoreCase) &&
                t.Repository.Equals(repository, StringComparison.OrdinalIgnoreCase)
                );
        }

        private async Task<List<PullRequestInfo>> GetListAsync(CancellationToken cancellationToken = default)
        {
            if (!File.Exists(_jsonPullRequestFile))
                return [];

            var json = await File.ReadAllTextAsync(_jsonPullRequestFile, cancellationToken);
            return JsonSerializer.Deserialize<List<PullRequestInfo>>(json) ?? [];
        }

        public Task SaveAsync(string taskId, string repository, int number, string url, CancellationToken cancellationToken = default)
        {
            return SaveAsync(new PullRequestInfo(taskId, repository, number, url), cancellationToken);
        }

        public async Task SaveAsync(PullRequestInfo prInfo, CancellationToken cancellationToken = default)
        {
            var jsonList = await GetListAsync(cancellationToken);
            
            if (jsonList.Exists(t => t.TaskId == prInfo.TaskId && t.Repository == prInfo.Repository))
                jsonList.RemoveAll(t => t.TaskId == prInfo.TaskId && t.Repository == prInfo.Repository);
            
            jsonList.Add(prInfo);
            
            var json = JsonSerializer.Serialize(jsonList, _jsonOptions);
            await File.WriteAllTextAsync(_jsonPullRequestFile, json, cancellationToken);
        }

        public async Task<List<PullRequestInfo>> GetByTaskIdAsync(string taskId, CancellationToken cancellationToken = default)
        {
            var taskList = await GetListAsync(cancellationToken);
            return taskList?.Where(t => t.TaskId.Equals(taskId, StringComparison.OrdinalIgnoreCase)).ToList() ?? [];
        }

        public async Task<bool> ExistsAsync(string taskId, string repository, CancellationToken cancellationToken = default)
        {
            var pr = await GetAsync(taskId, repository, cancellationToken);
            return pr != null;
        }
    }
}
