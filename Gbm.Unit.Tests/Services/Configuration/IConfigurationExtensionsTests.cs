using Gbm.Persistence.Configuration;
using Gbm.Services.Configuration;
using Gbm.Services.Initialization;
using Microsoft.Extensions.Configuration;
using RA.Console.DependencyInjection.Args;

namespace Gbm.Unit.Tests.Services.Configuration;

public class IConfigurationExtensionsTests
{
    private static IConfiguration BuildConfig(IDictionary<string, string?>? values = null)
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values ?? new Dictionary<string, string?>());
        return builder.Build();
    }

    [Fact]
    public void GetValue_ReturnsValue_WhenPresent()
    {
        var cfg = BuildConfig(new Dictionary<string, string?>
        {
            { ConfigurationVariable.BasePath.ToString(), "C:/tmp" }
        });

        var value = cfg.GetValue(ConfigurationVariable.BasePath);
        Assert.Equal("C:/tmp", value);
    }

    [Fact]
    public void SetValue_SetsValue()
    {
        var cfg = BuildConfig();
        cfg.SetValue(ConfigurationVariable.GitHubToken, "token123");
        Assert.Equal("token123", cfg[ConfigurationVariable.GitHubToken.ToString()]);
    }

    [Fact]
    public void GetValueOrThrow_Throws_WithMessage()
    {
        var cfg = BuildConfig();
        var ex = Assert.Throws<ArgsValidationException>(() => cfg.GetValueOrThrow(ConfigurationVariable.BasePath));
        Assert.Equal(FailMessages.MissingConfigurationVariableMessages[ConfigurationVariable.BasePath], ex.Message);
    }
}
