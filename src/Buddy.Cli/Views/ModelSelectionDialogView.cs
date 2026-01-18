using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Buddy.Cli.ViewModels;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using Terminal.Gui;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Buddy.Cli.Views;

/// <summary>
/// A dialog for selecting an AI model from available providers.
/// Implements IRunnable via Dialog&lt;TResult&gt; for proper Terminal.Gui v2 session management.
/// </summary>
public class ModelSelectionDialogView : Dialog<ModelSelectionResult?>, IViewFor<ModelSelectionDialogViewModel> {
    private readonly CompositeDisposable _disposable = [];
    private readonly ListView _listView;

    object? IViewFor.ViewModel {
        get => ViewModel;
        set => ViewModel = (ModelSelectionDialogViewModel?)value;
    }

    public ModelSelectionDialogViewModel? ViewModel { get; set; }

    public ModelSelectionDialogView(ModelSelectionDialogViewModel viewModel) {
        ViewModel = viewModel;

        Title = "Select Model";
        Width = Dim.Percent(60);
        Height = Dim.Percent(70);

        var label = new Label { Text = "Models", X = 1, Y = 0 };

        _listView = new ListView {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Height = Dim.Fill(3),
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            AllowsMarking = false
        };
        _listView.SetSource(new ObservableCollection<string>(ViewModel.Items));

        var selectButton = new Button {
            Text = "Use Model",
            IsDefault = true
        };
        selectButton.Accepting += OnSelectClicked;

        var cancelButton = new Button {
            Text = "Cancel"
        };
        cancelButton.Accepting += OnCancelClicked;

        Add(label, _listView);
        AddButton(selectButton);
        AddButton(cancelButton);

        // Bind selected index
        _listView
            .Events()
            .SelectedItemChanged
            .Where(_ => ViewModel is not null)
            .Subscribe(_ => {
                if (_listView.SelectedItem is int index) {
                    ViewModel!.SelectedIndex = index;
                }
            })
            .DisposeWith(_disposable);

        _listView.SelectedItem = 0;
    }

    private void OnSelectClicked(object? sender, CommandEventArgs e) {
        e.Handled = true;
        Result = ViewModel?.GetSelectedResult();
        App?.RequestStop(this);
    }

    private void OnCancelClicked(object? sender, CommandEventArgs e) {
        e.Handled = true;
        Result = null;
        App?.RequestStop(this);
    }

    protected override bool OnIsRunningChanging(bool oldValue, bool newValue) {
        if (!newValue) {
            // Stopping - result should already be set by button handlers
            // This is a fallback if closed via other means
            Result ??= null;
        }
        return base.OnIsRunningChanging(oldValue, newValue);
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _disposable.Dispose();
        }
        base.Dispose(disposing);
    }
}
