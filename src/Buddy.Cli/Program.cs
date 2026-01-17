using System.Reflection;
using System.Runtime.InteropServices;
using Buddy.Cli;
using Buddy.Core.Agents;
using Buddy.Core.Application;
using Buddy.Core.Configuration;
using Buddy.Core.Instructions;
using Buddy.Core.Worktree;
using WorktreeSnapshot = Buddy.Cli.WorktreeSnapshot;
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

var worktreeSnapshot = Buddy.Cli.WorktreeSnapshot.Build(workingDirectory);

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
{worktreeSnapshot}
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
