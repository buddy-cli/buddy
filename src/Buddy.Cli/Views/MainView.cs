using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Buddy.Cli.ViewModels;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Buddy.Cli.Views;

public class MainView : Window, IViewFor<MainViewModel> {
    private const string BannerText = """
        
        ██████╗ ██╗   ██╗██████╗ ██████╗ ██╗   ██╗
        ██╔══██╗██║   ██║██╔══██╗██╔══██╗╚██╗ ██╔╝
        ██████╔╝██║   ██║██║  ██║██║  ██║ ╚████╔╝ 
        ██╔══██╗██║   ██║██║  ██║██║  ██║  ╚██╔╝  
        ██████╔╝╚██████╔╝██████╔╝██████╔╝   ██║   
        ╚═════╝  ╚═════╝ ╚═════╝ ╚═════╝    ╚═╝   
        """;

    private const int BannerHeight = 8;
    private const int InputHeight = 5;
    private const int ModelInfoHeight = 1;
    private const int FooterHeight = 1;
    private const int SlashCommandsHeight = 8;

    private readonly IApplication _app;
    private readonly CompositeDisposable _disposable = [];
    private readonly Label _banner;
    private readonly TextView _input;
    private readonly Label _modelInfo;
    private readonly SlashCommandsView _slashCommandsView;
    private readonly AgentLogView _agentLogView;
    private readonly View _footer;
    private readonly Label _footerWorkingDir;
    private readonly Label _footerVersion;

    object? IViewFor.ViewModel {
        get => ViewModel;
        set => ViewModel = (MainViewModel?)value;
    }

    public MainViewModel? ViewModel { get; set; }

