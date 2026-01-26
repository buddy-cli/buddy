using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Buddy.Cli.ViewModels;
using Buddy.Core.Configuration;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Buddy.Cli.Views;

/// <summary>
/// A dialog for configuring LLM providers.
/// </summary>
public class ProviderConfigDialogView : Dialog<ProviderConfigResult?>, IViewFor<ProviderConfigDialogViewModel> {
    private readonly CompositeDisposable _disposable = [];
    private readonly ListView _listView;
    private readonly Label _errorLabel;
    private readonly IApplication _app;
    private readonly IDialogViewModelFactory _dialogFactory;

    object? IViewFor.ViewModel {
        get => ViewModel;
        set => ViewModel = (ProviderConfigDialogViewModel?)value;
    }

    public ProviderConfigDialogViewModel? ViewModel { get; set; }

    public ProviderConfigDialogView(
        ProviderConfigDialogViewModel viewModel,
        IApplication app,
        IDialogViewModelFactory dialogFactory) {
        ViewModel = viewModel;
        _app = app;
        _dialogFactory = dialogFactory;

        Title = "Provider Configuration";
        Width = Dim.Percent(80);
        Height = Dim.Percent(80);

        var label = new Label { Text = "Providers", X = 1, Y = 0 };

        _listView = new ListView {
            X = 1,
            Y = 1,
            Width = Dim.Percent(60),
            Height = Dim.Fill(4),
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            AllowsMarking = false
        };
        _listView.SetSource(new ObservableCollection<string>(ViewModel.Items));

        var addButton = new Button {
            Text = "Add",
            X = Pos.Right(_listView) + 2,
            Y = 1
        };
        addButton.Accepting += OnAddClicked;

        var editButton = new Button {
            Text = "Edit",
            X = Pos.Right(_listView) + 2,
            Y = Pos.Bottom(addButton) + 1
        };
        editButton.Accepting += OnEditClicked;

        var removeButton = new Button {
            Text = "Remove",
            X = Pos.Right(_listView) + 2,
            Y = Pos.Bottom(editButton) + 1
        };
        removeButton.Accepting += OnRemoveClicked;

        var hintLabel = new Label {
            Text = "Select a provider to edit.",
            X = 1,
            Y = Pos.AnchorEnd(3),
            Width = Dim.Fill(),
            Height = 1
        };

        _errorLabel = new Label {
            Text = string.Empty,
            X = 1,
            Y = Pos.AnchorEnd(2),
            Width = Dim.Fill(),
            Height = 1
        };

        var saveButton = new Button {
            Text = "Save",
            IsDefault = true
        };
        saveButton.Accepting += OnSaveClicked;

        var cancelButton = new Button {
            Text = "Cancel"
        };
        cancelButton.Accepting += OnCancelClicked;

        Add(label, _listView, addButton, editButton, removeButton, hintLabel, _errorLabel);
        AddButton(saveButton);
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

        // Bind error message
        ViewModel
            .WhenAnyValue(x => x.ErrorMessage)
            .BindTo(_errorLabel, x => x.Text)
            .DisposeWith(_disposable);

        if (ViewModel.HasItems) {
            _listView.SelectedItem = 0;
        }
    }

    private void OnAddClicked(object? sender, CommandEventArgs e) {
        e.Handled = true;

        var editorVm = _dialogFactory.CreateProviderEditor(null);
        using var editorDialog = new ProviderEditorDialogView(editorVm, _app, _dialogFactory);
        _app.Run(editorDialog);

        if (editorDialog.Result is { } provider) {
            ViewModel!.AddProvider(provider);
            RefreshListView();
        }
    }

    private void OnEditClicked(object? sender, CommandEventArgs e) {
        e.Handled = true;

        var selected = ViewModel!.GetSelectedProvider();
        if (selected is null) {
            ViewModel.ErrorMessage = "Select a provider to edit.";
            return;
        }

        var editorVm = _dialogFactory.CreateProviderEditor(selected);
        using var editorDialog = new ProviderEditorDialogView(editorVm, _app, _dialogFactory);
        _app.Run(editorDialog);

        if (editorDialog.Result is { } provider) {
            ViewModel.UpdateProvider(ViewModel.SelectedIndex, provider);
            RefreshListView();
        }
    }

    private void OnRemoveClicked(object? sender, CommandEventArgs e) {
        e.Handled = true;
        ViewModel!.RemoveSelectedProvider();
        RefreshListView();
    }

    private void OnSaveClicked(object? sender, CommandEventArgs e) {
        e.Handled = true;
        Result = ViewModel!.GetResult();
        if (Result is not null) {
            App?.RequestStop(this);
        }
    }

    private void OnCancelClicked(object? sender, CommandEventArgs e) {
        e.Handled = true;
        Result = null;
        App?.RequestStop(this);
    }

    private void RefreshListView() {
        _listView.SetSource(new ObservableCollection<string>(ViewModel!.Items));
        if (ViewModel.SelectedIndex >= 0) {
            _listView.SelectedItem = ViewModel.SelectedIndex;
        }
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _disposable.Dispose();
        }
        base.Dispose(disposing);
    }
}
