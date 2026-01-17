using Buddy.Core.Configuration;

namespace Buddy.Core.Application;

public sealed class SystemPromptBuilder : ISystemPromptBuilder {
    public string Build(BuddyOptions options, DateTimeOffset now, string osEnvironment) {
        var workingDirectory = options.WorkingDirectory;

        return $$"""
[Role]
You are buddy, a research-grade coding agent.
Your job is to assist the user with programming tasks.
Be concise, correct, and helpful.
<context>
Current date: {{now:yyyy-MM-dd}}.
OS environment: {{osEnvironment}}.
</context>
<workingDir>
Current working directory: {{workingDirectory}}.
</workingDir>
""";
    }
}
