using Buddy.Cli;
using Buddy.Cli.AgentRuntime;
using Buddy.Cli.DependencyInjection;
using Buddy.Core.Application;
using Buddy.Core.Configuration;
using Microsoft.Extensions.Hosting;


var environment = EnvironmentLoader.Load();
var options = BuddyOptionsLoader.Load(environment.WorkingDirectory, args);

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddBuddyCore(options);
builder.Services.AddBuddyCli();
using var host = builder.Build();


using var cancellationTokenSource = new CancellationTokenSource();
ConsoleCancelEventHandler? cancelHandler = (_, eventArgs) => {
    eventArgs.Cancel = true;
    cancellationTokenSource.Cancel();
};
Console.CancelKeyPress += cancelHandler;

var chatApplication = await (new ChatApplicationFactory(host.Services)).Create(
    environment,
    options,
    cancellationTokenSource.Token);

var exitCode = await chatApplication.RunAsync();

Console.CancelKeyPress -= cancelHandler;
return exitCode;
