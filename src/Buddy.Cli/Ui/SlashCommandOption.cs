namespace Buddy.Cli.Ui;

/// <summary>
/// Represents a slash command option with its name and description.
/// </summary>
public class SlashCommandOption
{
    /// <summary>
    /// The command name (without the leading "/"). e.g., "help", "clear", "model"
    /// </summary>
    public required string Command { get; init; }
    
    /// <summary>
    /// A brief description of what the command does.
    /// </summary>
    public required string Description { get; init; }
    
    /// <summary>
    /// Optional parameter hint to display (e.g., "&lt;name&gt;" for /model).
    /// </summary>
    public string? ParameterHint { get; init; }

    /// <summary>
    /// Gets the full display text for this command option.
    /// </summary>
    public string DisplayText => 
        string.IsNullOrEmpty(ParameterHint) 
            ? $"/{Command}" 
            : $"/{Command} {ParameterHint}";
}
