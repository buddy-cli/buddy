// using System.Text;
// using Terminal.Gui;
//
// namespace Buddy.Cli.Ui;
//
// /// <summary>
// /// Provides autocomplete suggestions for slash commands in a TextView.
// /// Filters commands by prefix match and highlights the matching portion.
// /// </summary>
// public class SlashCommandSuggestionGenerator : ISuggestionGenerator {
//     private readonly List<SlashCommandOption> _commands;
//
//     public SlashCommandSuggestionGenerator(List<SlashCommandOption> commands) {
//         _commands = commands ?? throw new ArgumentNullException(nameof(commands));
//     }
//
//     public bool IsWordChar(Rune rune) {
//         // Include '/' and alphanumeric characters as word characters for slash commands
//         return rune.Value == '/' || Rune.IsLetterOrDigit(rune);
//     }
//
//     public IEnumerable<Suggestion>? GenerateSuggestions(AutocompleteContext context) {
//         // context.CurrentLine contains a List<Cell> that represents the current word
//         if (context.CurrentLine is null || context.CurrentLine.Count == 0) {
//             return Enumerable.Empty<Suggestion>(); // Return empty to avoid null reference
//         }
//
//         // Extract the text from the Cell list
//         var currentWord = string.Concat(context.CurrentLine.Select(cell => cell.Rune.ToString()));
//
//         // Only activate if we're typing a slash command
//         if (!currentWord.StartsWith("/", StringComparison.Ordinal)) {
//             return Enumerable.Empty<Suggestion>(); // Return empty to avoid null reference
//         }
//
//         // Extract just the command part (without the leading "/")
//         var commandPrefix = currentWord.Length > 1 ? currentWord.Substring(1) : string.Empty;
//
//         // Filter commands that match the prefix
//         var matchingCommands = _commands
//             .Where(cmd => cmd.Command.StartsWith(commandPrefix, StringComparison.OrdinalIgnoreCase))
//             .Select(cmd => CreateSuggestion(cmd, currentWord.Length))
//             .ToList();
//
//         if (matchingCommands.Count == 0) {
//             return new[] { CreateNoMatchesSuggestion() };
//         }
//
//         return matchingCommands;
//     }
//
//     private Suggestion CreateSuggestion(SlashCommandOption command, int currentWordLength) {
//         // Build the full command text to insert
//         var fullCommand = "/" + command.Command + (string.IsNullOrEmpty(command.ParameterHint) ? "" : " ");
//
//         // Create a title with description for the popup
//         var title = $"/{command.Command.PadRight(15)} - {command.Description}";
//
//         // Suggestion(int remove, string replacement, string title)
//         // remove: number of characters to remove backwards from cursor
//         // replacement: text to insert
//         return new Suggestion(
//             remove: currentWordLength,  // Remove the current "/hel" etc.
//             replacement: fullCommand,   // Replace with full "/help "
//             title: title
//         );
//     }
//
//     private static Suggestion CreateNoMatchesSuggestion() {
//         return new Suggestion(
//             remove: 0,
//             replacement: string.Empty,
//             title: "No matches found"
//         );
//     }
// }
