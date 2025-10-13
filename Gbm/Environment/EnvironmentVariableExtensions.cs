using System.ComponentModel;

namespace Gbm.Environment
{
    internal static class EnvironmentVariableExtensions
    {
        public static string? GetValue(this EnvironmentVariable variable)
        {
            var value = GetName(variable);
            return System.Environment.GetEnvironmentVariable(value, EnvironmentVariableTarget.User) ??
                   System.Environment.GetEnvironmentVariable(value, EnvironmentVariableTarget.Machine);
        }

        private static string GetName(this EnvironmentVariable variable)
        {
            var type = typeof(EnvironmentVariable);
            var memberInfo = type.GetMember(variable.ToString());
            if (memberInfo.Length > 0)
            {
                var attributes = memberInfo[0].GetCustomAttributes(typeof(AmbientValueAttribute), false);
                if (attributes.Length > 0)
                {
                    var ambientValueAttribute = (AmbientValueAttribute)attributes[0];
                    return ambientValueAttribute.Value?.ToString() ?? throw new InvalidOperationException($"Environment variable for '{variable}' is not set.");
                }
            }
            throw new InvalidOperationException($"Environment variable for '{variable}' is not set.");
        }

        public static void SetValue(this EnvironmentVariable variable, string value, EnvironmentVariableTarget environmentVariableTarget = EnvironmentVariableTarget.User)
        {
            var name = GetName(variable);
            System.Environment.SetEnvironmentVariable(name, value, environmentVariableTarget);
        }
    }
}
