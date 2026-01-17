using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Buddy.Cli;
using Buddy.Core.Agents;
using Buddy.Core.Application;
using Buddy.Core.Configuration;
using Buddy.Core.Instructions;
using Buddy.LLM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
var workingDirectory = Environment.CurrentDirectory;
var currentDate = DateTimeOffset.Now;
var osEnvironment = $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})";
var options = BuddyOptionsLoader.Load(workingDirectory, args);
var projectInstructions = ProjectInstructionsLoader.Load(workingDirectory);

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddBuddyCore(options);
using var host = builder.Build();

var worktreeSnapshot = BuildWorktreeSnapshot(workingDirectory);

var systemPrompt = $"""
[Role]
You are buddy, a research-grade coding agent.
Your job is to assist the user with programming tasks.
Be concise, correct, and helpful.
<context>
Current date: {currentDate:yyyy-MM-dd}.
OS environment: {osEnvironment}.
</context>
<workingDir>
Current working directory: {workingDirectory}.
Project worktree:
```
{worktreeSnapshot}
```
The worktree might be truncated and show up to 5 files and directories at each level.
</workingDir>
""";

var agent = host.Services.GetRequiredService<BuddyAgent>();
ILLMClient llmClient = host.Services.GetRequiredService<ILLMClient>();

using var shutdownCts = new CancellationTokenSource();
ConsoleCancelEventHandler? cancelHandler = (_, eventArgs) => {
    eventArgs.Cancel = true;
    shutdownCts.Cancel();
};
Console.CancelKeyPress += cancelHandler;

var exitCode = await TerminalGuiChat.RunAsync(
    agent,
    llmClient,
    model => new OpenAiLlmClient(options.ApiKey, model, options.BaseUrl),
    options,
    version,
    systemPrompt,
    projectInstructions,
    shutdownCts.Token);

Console.CancelKeyPress -= cancelHandler;
return exitCode;

static string BuildWorktreeSnapshot(string root) {
    if (string.IsNullOrWhiteSpace(root)) {
        root = Directory.GetCurrentDirectory();
    }

    var sb = new StringBuilder();
    var rootInfo = new DirectoryInfo(root);
    if (!rootInfo.Exists) {
        return "(working directory missing)";
    }

    sb.AppendLine(".");
    AppendChildren(sb, rootInfo, indentLevel: 1);
    return sb.ToString().TrimEnd();
}

static void AppendChildren(StringBuilder sb, DirectoryInfo directory, int indentLevel) {
    var entries = directory.EnumerateFileSystemInfos()
        .Where(entry => !string.Equals(entry.Name, ".git", StringComparison.OrdinalIgnoreCase))
        .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();

    var displayed = 0;
    foreach (var entry in entries) {
        if (displayed >= 5) {
            sb.AppendLine($"{Indent(indentLevel)}...");
            break;
        }

        if (entry is DirectoryInfo dir) {
            sb.AppendLine($"{Indent(indentLevel)}{dir.Name}/");
        }
        else {
            sb.AppendLine($"{Indent(indentLevel)}{entry.Name}");
        }

        displayed++;
    }
}

static string Indent(int level) => new string(' ', level * 2);
