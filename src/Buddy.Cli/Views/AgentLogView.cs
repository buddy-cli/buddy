using System.Collections.Specialized;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using Buddy.Cli.ViewModels;
using ReactiveUI;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Buddy.Cli.Views;

public class AgentLogView : FrameView, IViewFor<AgentLogViewModel> {
    private const int StageRowHeight = 1;

    private readonly CompositeDisposable _disposable = [];
    private readonly SpinnerView _spinner;
    private readonly Label _stageLabel;
    private readonly TextView _logTextView;

    object? IViewFor.ViewModel {
        get => ViewModel;
        set => ViewModel = (AgentLogViewModel?)value;
    }

    public AgentLogViewModel? ViewModel { get; set; }

    public AgentLogView(AgentLogViewModel viewModel) {
        ViewModel = viewModel;
        Title = "Agent Log";
        Visible = false;

        // Spinner positioned at the top left
        _spinner = new SpinnerView {
            X = 0,
            Y = 0,
            Width = 2,
            Height = 1,
            AutoSpin = false,
            Visible = false
        };

        // Stage label next to spinner
        _stageLabel = new Label {
            X = Pos.Right(_spinner) + 1,
            Y = 0,
            Text = "Stage: Idle"
        };

        // Log text view below the stage row
        _logTextView = new TextView {
            X = 0,
            Y = StageRowHeight,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true
        };

        Add(_spinner, _stageLabel, _logTextView);

        // Bind spinner visibility and auto-spin to IsProcessing
        viewModel
            .WhenAnyValue(x => x.IsProcessing)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(isProcessing => {
                _spinner.Visible = isProcessing;
                _spinner.AutoSpin = isProcessing;
            })
            .DisposeWith(_disposable);

        // Bind stage label text
        viewModel
            .WhenAnyValue(x => x.CurrentStage)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(stage => {
                _stageLabel.Text = $"Stage: {stage}";
            })
            .DisposeWith(_disposable);

        // Subscribe to collection changes
        Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                h => viewModel.Entries.CollectionChanged += h,
                h => viewModel.Entries.CollectionChanged -= h)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => UpdateLogText())
            .DisposeWith(_disposable);

        // Also subscribe to property changes on the ViewModel (for streaming text updates)
        viewModel.Changed
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => UpdateLogText())
            .DisposeWith(_disposable);
    }

    private void UpdateLogText() {
        if (ViewModel is null) return;

        var sb = new StringBuilder();
        foreach (var entry in ViewModel.Entries) {
            var prefix = entry.Type switch {
                LogEntryType.User => "👤 You: ",
                LogEntryType.AssistantText => "🤖 ",
                LogEntryType.ToolStatus => "🔧 ",
                _ => ""
            };

            sb.AppendLine($"{prefix}{entry.Content}");
            sb.AppendLine();
        }

        _logTextView.Text = sb.ToString();

        // Scroll to bottom
        _logTextView.MoveEnd();
    }

    protected override void Dispose(bool disposing) {
        _disposable.Dispose();
        base.Dispose(disposing);
    }
}
