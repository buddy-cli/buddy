using System.Reflection;
using System.Runtime.InteropServices;
using Buddy.Cli;
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

var systemPrompt = $@"You are buddy, a research-grade coding agent. 
Your job is to assist the user with programming tasks. 
Current date: {currentDate:yyyy-MM-dd}. 
Current working directory: {workingDirectory}. 
OS environment: {osEnvironment}. 
Be concise, correct, and helpful.";

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
