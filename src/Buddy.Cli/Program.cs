using System.Reflection;
using System.Runtime.InteropServices;
using Buddy.Core.Agents;
using Buddy.Core.Application;
using Buddy.Core.Configuration;
using Buddy.Core.Instructions;
using Buddy.LLM;
using DotNetEnv;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;

// Phase 0 scaffold: load .env if present (lowest priority) so users can set API key/model early.
try {
    // Keep .env as lowest priority: don't overwrite real environment variables.
    Env.NoClobber().TraversePath().Load();
}
catch (Exception ex) {
    AnsiConsole.MarkupLine($"[red]warning:[/] failed to load .env ({ex.Message})");
}

var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
var workingDirectory = Environment.CurrentDirectory;
var currentDate = DateTimeOffset.Now;
var osEnvironment = $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})";

var options = BuddyOptionsLoader.Load(workingDirectory, args);
var projectInstructions = ProjectInstructionsLoader.Load(workingDirectory);

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddBuddyCore(options);
using var host = builder.Build();

AnsiConsole.Write(new FigletText("buddy").Color(Color.Grey));
AnsiConsole.MarkupLine("[bold]buddy coding agent[/] [grey](phase 2: streaming hello)[/]");
AnsiConsole.MarkupLine($"version {version} • working dir [underline]{workingDirectory}[/]");
AnsiConsole.MarkupLine($"model [bold]{options.Model}[/] • base url [grey]{options.BaseUrl ?? "(default)"}[/]");
AnsiConsole.MarkupLine("Type a message to chat. Use /help for commands. Press Ctrl+Enter or Shift+Enter for a newline.");

var systemPrompt = $@"You are buddy, a research-grade coding agent. 
Your job is to assist the user with programming tasks. 
Current date: {currentDate:yyyy-MM-dd}. 
Current working directory: {workingDirectory}. 
OS environment: {osEnvironment}. 
Be concise, correct, and helpful.";

var agent = host.Services.GetRequiredService<BuddyAgent>();
ILLMClient llmClient = host.Services.GetRequiredService<ILLMClient>();
CancellationTokenSource? turnCts = null;
var exitRequested = false;

Console.CancelKeyPress += (_, e) => {
    // First Ctrl+C cancels the current model request; second Ctrl+C exits.
    if (turnCts is not null && !turnCts.IsCancellationRequested) {
        e.Cancel = true;
        turnCts.Cancel();
        return;
    }

    exitRequested = true;
    e.Cancel = true;
};

while (!exitRequested) {
    var input = ReadUserInput();

    if (string.IsNullOrWhiteSpace(input)) {
        continue;
    }

    if (input.StartsWith("/", StringComparison.Ordinal)) {
        var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var cmd = parts[0];
        var arg = parts.Length > 1 ? parts[1] : null;

        switch (cmd) {
            case "/help":
                AnsiConsole.MarkupLine("Commands:");
                AnsiConsole.MarkupLine("  [bold]/help[/]            Show this help");
                AnsiConsole.MarkupLine("  [bold]/clear[/]           Clear conversation history");
                AnsiConsole.MarkupLine("  [bold]/model <name>[/]    Switch model for next turns");
                AnsiConsole.MarkupLine("  [bold]/exit[/] or [bold]/quit[/]  Exit");
                continue;

            case "/clear":
                agent.ClearHistory();
                AnsiConsole.MarkupLine("[grey]history cleared[/]");
                continue;

            case "/model":
                if (string.IsNullOrWhiteSpace(arg)) {
                    AnsiConsole.MarkupLine($"current model [bold]{options.Model}[/]");
                    continue;
                }

                options.Model = arg.Trim();
                if (llmClient is IDisposable d) d.Dispose();
                llmClient = new OpenAiLlmClient(options.ApiKey, options.Model, options.BaseUrl);
                AnsiConsole.MarkupLine($"[grey]model set to[/] [bold]{options.Model}[/]");
                continue;

            case "/exit":
            case "/quit":
                exitRequested = true;
                continue;

            default:
                AnsiConsole.MarkupLine("[yellow]unknown command[/] — try /help");
                continue;
        }
    }

    turnCts = new CancellationTokenSource();

    AnsiConsole.Markup("[bold]buddy:[/] ");
    try {
        await agent.RunTurnAsync(
            llmClient,
            systemPrompt,
            projectInstructions,
            input,
            onTextDelta: text => {
                Console.Write(text);
                return Task.CompletedTask;
            },
            onToolStatus: status => {
                Console.WriteLine();
                AnsiConsole.MarkupLine($"[grey]{Markup.Escape(status)}[/]");
                AnsiConsole.Markup("[bold]buddy:[/] ");
                return Task.CompletedTask;
            },
            cancellationToken: turnCts.Token);
    }
    catch (OperationCanceledException) {
        AnsiConsole.MarkupLine("\n[grey](canceled)[/]");
    }
    catch (Exception ex) {
        AnsiConsole.MarkupLine($"\n[red]error:[/] {ex.Message}");
    }
    finally {
        turnCts.Dispose();
        turnCts = null;
    }

    Console.WriteLine();
}

return 0;

string ReadUserInput() {
    const string promptPlain = "> ";
    const string promptMarkup = "[green]>[/] ";
    var editor = new Buddy.Cli.ConsoleTextEditor(promptMarkup, promptPlain);
    return editor.ReadInput();
}
