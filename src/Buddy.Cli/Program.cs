using System.Reactive.Concurrency;
using Buddy.Cli.AgentRuntime;
using Buddy.Cli.DependencyInjection;
using Buddy.Cli.Ui;
using Buddy.Cli.Views;
using Buddy.Core.Application;
using Buddy.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Terminal.Gui.App;

var services = new ServiceCollection();
services.AddBuddyCli();

var tempProvider = services.BuildServiceProvider();
var environmentLoader = tempProvider.GetRequiredService<EnvironmentLoader>();
var options = BuddyOptionsLoader.Load(environmentLoader.Environment.WorkingDirectory, args);

services.AddBuddyCore(options);

using IApplication app = Application.Create().Init();
services.AddSingleton(app);

var serviceProvider = services.BuildServiceProvider();

RxApp.MainThreadScheduler = new TerminalScheduler(app);
RxApp.TaskpoolScheduler = TaskPoolScheduler.Default;

using var mainView = serviceProvider.GetRequiredService<MainView>();
app.Run(mainView);
