using Microsoft.Extensions.Configuration;

namespace Altium.Shared;

public static class ConfigurationBuilderExtensions
{
    private const string _appsettingsFileName = "appsettings";
    private const string _appsettingsFileExtension = ".json";

    public static void BuildConfig(this IConfigurationBuilder builder)
    {
        builder
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile($"{_appsettingsFileName}{_appsettingsFileExtension}", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }
}