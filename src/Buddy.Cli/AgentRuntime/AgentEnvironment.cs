namespace Buddy.Cli.AgentRuntime;

public record AgentEnvironment(string Version, string WorkingDirectory, DateTimeOffset CurrentDate, string OsEnvironment) {
}