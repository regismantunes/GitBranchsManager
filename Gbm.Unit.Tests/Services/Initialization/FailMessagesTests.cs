using System;
using System.Linq;
using Gbm.Persistence.Configuration;
using Gbm.Services.Initialization;
using Xunit;

namespace Gbm.Unit.Tests.Services.Initialization;

public class FailMessagesTests
{
    [Fact]
    public void MissingConfigurationVariableMessages_HasAllEnumKeys()
    {
        var enumValues = Enum.GetValues(typeof(ConfigurationVariable)).Cast<ConfigurationVariable>().ToArray();
        var dict = FailMessages.MissingConfigurationVariableMessages;
        foreach (var v in enumValues)
        {
            Assert.True(dict.ContainsKey(v), $"Missing message for {v}");
            Assert.False(string.IsNullOrWhiteSpace(dict[v]));
        }
    }
}
