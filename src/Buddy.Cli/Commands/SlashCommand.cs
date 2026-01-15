namespace Buddy.Cli.Commands;

internal sealed record SlashCommand(
    string Name,
    string? Description = null,
    string? Parameters = null,
    IReadOnlyList<string>? Aliases = null
);
