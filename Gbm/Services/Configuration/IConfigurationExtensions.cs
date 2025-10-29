using Gbm.Persistence.Configuration;
using Gbm.Services.Initialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json.Linq;
using RA.Console.DependencyInjection.Args;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;

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

            SetValueOnFile(configuration, variable, value);
        }

        private static void SetValueOnFile(this IConfiguration configuration, ConfigurationVariable variable, string value)
        {
            if (configuration is IConfigurationRoot root)
            {
                var jsonProvider = root.Providers.OfType<JsonConfigurationProvider>().FirstOrDefault();
                if (jsonProvider is null)
                    return;

                var source = jsonProvider.Source;
                var fileInfo = source.FileProvider?.GetFileInfo(source.Path) ?? new NotFoundFileInfo(source.Path);
                var fullPath = fileInfo.PhysicalPath;
                if (string.IsNullOrWhiteSpace(fullPath))
                    return;

                const string emptyJson = "{}";
                var jsonText = File.Exists(fullPath) ? File.ReadAllText(fullPath) : emptyJson;
                if (string.IsNullOrWhiteSpace(jsonText))
                    jsonText = emptyJson;

                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonText) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                data[variable.ToString()] = value;

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(fullPath, json);
            }
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
