using Microsoft.Extensions.Configuration;

namespace Buddy.Core;

public static class BuddyOptionsLoader
{
    private static readonly Dictionary<string, string> CommandLineSwitchMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["--api-key"] = "Buddy:ApiKey",
        ["--model"] = "Buddy:Model",
        ["--base-url"] = "Buddy:BaseUrl",
        ["--working-directory"] = "Buddy:WorkingDirectory",
        ["--working-dir"] = "Buddy:WorkingDirectory"
    };

    public static BuddyOptions Load(string workingDirectory, string[] args)
    {
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            workingDirectory = Directory.GetCurrentDirectory();
        }

        var config = new ConfigurationBuilder()
            .SetBasePath(workingDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args, CommandLineSwitchMappings)
            .Build();

        var options = new BuddyOptions
        {
            WorkingDirectory = workingDirectory
        };

        // appsettings.json support: { "Buddy": { ... } }
        config.GetSection("Buddy").Bind(options);

        // Environment variable compatibility (recommended in roadmap)
        // Precedence: Buddy:* (incl. CLI switches) > BUDDY_* fallbacks > defaults
        options.ApiKey = config["Buddy:ApiKey"] ?? config["BUDDY_API_KEY"] ?? options.ApiKey;
        options.Model = config["Buddy:Model"] ?? config["BUDDY_MODEL"] ?? options.Model;
        options.BaseUrl = config["Buddy:BaseUrl"] ?? config["BUDDY_BASE_URL"] ?? options.BaseUrl;
        options.WorkingDirectory = config["Buddy:WorkingDirectory"] ?? config["BUDDY_WORKING_DIRECTORY"] ?? options.WorkingDirectory;

        if (string.IsNullOrWhiteSpace(options.WorkingDirectory))
        {
            options.WorkingDirectory = workingDirectory;
        }

        return options;
    }
}
