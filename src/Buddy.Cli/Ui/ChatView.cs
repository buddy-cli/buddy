using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Buddy.Core.Configuration;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using Terminal.Gui;

namespace Buddy.Cli.Ui;

/// <summary>
/// The main chat view implementing reactive MVVM bindings with Terminal.Gui.
/// </summary>
internal sealed class ChatView 
//: //Window, IViewFor<ChatViewModel>
{
    // private readonly CompositeDisposable _disposables = new();
    // private readonly ChatLayoutMetrics _metrics;
    // private readonly string _version;
    // private readonly ObservableCollection<string> _suggestionItems = new();
    // private ChatViewModel? _viewModel;
    //
    // // UI Elements
    // private readonly View _startView;
    // private readonly View _sessionView;
    // private readonly SpinnerView _stageSpinner;
    // private readonly Label _stageLabel;
    // private readonly TextView _history;
    // private readonly View _inputPanel;
    // private readonly View _infoLayer;
    // private readonly View _footer;
    // private readonly Label _footerProvider;
    // private readonly Label _footerLeft;
    // private readonly Label _footerRight;
    // private readonly ListView _suggestionOverlay;
    // private readonly TextView _input;
    // private readonly Button _sendButton;
    // private readonly ColorScheme _idleLogScheme;
    // private readonly ColorScheme _activeLogScheme;
    //
    // public ChatView(ChatLayoutMetrics metrics, string version) {
    //     _metrics = metrics;
    //     _version = version;
    //
    //     Title = "buddy - coding agent (Esc to quit)";
    //     X = 0;
    //     Y = 0;
    //     Width = Dim.Fill();
    //     Height = Dim.Fill();
    //
    //     // Create start view with banner
    //     _startView = new View {
    //         X = 0,
    //         Y = 0,
    //         Width = Dim.Fill(),
    //         Height = Dim.Fill()
    //     };
    //
    //     var headerTitle = new Label {
    //         Text = TerminalGuiLayout.BannerText,
    //         X = 1,
    //         Y = 0
    //     };
    //     _startView.Add(headerTitle);
    //
    //     // Create session view (hidden initially)
    //     _sessionView = new View {
    //         X = 0,
    //         Y = 0,
    //         Width = Dim.Fill(),
    //         Height = Dim.Fill(),
    //         Visible = false
    //     };
    //
    //     var sessionHeader = new View {
    //         X = 0,
    //         Y = 0,
    //         Width = Dim.Fill(),
    //         Height = metrics.SessionHeaderHeight
    //     };
    //     sessionHeader.Add(new Label { Text = "buddy", X = 1, Y = 0 });
    //
    //     _stageSpinner = new SpinnerView {
    //         X = 1,
    //         Y = Pos.Bottom(sessionHeader),
    //         Width = 1,
    //         Height = 1,
    //         AutoSpin = false,
    //         Visible = false
    //     };
    //
    //     _stageLabel = new Label {
    //         Text = "Stage: Idle",
    //         X = Pos.Right(_stageSpinner) + 1,
    //         Y = Pos.Top(_stageSpinner)
    //     };
    //
    //     const int defaultInputHeight = 3;
    //     _history = new TextView {
    //         X = 0,
    //         Y = Pos.Bottom(_stageLabel),
    //         Width = Dim.Fill(),
    //         Height = Dim.Fill(metrics.SessionHeaderHeight + metrics.StageHeight + metrics.InfoLayerHeight + defaultInputHeight + metrics.FooterHeight + 3),
    //         WordWrap = true,
    //         CanFocus = false,
    //         TabStop = TabBehavior.NoStop
    //     };
    //
    //     _inputPanel = new View {
    //         X = 0,
    //         Y = Pos.AnchorEnd(defaultInputHeight + metrics.FooterHeight + 1),
    //         Width = Dim.Fill(),
    //         Height = defaultInputHeight + 1,
    //         CanFocus = true,
    //         TabStop = TabBehavior.TabStop
    //     };
    //
    //     _infoLayer = new View {
    //         X = 0,
    //         Y = Pos.AnchorEnd(defaultInputHeight + metrics.FooterHeight + metrics.InfoLayerHeight + 1),
    //         Width = Dim.Fill(),
    //         Height = metrics.InfoLayerHeight,
    //         CanFocus = false,
    //         TabStop = TabBehavior.NoStop
    //     };
    //     _infoLayer.Add(new Label { Text = "Type a message or /command (e.g., /help) ", X = 1, Y = 0 });
    //
    //     _footer = new View {
    //         X = 0,
    //         Y = Pos.AnchorEnd(metrics.FooterHeight),
    //         Width = Dim.Fill(),
    //         Height = metrics.FooterHeight,
    //         CanFocus = false,
    //         TabStop = TabBehavior.NoStop
    //     };
    //
    //     _footerProvider = new Label { Text = "", X = 1, Y = 0 };
    //     _footerLeft = new Label { Text = "", X = 1, Y = 1 };
    //     _footerRight = new Label { Text = version, X = Pos.AnchorEnd(version.Length + 1), Y = 1 };
    //     _footer.Add(_footerProvider, _footerLeft, _footerRight);
    //
    //     _suggestionOverlay = new ListView {
    //         X = 1,
    //         Y = 0,
    //         Width = Dim.Fill(12),
    //         Height = 0,
    //         CanFocus = false,
    //         TabStop = TabBehavior.NoStop,
    //         AllowsMarking = false,
    //         Visible = false
    //     };
    //     _suggestionOverlay.SetSource(_suggestionItems);
    //
    //     _input = new TextView {
    //         X = 1,
    //         Y = 1,
    //         Width = Dim.Fill(12),
    //         Height = defaultInputHeight,
    //         WordWrap = true,
    //         CanFocus = true,
    //         TabStop = TabBehavior.TabStop
    //     };
    //
    //     _sendButton = new Button {
    //         Text = "Send",
    //         X = Pos.AnchorEnd(10),
    //         Y = 2,
    //         CanFocus = true,
    //         TabStop = TabBehavior.TabStop,
    //         IsDefault = true
    //     };
    //
    //     _inputPanel.Add(_input, _sendButton);
    //     _sessionView.Add(sessionHeader, _stageSpinner, _stageLabel, _history);
    //     Add(_startView, _sessionView, _infoLayer, _inputPanel, _footer, _suggestionOverlay);
    //
    //     // Create color schemes (will be updated after driver init)
    //     _idleLogScheme = new ColorScheme();
    //     _activeLogScheme = new ColorScheme();
    // }
    //
    // #region IViewFor Implementation
    //
    // object? IViewFor.ViewModel {
    //     get => ViewModel;
    //     set => ViewModel = value as ChatViewModel;
    // }
    //
    // public ChatViewModel? ViewModel {
    //     get => _viewModel;
    //     set {
    //         if (_viewModel == value) return;
    //         _viewModel = value;
    //         SetupBindings();
    //     }
    // }
    //
    // #endregion
    //
    // #region Bindings
    //
    // private void SetupBindings() {
    //     if (ViewModel is null) return;
    //
    //     // Dispose previous bindings
    //     _disposables.Clear();
    //
    //     var vm = ViewModel;
    //
    //     // Stage label binding
    //     vm.WhenAnyValue(x => x.CurrentStage)
    //         .ObserveOn(RxApp.MainThreadScheduler)
    //         .Subscribe(stage => {
    //             _stageLabel.Text = $"Stage: {stage}";
    //             _stageLabel.SetNeedsDraw();
    //         })
    //         .DisposeWith(_disposables);
    //
    //     // Spinner visibility binding
    //     vm.WhenAnyValue(x => x.ShowSpinner)
    //         .ObserveOn(RxApp.MainThreadScheduler)
    //         .Subscribe(show => {
    //             _stageSpinner.Visible = show;
    //             _stageSpinner.AutoSpin = show;
    //         })
    //         .DisposeWith(_disposables);
    //
    //     // Session started binding (switch between start and session views)
    //     vm.WhenAnyValue(x => x.SessionStarted)
    //         .ObserveOn(RxApp.MainThreadScheduler)
    //         .Subscribe(started => {
    //             _startView.Visible = !started;
    //             _sessionView.Visible = started;
    //             ApplyLayout();
    //         })
    //         .DisposeWith(_disposables);
    //
    //     // Input enabled binding (inverse of TurnInFlight)
    //     vm.WhenAnyValue(x => x.TurnInFlight)
    //         .ObserveOn(RxApp.MainThreadScheduler)
    //         .Subscribe(inFlight => {
    //             _input.Enabled = !inFlight;
    //             _sendButton.Enabled = !inFlight;
    //             if (!inFlight) {
    //                 _input.SetFocus();
    //             }
    //         })
    //         .DisposeWith(_disposables);
    //
    //     // Input height binding
    //     vm.WhenAnyValue(x => x.InputHeight)
    //         .ObserveOn(RxApp.MainThreadScheduler)
    //         .Subscribe(_ => ApplyLayout())
    //         .DisposeWith(_disposables);
    //
    //     // History appended binding
    //     vm.HistoryAppended
    //         .ObserveOn(RxApp.MainThreadScheduler)
    //         .Subscribe(text => {
    //             _history.MoveEnd();
    //             _history.InsertText(text);
    //             UpdateLogStyle();
    //             _history.MoveEnd();
    //             _history.SetNeedsDraw();
    //             Application.LayoutAndDraw(forceDraw: false);
    //         })
    //         .DisposeWith(_disposables);
    //
    //     // History clear binding
    //     vm.HistoryClearRequested
    //         .ObserveOn(RxApp.MainThreadScheduler)
    //         .Subscribe(_ => {
    //             _history.Text = string.Empty;
    //             UpdateLogStyle();
    //         })
    //         .DisposeWith(_disposables);
    //
    //     // Two-way input text binding (View → ViewModel)
    //     _input.Events()
    //         .TextChanged
    //         .Select(_ => _input.Text ?? string.Empty)
    //         .DistinctUntilChanged()
    //         .Subscribe(text => {
    //             if (vm.InputText != text) {
    //                 vm.InputText = text;
    //             }
    //         })
    //         .DisposeWith(_disposables);
    //
    //     // ViewModel → View for input
    //     vm.WhenAnyValue(x => x.InputText)
    //         .ObserveOn(RxApp.MainThreadScheduler)
    //         .Where(text => _input.Text != text)
    //         .Subscribe(text => _input.Text = text)
    //         .DisposeWith(_disposables);
    //
    //     // Send button click → SendCommand
    //     _sendButton.Events()
    //         .Accepting
    //         .Select(_ => Unit.Default)
    //         .InvokeCommand(vm, x => x.SendCommand)
    //         .DisposeWith(_disposables);
    //
    //     // Ctrl+Enter → SendCommand
    //     _input.Events()
    //         .KeyDown
    //         .Where(e => e.KeyCode == KeyCode.Enter && e.IsCtrl)
    //         .Do(e => e.Handled = true)
    //         .Select(_ => Unit.Default)
    //         .InvokeCommand(vm, x => x.SendCommand)
    //         .DisposeWith(_disposables);
    //
    //     // Alt+Up/Down → Adjust input height
    //     _input.Events()
    //         .KeyDown
    //         .Where(e => e.IsAlt && e.KeyCode == KeyCode.CursorUp)
    //         .Do(e => e.Handled = true)
    //         .Subscribe(_ => vm.InputHeight = Math.Min(10, vm.InputHeight + 1))
    //         .DisposeWith(_disposables);
    //
    //     _input.Events()
    //         .KeyDown
    //         .Where(e => e.IsAlt && e.KeyCode == KeyCode.CursorDown)
    //         .Do(e => e.Handled = true)
    //         .Subscribe(_ => vm.InputHeight = Math.Max(2, vm.InputHeight - 1))
    //         .DisposeWith(_disposables);
    //
    //     // Tab key handling
    //     _input.Events()
    //         .KeyDown
    //         .Where(e => e.KeyCode == KeyCode.Tab && !_input.Autocomplete.Visible)
    //         .Do(e => e.Handled = true)
    //         .Subscribe(_ => _input.Text += "    ")
    //         .DisposeWith(_disposables);
    //
    //     // Model change triggers footer refresh
    //     vm.WhenAnyValue(x => x.CurrentMClient)
    //         .ObserveOn(RxApp.MainThreadScheduler)
    //         .Subscribe(_ => RefreshFooter())
    //         .DisposeWith(_disposables);
    //
    //     // Initialize footer
    //     RefreshFooter();
    //     ApplyLayout();
    //     UpdateLogStyle();
    //     _input.SetFocus();
    // }
    //
    // #endregion
    //
    // #region Layout
    //
    // private void ApplyLayout() {
    //     if (ViewModel is null) return;
    //
    //     var inputHeight = ViewModel.InputHeight;
    //     var suggestionRows = ViewModel.SuggestionOverlayRows;
    //
    //     _inputPanel.Y = Pos.AnchorEnd(inputHeight + _metrics.FooterHeight + 1);
    //     _inputPanel.Height = inputHeight + 1;
    //     _input.Height = inputHeight;
    //     _sendButton.Y = Pos.Bottom(_input) - 1;
    //
    //     _history.Height = Dim.Fill(_metrics.SessionHeaderHeight + _metrics.StageHeight + _metrics.InfoLayerHeight + inputHeight + _metrics.FooterHeight + 3);
    //
    //     _infoLayer.Y = Pos.AnchorEnd(inputHeight + _metrics.FooterHeight + _metrics.InfoLayerHeight + 1);
    //
    //     _suggestionOverlay.Y = Pos.AnchorEnd(inputHeight + _metrics.FooterHeight + _metrics.InfoLayerHeight + suggestionRows + 1);
    //     _suggestionOverlay.Height = suggestionRows;
    //
    //     Application.LayoutAndDraw(forceDraw: false);
    // }
    //
    // private void UpdateLogStyle() {
    //     var driver = Application.Driver;
    //     if (driver is null) return;
    //
    //     var scheme = ViewModel?.HasHistory == true
    //         ? new ColorScheme {
    //             Normal = driver.MakeColor(Color.Black, Color.White),
    //             Focus = driver.MakeColor(Color.Black, Color.White)
    //         }
    //         : new ColorScheme {
    //             Normal = driver.MakeColor(Color.White, Color.Blue),
    //             Focus = driver.MakeColor(Color.White, Color.Blue)
    //         };
    //
    //     _history.ColorScheme = scheme;
    // }
    //
    // private void RefreshFooter() {
    //     if (ViewModel is null) return;
    //
    //     var options = ViewModel.Options;
    //     _footerProvider.Text = $"provider {TerminalGuiLayout.GetProviderLabel(options)}  •  model {TerminalGuiLayout.GetModelLabel(options)}";
    //     _footerLeft.Text = options.WorkingDirectory;
    //     _footerRight.Text = _version;
    //     _footerRight.X = Pos.AnchorEnd(_version.Length + 1);
    //     _footer.SetNeedsDraw();
    // }
    //
    // #endregion
    //
    // #region Public Accessors for SlashCommandUi integration
    //
    // public TextView InputView => _input;
    // public ListView SuggestionOverlayView => _suggestionOverlay;
    // public ObservableCollection<string> SuggestionItems => _suggestionItems;
    //
    // #endregion
    //
    // protected override void Dispose(bool disposing) {
    //     if (disposing) {
    //         _disposables.Dispose();
    //     }
    //     base.Dispose(disposing);
    // }
}
