using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Buddy.Cli.ViewModels;
using ReactiveUI;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Buddy.Cli.Views;

public class SlashCommandsView : FrameView, IViewFor<SlashCommandsViewModel> {
    private readonly CompositeDisposable _disposable = [];
    private readonly ListView _listView;
    private readonly ObservableCollection<string> _displayItems = [];

    object? IViewFor.ViewModel {
        get => ViewModel;
        set => ViewModel = (SlashCommandsViewModel?)value;
    }

    public SlashCommandsViewModel? ViewModel { get; set; }

    public SlashCommandsView(SlashCommandsViewModel viewModel) {
        ViewModel = viewModel;
        Title = "Commands";
        Visible = false;

        _listView = new ListView {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        _listView.SetSource(_displayItems);

        Add(_listView);

        // Update list when filtered commands change
        Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                h => ((INotifyCollectionChanged)ViewModel.FilteredCommands).CollectionChanged += h,
                h => ((INotifyCollectionChanged)ViewModel.FilteredCommands).CollectionChanged -= h)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => UpdateListSource())
            .DisposeWith(_disposable);

        // Bind selected index
        ViewModel
            .WhenAnyValue(x => x.SelectedIndex)
            .Subscribe(idx => {
                if (idx >= 0 && idx < _listView.Source?.Count) {
                    _listView.SelectedItem = idx;
                }
            })
            .DisposeWith(_disposable);

        _listView.SelectedItemChanged += (_, args) => {
            if (args.Item.HasValue) {
                ViewModel.SelectedIndex = args.Item.Value;
            }
        };

        // Initial population
        UpdateListSource();
    }

    private void UpdateListSource() {
        _displayItems.Clear();
        foreach (var cmd in ViewModel!.FilteredCommands) {
            _displayItems.Add($"/{cmd.Command,-12} {cmd.Description}");
        }
    }

    protected override void Dispose(bool disposing) {
        _disposable.Dispose();
        base.Dispose(disposing);
    }
}
