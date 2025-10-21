using Gbm.Persistence.Environment;
using Gbm.Persistence.Repositories;
using Gbm.Persistence.Repositories.Interfaces;
using Gbm.Services.Extensions;
using Gbm.Services.Git;
using Gbm.Services.GitHub;
using Gbm.Services.Jira;
using Microsoft.Extensions.DependencyInjection;

namespace Gbm.Services.Initialization
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddAllServices(this IServiceCollection services)
        {
            return services
                .AddJiraClient()
                .AddGitHubClient()
                .AddGitTool()
                .AddPullRequestInfoRepository()
                .AddTaskInfoRepository();
        }

        public static IServiceCollection AddJiraClient(this IServiceCollection services)
        {
            return services.AddSingleton<IJiraClient>(s =>
            {
                var jiraDomain = EnvironmentVariable.JiraDomain.GetValueOrThrow();
                var jiraConsumerKey = EnvironmentVariable.JiraConsumerKey.GetValue();
                var jiraConsumerSecrety = EnvironmentVariable.JiraConsumerSecret.GetValue();
                var jiraAcccessToken = EnvironmentVariable.JiraAccessToken.GetValue();
                var jiraTokenSecrety = EnvironmentVariable.JiraTokenSecrety.GetValue();
                var jiraUserMail = EnvironmentVariable.JiraUserMail.GetValue();
                var jiraUserPassword = EnvironmentVariable.JiraUserPassword.GetValue();
                var taskInfoRepository = s.GetRequiredService<ITaskInfoRepository>();
                if (jiraConsumerKey is not null &&
                    jiraConsumerSecrety is not null &&
                    jiraAcccessToken is not null &&
                    jiraTokenSecrety is not null)
                {
                    return new JiraClient(taskInfoRepository, jiraDomain, jiraConsumerKey, jiraConsumerKey, jiraAcccessToken, jiraTokenSecrety);
                }
                else if (jiraUserMail is not null &&
                    jiraUserPassword is not null)
                {
                    return new JiraClient(taskInfoRepository, jiraDomain, jiraUserMail, jiraUserPassword);
                }
                else
                {
                    return new FakeJiraClient(taskInfoRepository);
                }
            });
        }

        public static IServiceCollection AddGitTool(this IServiceCollection services)
        {
            return services.AddSingleton<IGitTool>(s => 
            { 
                var basePath = EnvironmentVariable.BasePath.GetValueOrThrow();
                return new GitTool(basePath);
            });
        }

        public static IServiceCollection AddGitHubClient(this IServiceCollection services)
        {
            return services.AddSingleton<IGitHubClient>(s =>
            {
                var token = EnvironmentVariable.GitHubToken.GetValueOrThrow();
                var repositoriesOwner = EnvironmentVariable.GitHubRepositoriesOwner.GetValueOrThrow();
                return new GitHubClient(
                    token,
                    repositoriesOwner);
            });
        }

        public static IServiceCollection AddTaskInfoRepository(this IServiceCollection services)
        {
            return services.AddSingleton<ITaskInfoRepository>(s =>
            {
                var jiraDomain = EnvironmentVariable.JiraDomain.GetValueOrThrow();
                var basePath = EnvironmentVariable.BasePath.GetValueOrThrow();
                return new TaskInfoRepository(jiraDomain, Path.Combine(basePath, "tasksinfo.json"));
            });
        }

        public static IServiceCollection AddPullRequestInfoRepository(this IServiceCollection services)
        {
            return services.AddSingleton<IPullRequestInfoRepository>(s =>
            {
                var basePath = EnvironmentVariable.BasePath.GetValueOrThrow();
                return new PullRequestInfoRepository(Path.Combine(basePath, "pullrequestsinfo.json"));
            });
        }
    }
}
