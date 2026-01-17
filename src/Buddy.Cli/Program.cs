using System.Reflection;
using System.Runtime.InteropServices;
using Buddy.Cli;
using Buddy.Cli.Extensions;
using Buddy.Core.Application;
using Buddy.Core.Configuration;
using Buddy.Core.Instructions;
using Buddy.Core.Worktree;
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
builder.Services.AddBuddyCli();
using var host = builder.Build();

var worktreeSnapshotProvider = host.Services.GetRequiredService<IWorktreeSnapshotProvider>();
var worktreeSnapshot = worktreeSnapshotProvider.Build(workingDirectory);

var systemPromptBuilder = host.Services.GetRequiredService<ISystemPromptBuilder>();
var systemPrompt = systemPromptBuilder.Build(options, currentDate, osEnvironment)
    + $"""
<ProjectWorktree>
Project worktree:
{worktreeSnapshot}
</ProjectWorktree>
""";

using var shutdownCts = new CancellationTokenSource();
ConsoleCancelEventHandler? cancelHandler = (_, eventArgs) => {
    eventArgs.Cancel = true;
    shutdownCts.Cancel();
};
Console.CancelKeyPress += cancelHandler;

var chatApplication = new ChatApplicationFactory(host.Services).Create(
    version,
    systemPrompt,
    projectInstructions,
    shutdownCts.Token);

var exitCode = await chatApplication.RunAsync();

Console.CancelKeyPress -= cancelHandler;
return exitCode;
