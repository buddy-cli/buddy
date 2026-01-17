using Buddy.Core.Configuration;
using Terminal.Gui;

namespace Buddy.Cli.Ui;

internal sealed record ChatControllerContext(
    BuddyOptions Options,
    string Version,
    string SystemPrompt,
    string? ProjectInstructions,
    ColorScheme IdleLogScheme,
    ColorScheme ActiveLogScheme,
    ChatLayoutParts LayoutParts);
