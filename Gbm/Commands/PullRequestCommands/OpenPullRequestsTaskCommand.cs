using Gbm.Persistence.Entities;
using Gbm.Persistence.Repositories.Interfaces;
using Gbm.Services.Build;
using Gbm.Services.Git;
using Gbm.Services.GitHub;
using Gbm.Services.Tasks;
using RA.Console.DependencyInjection.Attributes;

namespace Gbm.Commands.PullRequestCommands;

public class OpenPullRequestsTaskCommand(IPullRequestInfoRepository repository, IGitHubClient gitHubClient, IGitTool gitTool, IBuildTool dotnetTool)
{
    [CommandAsyncWithArgsBuilderAsync<OpenPullRequestsTaskCommandArgsBuilder>("-pr",
        Description = "Create PRs using each repository's mapped task branch and update related links",
        Example = "gbm -pr <TaskId> [nopush] [nobuild] [Repos...]", Group = CommandGroups.PullRequests, Order = 0)]
    public async Task<int> ExecuteAsync(TaskInfo taskInfo, RepositoryBranchTarget[] targets, bool pushLocalChanges = true,
        bool noBuild = false, CancellationToken cancellationToken = default)
    {
        MyConsole.WriteCommandHeader($"🔧 Creating/updating PRs for {taskInfo.Id}: {taskInfo.Summary}");
        var pending = new List<RepositoryBranchTarget>();
        foreach (var target in targets)
            if (!await repository.ExistsAsync(taskInfo.Id, target.Repository, cancellationToken)) pending.Add(target);

        if (!noBuild)
            foreach (var target in pending)
                if (!await dotnetTool.BuildBranchAsync(target.Repository, target.BranchName, cancellationToken))
                {
                    MyConsole.WriteError($"❌ Build failed in '{target.Repository}'. No PRs were created and nothing was pushed.");
                    return 1;
                }

        await BranchExecution.ForEachAsync(pending, async target =>
        {
            await BranchExecution.CheckoutRequiredAsync(gitTool, target, cancellationToken);
            if (pushLocalChanges) await gitTool.PushAsync(cancellationToken);
            var baseBranch = await gitTool.GetMainBranchAsync(cancellationToken);
            var pr = await gitHubClient.CreatePullRequestAsync(target.Repository, target.BranchName, baseBranch, taskInfo, cancellationToken);
            await repository.SaveAsync(pr, cancellationToken);
            MyConsole.WriteSucess($"✅ PR saved: {pr.Repository} -> {pr.Url}");
        });

        var related = await repository.GetByTaskIdAsync(taskInfo.Id, cancellationToken);
        var links = related.Count == 0 ? string.Empty : "**Related PRs:**\n" + string.Join('\n', related.Select(p => $"- [{p.Repository}]({p.Url})"));
        foreach (var pr in related)
        {
            try { await gitHubClient.UpdatePullRequestAsync(pr.Repository, pr.Number, links, taskInfo, cancellationToken); }
            catch
            {
                MyConsole.WriteError($"❌ Updating related links failed in '{pr.Repository}'. Previously created PRs and pushes were retained.");
                throw;
            }
        }
        foreach (var pr in related) MyConsole.WriteInfo($"🔗 {pr.Repository}: {pr.Url}");
        return 0;
    }
}
