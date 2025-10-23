using Gbm.Persistence.Configuration;
using Gbm.Persistence.Repositories.Interfaces;
using Gbm.Services.Git;
using Gbm.Services.GitHub;
using Gbm.Services.Initialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RA.Console.DependencyInjection.Args;

namespace Gbm.Unit.Tests.Services.Initialization;

public class IServiceCollectionExtensionsTests
{
    private static ServiceProvider BuildProvider(IDictionary<string, string?> cfg)
    {
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(cfg)
            .Build();
        services.AddSingleton(configuration);
        services
            .AddGitTool()
            .AddGitHubClient()
            .AddTaskInfoRepository()
            .AddPullRequestInfoRepository();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Resolve_GitTool_Throws_WhenMissingBasePath()
    {
        var provider = BuildProvider(new Dictionary<string, string?>());
        var ex = Assert.Throws<ArgsValidationException>(() => provider.GetRequiredService<IGitTool>());
        Assert.Equal(FailMessages.MissingConfigurationVariableMessages[ConfigurationVariable.BasePath], ex.Message);
    }

    [Fact]
    public void Resolve_GitHubClient_Throws_WhenMissingToken()
    {
        var provider = BuildProvider(new Dictionary<string, string?>
        {
            { ConfigurationVariable.BasePath.ToString(), "C:/tmp" }
        });
        var ex = Assert.Throws<ArgsValidationException>(() => provider.GetRequiredService<IGitHubClient>());
        Assert.Equal(FailMessages.MissingConfigurationVariableMessages[ConfigurationVariable.GitHubToken], ex.Message);
    }

    [Fact]
    public void Resolve_TaskInfoRepository_Throws_WhenMissingJiraDomain()
    {
        var provider = BuildProvider(new Dictionary<string, string?>
        {
            { ConfigurationVariable.BasePath.ToString(), "C:/tmp" }
        });
        var ex = Assert.Throws<ArgsValidationException>(() => provider.GetRequiredService<ITaskInfoRepository>());
        Assert.Equal(FailMessages.MissingConfigurationVariableMessages[ConfigurationVariable.JiraDomain], ex.Message);
    }

    [Fact]
    public void Resolve_Services_Succeeds_WithAllValues()
    {
        var provider = BuildProvider(new Dictionary<string, string?>
        {
            { ConfigurationVariable.BasePath.ToString(), Path.GetTempPath() },
            { ConfigurationVariable.GitHubToken.ToString(), "t" },
            { ConfigurationVariable.GitHubRepositoriesOwner.ToString(), "owner" },
            { ConfigurationVariable.JiraDomain.ToString(), "myjira" }
        });

        Assert.NotNull(provider.GetRequiredService<IGitTool>());
        Assert.NotNull(provider.GetRequiredService<IGitHubClient>());
        Assert.NotNull(provider.GetRequiredService<ITaskInfoRepository>());
    }
}
