using System.Text.Json;
using Gbm.Persistence.Entities;
using Gbm.Persistence.Repositories;

namespace Gbm.Unit.Tests.Persistence.Repositories;

public class TaskInfoRepositoryTests
{
    private static string TempFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"gbm-tests-{Guid.NewGuid():N}.json");
        return path;
    }

    [Fact]
    public void Ctor_Throws_OnRelativePath()
    {
        Assert.Throws<ArgumentException>(() => new TaskInfoRepository("jira", "relative.json"));
    }

    [Fact]
    public void GetTaskUrl_ComposesCorrectUrl()
    {
        var repo = new TaskInfoRepository("company", Path.GetFullPath(TempFile()));
        Assert.Equal("https://company.atlassian.net/browse/GBM-123", repo.GetTaskUrl("GBM-123"));
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenFileMissing()
    {
        var repo = new TaskInfoRepository("jira", Path.GetFullPath(TempFile()));
        var result = await repo.GetAsync("GBM-1");
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_WritesAndGetsItem()
    {
        var file = Path.GetFullPath(TempFile());
        try
        {
            var repo = new TaskInfoRepository("jira", file);
            await repo.SaveAsync("GBM-1", "sum", "desc", "feature/GBM-1");

            var loaded = await repo.GetAsync("GBM-1");
            Assert.NotNull(loaded);
            Assert.Equal("GBM-1", loaded!.Id);
            Assert.Equal("feature/GBM-1", loaded.BranchName);
            Assert.Equal("https://jira.atlassian.net/browse/GBM-1", loaded.Url);
        }
        finally
        {
            if (File.Exists(file)) File.Delete(file);
        }
    }

    [Fact]
    public async Task SaveAsync_ReplacesExisting()
    {
        var file = Path.GetFullPath(TempFile());
        try
        {
            var repo = new TaskInfoRepository("jira", file);
            await repo.SaveAsync("GBM-2", "sum1", "desc1", "branch1");
            await repo.SaveAsync("GBM-2", "sum2", "desc2", "branch2");

            var json = await File.ReadAllTextAsync(file);
            var list = JsonSerializer.Deserialize<List<TaskInfo>>(json)!;
            Assert.Single(list);
            Assert.Equal("branch2", list[0].BranchName);
        }
        finally
        {
            if (File.Exists(file)) File.Delete(file);
        }
    }
}
