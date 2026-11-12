using System.Text.Json;
using Gbm.Persistence.Entities;
using Gbm.Persistence.Repositories;
using Gbm.Unit.Tests.Shared;

namespace Gbm.Unit.Tests.Persistence.Repositories;

public class TaskInfoRepositoryTests
{
    [Fact]
    public void ConstructorRejectsRelativePath() => Assert.Throws<ArgumentException>(() => new TaskInfoRepository("jira", "relative.json"));

    [Fact]
    public async Task RoundTripAndUpdatesPreserveOtherTasksAndMappings()
    {
        using var directory = new TemporaryDirectory();
        var repo = new TaskInfoRepository("company", directory.File("tasksinfo.json"));
        Assert.Null(await repo.GetAsync("PROJ-123"));
        Assert.Equal("https://company.atlassian.net/browse/PROJ-123", repo.GetTaskUrl("PROJ-123"));
        await repo.SaveMetadataAsync("PROJ-123", "first", "description");
        await repo.UpsertRepositoryBranchAsync("proj-123", new("Api", "feature/api"));
        await repo.UpsertRepositoryBranchAsync("PROJ-123", new("Web", "fix/ui"));
        await repo.SaveMetadataAsync("PROJ-456", "other", "other description");
        await repo.SaveMetadataAsync("proj-123", "updated", "new description");
        await repo.UpsertRepositoryBranchAsync("proj-123", new("api", "hotfix/API"));
        var loaded = Assert.IsType<TaskInfo>(await repo.GetAsync("PROJ-123"));
        Assert.Equal("PROJ-123", loaded.Id);
        Assert.Equal("updated", loaded.Summary);
        Assert.Equal("new description", loaded.Description);
        Assert.Equal([new RepositoryBranch("Web", "fix/ui"), new RepositoryBranch("api", "hotfix/API")], loaded.Repositories);
        Assert.Empty((await repo.GetAsync("PROJ-456"))!.Repositories);
        loaded.Repositories.Clear();
        Assert.Equal(2, (await repo.GetAsync("proj-123"))!.Repositories.Count);
        var list = JsonSerializer.Deserialize<List<TaskInfo>>(await File.ReadAllTextAsync(directory.File("tasksinfo.json")))!;
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task SnapshotReplacesAllMappingsIncludingWithEmptyList()
    {
        using var directory = new TemporaryDirectory();
        var repo = new TaskInfoRepository("jira", directory.File("tasksinfo.json"));
        var task = new TaskInfo("P-1", "s", "d", "u", [new("Api", "old"), new("Web", "old")]);
        await repo.SaveSnapshotAsync(task);
        await repo.SaveSnapshotAsync(task with { Repositories = [new("Api", "new")] });
        Assert.Equal(new RepositoryBranch("Api", "new"), Assert.Single((await repo.GetAsync("p-1"))!.Repositories));
        await repo.SaveSnapshotAsync(task with { Repositories = [] });
        Assert.Empty((await repo.GetAsync("p-1"))!.Repositories);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{")]
    [InlineData("[{\"Id\":\"P-1\",\"Summary\":\"s\",\"Description\":\"d\",\"Url\":\"u\",\"BranchName\":\"old\"}]")]
    [InlineData("[{\"Id\":\"P-1\",\"Summary\":\"s\",\"Description\":\"d\",\"Url\":\"u\",\"Repositories\":null}]")]
    [InlineData("[{\"Id\":\"P-1\",\"Summary\":\"s\",\"Description\":\"d\",\"Url\":\"u\",\"Repositories\":[{\"Repository\":\"Api\",\"BranchName\":\"one\"},{\"Repository\":\"api\",\"BranchName\":\"two\"}]}]")]
    public async Task InvalidAndLegacyFilesCannotBeOverwritten(string original)
    {
        using var directory = new TemporaryDirectory();
        var file = directory.File("tasksinfo.json");
        await File.WriteAllTextAsync(file, original);
        var repo = new TaskInfoRepository("jira", file);
        await Assert.ThrowsAsync<InvalidDataException>(() => repo.GetAsync("P-1"));
        await Assert.ThrowsAsync<InvalidDataException>(() => repo.SaveMetadataAsync("P-2", "s", "d"));
        Assert.Equal(original, await File.ReadAllTextAsync(file));
    }

    [Fact]
    public async Task InvalidSnapshotAndCancellationLeaveBytesUnchanged()
    {
        using var directory = new TemporaryDirectory();
        var file = directory.File("tasksinfo.json");
        var repo = new TaskInfoRepository("jira", file);
        await repo.SaveMetadataAsync("P-1", "s", "d");
        var before = await File.ReadAllBytesAsync(file);
        var invalid = new TaskInfo("P-1", "s", "d", "u", [new("Api", "one"), new("api", "two")]);
        await Assert.ThrowsAsync<InvalidDataException>(() => repo.SaveSnapshotAsync(invalid));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.SaveMetadataAsync("P-1", "new", "new", cancellation.Token));
        Assert.Equal(before, await File.ReadAllBytesAsync(file));
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp"));
    }

    [Fact]
    public async Task ConcurrentUpsertsInOneInstanceDoNotLoseMappings()
    {
        using var directory = new TemporaryDirectory();
        var repo = new TaskInfoRepository("jira", directory.File("tasksinfo.json"));
        await repo.SaveMetadataAsync("P-1", "s", "d");
        await Task.WhenAll(Enumerable.Range(0, 12).Select(i => repo.UpsertRepositoryBranchAsync("P-1", new($"Repo{i}", $"branch/{i}"))));
        Assert.Equal(12, (await repo.GetAsync("P-1"))!.Repositories.Count);
    }
}