    public MainView(MainViewModel viewModel, IApplication app) {
        ViewModel = viewModel;
        _app = app;
        Title = $"Buddy - {Application.QuitKey} to Exit";

        _banner = new Label { Text = BannerText, X = 0, Y = 0 };
        _input = new TextView {
            X = 0,
            Y = BannerHeight,
            Width = Dim.Fill(),
            Height = InputHeight,
            WordWrap = true,
            EnterKeyAddsLine = false // Enter is reserved for "send message"
        };

        // Handle Ctrl+J (shows as Ctrl+Enter) for new line
        _input.KeyDown += (_, key) => {
            if (key == Key.Enter.WithCtrl) {
                _input.InsertText("\n");
                key.Handled = true;
            }
        };

        // Remove mouse binding for context menu (Ctrl+Click)
        _input.MouseBindings.Remove(MouseFlags.LeftButtonPressed | MouseFlags.Ctrl);

        // Suppress right-click context menu by handling the mouse event
        _input.MouseEvent += (_, e) => {
            if (e.Flags.HasFlag(MouseFlags.RightButtonPressed) || e.Flags.HasFlag(MouseFlags.RightButtonClicked)) {
                e.Handled = true;
            }
        };

        _modelInfo = new Label {
            X = 0,
            Y = Pos.Bottom(_input),
            Width = Dim.Fill(),
            Height = ModelInfoHeight
        };

        _slashCommandsView = new SlashCommandsView(ViewModel.SlashCommands) {
            X = 0,
            Y = Pos.Bottom(_modelInfo),
            Width = Dim.Fill(),
            Height = 8
        };

        _agentLogView = new AgentLogView(ViewModel.AgentLog) {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill() - InputHeight - ModelInfoHeight - FooterHeight,
            Visible = false
        };

        _footer = new View {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Height = 1
        };

        _footerWorkingDir = new Label { X = 0, Y = 0 };
        _footerVersion = new Label { X = Pos.AnchorEnd(10), Y = 0 };
        _footer.Add(_footerWorkingDir, _footerVersion);

        // AgentLogView added first so it's behind other views (z-order)
        Add(_agentLogView, _banner, _input, _modelInfo, _slashCommandsView, _footer);

        // Bind input text (two-way)
        // View -> ViewModel
        _input
            .Events()
            .ContentsChanged
            .Select(_ => _input.Text)
            .DistinctUntilChanged()
            .BindTo(ViewModel, x => x.InputText)
            .DisposeWith(_disposable);

        // ViewModel -> View
        ViewModel
            .WhenAnyValue(x => x.InputText)
            .DistinctUntilChanged()
            .Where(_ => _input.Text != ViewModel.InputText)
            .Subscribe(text => {
                _input.Text = text;
            })
            .DisposeWith(_disposable);

        // Handle Tab for autocomplete
        _input
            .Events()
            .KeyDown
            .Where(e => e == Key.Tab && ViewModel.SlashCommands.IsActive)
            .Subscribe(e => {
                var completed = ViewModel.SlashCommands.GetCompletedText();
                if (completed is not null) {
                    _input.Text = completed;
                    _input.MoveEnd();
                    e.Handled = true;
                }
            })
            .DisposeWith(_disposable);

        // Handle Enter key for submit or slash command execution
        _input
            .Events()
            .KeyDown
            .Where(e => e == Key.Enter)
            .Subscribe(e => {
                // First try to execute as a slash command
                if (ViewModel.TryExecuteSlashCommand()) {
                    e.Handled = true;
                    return;
                }
                
                // Otherwise, treat as normal message submit
                if (ViewModel.SubmitCommand.CanExecute(null)) {
                    ViewModel.SubmitCommand.Execute(null);
                    e.Handled = true;
                }
            })
            .DisposeWith(_disposable);

        // Bind slash commands visibility
        ViewModel.SlashCommands
            .WhenAnyValue(x => x.IsActive)
            .BindTo(_slashCommandsView, x => x.Visible)
            .DisposeWith(_disposable);

        // Footer bindings
        ViewModel
            .WhenAnyValue(x => x.WorkingDirectory)
            .BindTo(_footerWorkingDir, x => x.Text)
            .DisposeWith(_disposable);

        ViewModel
            .WhenAnyValue(x => x.Version)
            .Do(ver => _footerVersion.X = Pos.AnchorEnd(ver.Length))
            .BindTo(_footerVersion, x => x.Text)
            .DisposeWith(_disposable);

        // Model info binding
        ViewModel
            .WhenAnyValue(x => x.ModelInfo)
            .BindTo(_modelInfo, x => x.Text)
            .DisposeWith(_disposable);

        // Bind agent work mode - update layout when switching modes
        ViewModel
            .WhenAnyValue(x => x.IsAgentWorkMode)
            .Subscribe(isAgentMode => {
                if (isAgentMode) {
                    // Agent work mode: hide banner, move input to bottom, show agent log
                    _banner.Visible = false;
                    _input.Y = Pos.AnchorEnd(InputHeight + ModelInfoHeight + FooterHeight);
                    _modelInfo.Y = Pos.Bottom(_input);
                    // Slash commands appear above input in agent mode
                    _slashCommandsView.Y = Pos.AnchorEnd(InputHeight + ModelInfoHeight + FooterHeight + SlashCommandsHeight);
                    _agentLogView.Visible = true;
                } else {
                    // Start mode: show banner, input below banner, hide agent log
                    _banner.Visible = true;
                    _input.Y = BannerHeight;
                    _modelInfo.Y = Pos.Bottom(_input);
                    // Slash commands appear below model info in start mode
                    _slashCommandsView.Y = Pos.Bottom(_modelInfo);
                    _agentLogView.Visible = false;
                }
            })
            .DisposeWith(_disposable);

        // Register interaction handlers
        ViewModel.RequestExit.RegisterHandler(context => {
            _app.RequestStop();
            context.SetOutput(Unit.Default);
        }).DisposeWith(_disposable);

        ViewModel.ShowModelDialog.RegisterHandler(context => {
            using var dialog = new ModelSelectionDialogView(context.Input);
            _app.Run(dialog);
            context.SetOutput(dialog.Result);
        }).DisposeWith(_disposable);

        ViewModel.ShowProviderDialog.RegisterHandler(context => {
            using var dialog = new ProviderConfigDialogView(context.Input, _app);
            _app.Run(dialog);
            context.SetOutput(dialog.Result);
        }).DisposeWith(_disposable);

        _input.SetFocus();
    }

    protected override void Dispose(bool disposing) {
        _disposable.Dispose();
        base.Dispose(disposing);
    }
}