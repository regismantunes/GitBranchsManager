using Gbm.Persistence.Repositories;
using Gbm.Persistence.Repositories.Interfaces;
using Gbm.Services.Git;
using Gbm.Services.GitHub;
using Gbm.Services.Jira;
using Gbm.Services.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Gbm.Persistence.Configuration;

namespace Gbm.Services.Initialization
{
    public static class IServiceCollectionExtensions
    {
        private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Gbm");

        public static IServiceCollection AddAllServices(this IServiceCollection services)
        {
            return services
                .AddConfiguration()
                .AddJiraClient()
                .AddGitHubClient()
                .AddGitTool()
                .AddPullRequestInfoRepository()
                .AddTaskInfoRepository();
        }

        public static IServiceCollection AddConfiguration(this IServiceCollection services)
        {
            var configFilePath = Path.Combine(AppDataFolder, "config.json");

            Directory.CreateDirectory(AppDataFolder);
            if (!File.Exists(configFilePath))
                File.WriteAllText(configFilePath, "{}");

            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDataFolder)
                .AddJsonFile(Path.GetFileName(configFilePath), optional: false, reloadOnChange: false);

            var configuration = builder.Build();
            services.AddSingleton<IConfiguration>(configuration);
            return services;
        }

        public static IServiceCollection AddJiraClient(this IServiceCollection services)
        {
            return services.AddSingleton<IJiraClient>(s =>
            {
                var configuration = s.GetRequiredService<IConfiguration>();
                var jiraDomain = configuration.GetValueOrThrow(ConfigurationVariable.JiraDomain);
                var jiraConsumerKey = configuration.GetValue(ConfigurationVariable.JiraConsumerKey);
                var jiraConsumerSecrety = configuration.GetValue(ConfigurationVariable.JiraConsumerSecret);
                var jiraAcccessToken = configuration.GetValue(ConfigurationVariable.JiraAccessToken);
                var jiraTokenSecrety = configuration.GetValue(ConfigurationVariable.JiraTokenSecrety);
                var jiraUserMail = configuration.GetValue(ConfigurationVariable.JiraUserMail);
                var jiraUserPassword = configuration.GetValue(ConfigurationVariable.JiraUserPassword);
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
                var configuration = s.GetRequiredService<IConfiguration>();
                var basePath = configuration.GetValueOrThrow(ConfigurationVariable.BasePath);
                return new GitTool(basePath);
            });
        }

        public static IServiceCollection AddGitHubClient(this IServiceCollection services)
        {
            return services.AddSingleton<IGitHubClient>(s =>
            {
                var configuration = s.GetRequiredService<IConfiguration>();
                var token = configuration.GetValueOrThrow(ConfigurationVariable.GitHubToken);
                var repositoriesOwner = configuration.GetValueOrThrow(ConfigurationVariable.GitHubRepositoriesOwner);
                return new GitHubClient(token, repositoriesOwner);
            });
        }

        public static IServiceCollection AddTaskInfoRepository(this IServiceCollection services)
        {
            return services.AddSingleton<ITaskInfoRepository>(s =>
            {
                var configuration = s.GetRequiredService<IConfiguration>();
                var jiraDomain = configuration.GetValueOrThrow(ConfigurationVariable.JiraDomain);
                var basePath = configuration.GetValueOrThrow(ConfigurationVariable.BasePath);
                return new TaskInfoRepository(jiraDomain, Path.Combine(basePath, "tasksinfo.json"));
            });
        }

        public static IServiceCollection AddPullRequestInfoRepository(this IServiceCollection services)
        {
            return services.AddSingleton<IPullRequestInfoRepository>(s =>
            {
                var configuration = s.GetRequiredService<IConfiguration>();
                var basePath = configuration.GetValueOrThrow(ConfigurationVariable.BasePath);
                return new PullRequestInfoRepository(Path.Combine(basePath, "pullrequestsinfo.json"));
            });
        }
    }
}
