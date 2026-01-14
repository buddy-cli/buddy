using System.Reflection;
using Buddy.Core;
using DotNetEnv;
using Spectre.Console;

// Phase 0 scaffold: load .env if present (lowest priority) so users can set API key/model early.
try
{
    Env.TraversePath().Load();
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]warning:[/] failed to load .env ({ex.Message})");
}

var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
var workingDirectory = Environment.CurrentDirectory;

AnsiConsole.Write(new FigletText("buddy").Color(Color.Grey));
AnsiConsole.MarkupLine("[bold]buddy coding agent[/] [grey](phase 0 scaffold)[/]");
AnsiConsole.MarkupLine($"version {version} • working dir [underline]{workingDirectory}[/]");
AnsiConsole.MarkupLine("[yellow]Streaming, tools, and LLM integration will arrive in Phase 2+.[/]");

return 0;
