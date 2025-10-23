using Gbm.Services.Initialization;
using RA.Console.DependencyInjection.Args;
using Gbm.Persistence.Configuration;
using Microsoft.Extensions.Configuration;

namespace Gbm.Services.Configuration
{
    internal static class IConfigurationExtensions
    {
        public static string? GetValue(this IConfiguration configuration, ConfigurationVariable variable)
        {
            return configuration[variable.ToString()];
        }

        public static void SetValue(this IConfiguration configuration, ConfigurationVariable variable, string value)
        {
            configuration[variable.ToString()] = value;
        }

        public static string GetValueOrThrow(this IConfiguration configuration, ConfigurationVariable variable)
        {
            var value = configuration.GetValue(variable);
            return string.IsNullOrWhiteSpace(value) ?
                throw new ArgsValidationException(FailMessages.MissingConfigurationVariableMessages[variable]) :
                value;
        }
    }
}
