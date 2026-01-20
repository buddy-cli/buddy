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
/// A dialog for editing a single LLM provider.
/// </summary>
public class ProviderEditorDialogView : Dialog<LlmProviderConfig?>, IViewFor<ProviderEditorDialogViewModel> {
    private readonly CompositeDisposable _disposable = [];
    private readonly TextField _nameField;
    private readonly TextField _baseUrlField;
    private readonly TextField _apiKeyField;
    private readonly ListView _modelsView;
    private readonly Label _errorLabel;
    private readonly IApplication _app;
    private readonly IDialogViewModelFactory _dialogFactory;

    object? IViewFor.ViewModel {
        get => ViewModel;
        set => ViewModel = (ProviderEditorDialogViewModel?)value;
    }

    public ProviderEditorDialogViewModel? ViewModel { get; set; }

    public ProviderEditorDialogView(
        ProviderEditorDialogViewModel viewModel,
        IApplication app,
        IDialogViewModelFactory dialogFactory) {
        ViewModel = viewModel;
        _app = app;
        _dialogFactory = dialogFactory;

        Title = viewModel.IsNew ? "Add Provider" : "Edit Provider";
        Width = Dim.Percent(70);
        Height = Dim.Percent(80);

        var nameLabel = new Label { Text = "Provider Name", X = 1, Y = 0 };
        _nameField = new TextField {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Text = ViewModel.Name
        };

        var baseUrlLabel = new Label { Text = "Base URL", X = 1, Y = Pos.Bottom(_nameField) };
        _baseUrlField = new TextField {
            X = 1,
            Y = Pos.Bottom(_nameField) + 1,
            Width = Dim.Fill(2),
            Text = ViewModel.BaseUrl
        };

        var apiKeyLabel = new Label { Text = "API Key", X = 1, Y = Pos.Bottom(_baseUrlField) };
        _apiKeyField = new TextField {
            X = 1,
            Y = Pos.Bottom(_baseUrlField) + 1,
            Width = Dim.Fill(2),
            Text = ViewModel.ApiKey,
            Secret = true
        };

        var modelsLabel = new Label {
            Text = "Models",
            X = 1,
            Y = Pos.Bottom(_apiKeyField) + 1
        };

        _modelsView = new ListView {
            X = 1,
            Y = Pos.Bottom(modelsLabel),
            Width = Dim.Fill(20),
            Height = Dim.Fill(5),
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            AllowsMarking = false
        };
        _modelsView.SetSource(new ObservableCollection<string>(ViewModel.ModelItems));

        var addModelButton = new Button {
            Text = "Add",
            X = Pos.AnchorEnd(15),
            Y = Pos.Top(_modelsView)
        };
        addModelButton.Accepting += OnAddModelClicked;

        var editModelButton = new Button {
            Text = "Edit",
            X = Pos.AnchorEnd(15),
            Y = Pos.Bottom(addModelButton) + 1
        };
        editModelButton.Accepting += OnEditModelClicked;

        var removeModelButton = new Button {
            Text = "Remove",
            X = Pos.AnchorEnd(15),
            Y = Pos.Bottom(editModelButton) + 1
        };
        removeModelButton.Accepting += OnRemoveModelClicked;

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

        Add(
            nameLabel, _nameField,
            baseUrlLabel, _baseUrlField,
            apiKeyLabel, _apiKeyField,
            modelsLabel, _modelsView,
            addModelButton, editModelButton, removeModelButton,
            _errorLabel);
        
        AddButton(saveButton);
        AddButton(cancelButton);

        // Bind text fields to ViewModel
        _nameField
            .Events()
            .TextChanging
            .Subscribe(_ => ViewModel!.Name = _nameField.Text ?? string.Empty)
            .DisposeWith(_disposable);

        _baseUrlField
            .Events()
            .TextChanging
            .Subscribe(_ => ViewModel!.BaseUrl = _baseUrlField.Text ?? string.Empty)
            .DisposeWith(_disposable);

        _apiKeyField
            .Events()
            .TextChanging
            .Subscribe(_ => ViewModel!.ApiKey = _apiKeyField.Text ?? string.Empty)
            .DisposeWith(_disposable);

        // Bind selected model index
        _modelsView
            .Events()
            .SelectedItemChanged
            .Where(_ => ViewModel is not null)
            .Subscribe(_ => {
                if (_modelsView.SelectedItem is int index) {
                    ViewModel!.SelectedModelIndex = index;
                }
            })
            .DisposeWith(_disposable);

        // Bind error message
        ViewModel
            .WhenAnyValue(x => x.ErrorMessage)
            .BindTo(_errorLabel, x => x.Text)
            .DisposeWith(_disposable);

        if (ViewModel.ModelItems.Count > 0) {
            _modelsView.SelectedItem = 0;
        }
    }

    private void OnAddModelClicked(object? sender, CommandEventArgs e) {
        e.Handled = true;

        var editorVm = _dialogFactory.CreateModelEditor(null);
        using var editorDialog = new ModelEditorDialogView(editorVm);
        _app.Run(editorDialog);

        if (editorDialog.Result is { } model) {
            ViewModel!.AddModel(model);
            RefreshModelsView();
        }
    }

    private void OnEditModelClicked(object? sender, CommandEventArgs e) {
        e.Handled = true;

        var selected = ViewModel!.GetSelectedModel();
        if (selected is null) {
            ViewModel.ErrorMessage = "Select a model to edit.";
            return;
        }

        var editorVm = _dialogFactory.CreateModelEditor(selected);
        using var editorDialog = new ModelEditorDialogView(editorVm);
        _app.Run(editorDialog);

        if (editorDialog.Result is { } model) {
            ViewModel.UpdateModel(ViewModel.SelectedModelIndex, model);
            RefreshModelsView();
        }
    }

    private void OnRemoveModelClicked(object? sender, CommandEventArgs e) {
        e.Handled = true;
        ViewModel!.RemoveSelectedModel();
        RefreshModelsView();
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

    private void RefreshModelsView() {
        _modelsView.SetSource(new ObservableCollection<string>(ViewModel!.ModelItems));
        if (ViewModel.SelectedModelIndex >= 0) {
            _modelsView.SelectedItem = ViewModel.SelectedModelIndex;
        }
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _disposable.Dispose();
        }
        base.Dispose(disposing);
    }
}
