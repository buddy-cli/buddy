using System.Reactive.Disposables;
using Buddy.Cli.ViewModels;
using Buddy.Core.Configuration;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using Terminal.Gui;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Buddy.Cli.Views;

/// <summary>
/// A dialog for editing a single model.
/// </summary>
public class ModelEditorDialogView : Dialog<LlmModelConfig?>, IViewFor<ModelEditorDialogViewModel> {
    private readonly CompositeDisposable _disposable = [];
    private readonly TextField _displayNameField;
    private readonly TextField _systemNameField;
    private readonly Label _errorLabel;

    object? IViewFor.ViewModel {
        get => ViewModel;
        set => ViewModel = (ModelEditorDialogViewModel?)value;
    }

    public ModelEditorDialogViewModel? ViewModel { get; set; }

    public ModelEditorDialogView(ModelEditorDialogViewModel viewModel) {
        ViewModel = viewModel;

        Title = viewModel.IsNew ? "Add Model" : "Edit Model";
        Width = Dim.Percent(50);
        Height = 12;

        var displayNameLabel = new Label { Text = "Display Name (optional)", X = 1, Y = 0 };
        _displayNameField = new TextField {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Text = ViewModel.DisplayName
        };

        var systemNameLabel = new Label { Text = "System Name", X = 1, Y = Pos.Bottom(_displayNameField) };
        _systemNameField = new TextField {
            X = 1,
            Y = Pos.Bottom(_displayNameField) + 1,
            Width = Dim.Fill(2),
            Text = ViewModel.SystemName
        };

        _errorLabel = new Label {
            Text = string.Empty,
            X = 1,
            Y = Pos.Bottom(_systemNameField) + 1,
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

        Add(displayNameLabel, _displayNameField, systemNameLabel, _systemNameField, _errorLabel);
        AddButton(saveButton);
        AddButton(cancelButton);

        // Bind text fields to ViewModel
        _displayNameField
            .Events()
            .TextChanging
            .Subscribe(_ => ViewModel!.DisplayName = _displayNameField.Text ?? string.Empty)
            .DisposeWith(_disposable);

        _systemNameField
            .Events()
            .TextChanging
            .Subscribe(_ => ViewModel!.SystemName = _systemNameField.Text ?? string.Empty)
            .DisposeWith(_disposable);

        // Bind error message
        ViewModel
            .WhenAnyValue(x => x.ErrorMessage)
            .BindTo(_errorLabel, x => x.Text)
            .DisposeWith(_disposable);
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

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _disposable.Dispose();
        }
        base.Dispose(disposing);
    }
}
