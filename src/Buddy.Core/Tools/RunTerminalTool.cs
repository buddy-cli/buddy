using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Buddy.Core.Configuration;

namespace Buddy.Core.Tools;

public sealed class RunTerminalTool : ITool {
    private static readonly JsonElement Schema = JsonDocument.Parse(
        """
        {
          "type": "object",
          "properties": {
            "command": { "type": "string", "description": "Shell command to run." }
          },
          "required": ["command"],
          "additionalProperties": false
        }
        """).RootElement.Clone();

    private readonly string _workingDirectory;

    public RunTerminalTool(BuddyOptions options) {
        _workingDirectory = options.WorkingDirectory;
    }

    public string Name => "run_terminal";
    public string Description => "Run a shell command in the working directory and return stdout/stderr + exit code.";
    public JsonElement ParameterSchema => Schema;

    public async Task<string> ExecuteAsync(JsonElement args, CancellationToken cancellationToken = default) {
        if (!args.TryGetProperty("command", out var cmdEl) || cmdEl.ValueKind != JsonValueKind.String) {
            return "error: missing required string argument 'command'";
        }

        var command = cmdEl.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(command)) {
            return "error: command is empty";
        }

        // Use zsh for macOS friendliness; this is still fine on Linux where /bin/zsh exists.
        // If zsh doesn't exist, we can extend this later.
        var psi = new ProcessStartInfo {
            FileName = "/bin/zsh",
            Arguments = $"-lc \"{command.Replace("\\\"", "\\\\\\\"")}\"",
            WorkingDirectory = _workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        var exitCode = process.ExitCode;
        return $"exit_code: {exitCode}\nstdout:\n{stdout.ToString().TrimEnd()}\nstderr:\n{stderr.ToString().TrimEnd()}";
    }
}
