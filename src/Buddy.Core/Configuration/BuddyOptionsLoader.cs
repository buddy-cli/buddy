using Microsoft.Extensions.Configuration;

namespace Buddy.Core.Configuration;

public static class BuddyOptionsLoader {
    public static BuddyOptions Load(string workingDirectory, string[] args, string? configPath = null) {
        if (string.IsNullOrWhiteSpace(workingDirectory)) {
            workingDirectory = Directory.GetCurrentDirectory();
        }

        configPath ??= ResolveConfigPath();
        EnsureConfigExists(configPath);

        var config = new ConfigurationBuilder()
            .SetBasePath(workingDirectory)
            .AddJsonFile(configPath, optional: false)
            .Build();

        var options = new BuddyOptions {
            WorkingDirectory = workingDirectory
        };

        config.Bind(options);

        ApplyPrimaryProviderDefaults(options);

        if (string.IsNullOrWhiteSpace(options.WorkingDirectory)) {
            options.WorkingDirectory = workingDirectory;
        }

        return options;
    }

    private static void ApplyPrimaryProviderDefaults(BuddyOptions options) {
        if (options.Providers.Count == 0) {
            return;
        }

        var provider = options.Providers[0];
        options.BaseUrl = provider.BaseUrl;
        options.ApiKey = provider.ApiKey;

        if (provider.Models.Count == 0) {
            return;
        }

        options.Model = provider.Models[0].System;
    }

    private static string ResolveConfigPath() {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDirectory, ".buddy", "config.json");
    }

    private static void EnsureConfigExists(string configPath) {
        if (File.Exists(configPath)) {
            return;
        }

        var directory = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
        }

        var defaultConfig = new BuddyConfigFile {
            Providers = new List<LlmProviderConfig>()
        };

        var json = System.Text.Json.JsonSerializer.Serialize(
            defaultConfig,
            new System.Text.Json.JsonSerializerOptions {
                WriteIndented = true
            });

        File.WriteAllText(configPath, json);
    }
}

public sealed class BuddyConfigFile {
    public List<LlmProviderConfig> Providers { get; set; } = new();
}
