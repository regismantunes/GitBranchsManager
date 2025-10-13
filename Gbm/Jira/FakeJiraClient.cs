
using System.Text.Json;

namespace Gbm.Jira
{
    public class FakeJiraClient : IJiraClient
    {
        private readonly string _jiraDomain;
        private readonly string _jsonTaskFile;
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public FakeJiraClient(string jiraDomain, string jsonTaskFile)
        {
            _jiraDomain = jiraDomain ?? throw new ArgumentNullException(nameof(jiraDomain));
            _jsonTaskFile = jsonTaskFile ?? throw new ArgumentNullException(nameof(jsonTaskFile));

            if (!Path.IsPathFullyQualified(_jsonTaskFile))
                throw new ArgumentException("Json task file invalid path.");
        }

        public async Task<TaskInfo?> GetTaskInfoAsync(string taskId, CancellationToken cancellationToken = default)
        {
            var taskList = await GetTaskInfosAsync(cancellationToken);
            return taskList?.FirstOrDefault(t => t.Id.Equals(taskId, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<List<TaskInfo>> GetTaskInfosAsync(CancellationToken cancellationToken = default)
        {
            var json = await File.ReadAllTextAsync(_jsonTaskFile, cancellationToken);
            return JsonSerializer.Deserialize<List<TaskInfo>>(json) ?? [];
        }

        public async Task SaveTaskInfoAsync(string taskId, string taskSummary, string taskDescription, CancellationToken cancellationToken = default)
        {
            var jsonList = File.Exists(_jsonTaskFile) ?
                await GetTaskInfosAsync(cancellationToken) :
                [];

            if (jsonList.Exists(t => t.Id == taskId))
                jsonList.RemoveAll(t => t.Id == taskId);

            jsonList.Add(new TaskInfo(taskId, taskSummary, taskDescription, GetTaskUrl(taskId)));

            var json = JsonSerializer.Serialize(jsonList, _jsonOptions);
            await File.WriteAllTextAsync(_jsonTaskFile, json, cancellationToken);
        }

        public string GetTaskUrl(string taskId) => $"https://{_jiraDomain}.atlassian.net/browse/{taskId}";
    }
}
