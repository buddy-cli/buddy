using System.Collections.Generic;
using System.Linq;

namespace Buddy.Cli.Commands;

internal sealed class SlashCommandRegistry {
    private readonly List<SlashCommand> _commands = new();

    public void Register(SlashCommand command) {
        _commands.Add(command);
    }

    public IReadOnlyList<string> GetCompletions(string prefix) {
        if (string.IsNullOrWhiteSpace(prefix)) {
            return Array.Empty<string>();
        }

        var normalized = prefix.Trim();
        if (!normalized.StartsWith("/", StringComparison.Ordinal)) {
            return Array.Empty<string>();
        }

        var matches = new List<string>();
        foreach (var cmd in _commands) {
            if (IsPrefixMatch(cmd.Name, normalized)) {
                matches.Add(cmd.Name);
            }

            if (cmd.Aliases is null) {
                continue;
            }

            foreach (var alias in cmd.Aliases) {
                if (IsPrefixMatch(alias, normalized)) {
                    matches.Add(alias);
                }
            }
        }

        return matches
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(m => m, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsPrefixMatch(string candidate, string prefix) {
        return candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
}
