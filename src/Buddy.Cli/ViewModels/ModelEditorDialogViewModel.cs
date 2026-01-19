using Buddy.Core.Configuration;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Buddy.Cli.ViewModels;

/// <summary>
/// ViewModel for editing a single model.
/// </summary>
public partial class ModelEditorDialogViewModel : ReactiveObject {
    [Reactive]
    private string _displayName = string.Empty;

    [Reactive]
    private string _systemName = string.Empty;

    [Reactive]
    private string _errorMessage = string.Empty;

    public bool IsNew { get; }

    public ModelEditorDialogViewModel(LlmModelConfig? existing) {
        IsNew = existing is null;

        if (existing is not null) {
            _displayName = existing.Name;
            _systemName = existing.System;
        }
    }

    public LlmModelConfig? GetResult() {
        var systemValue = SystemName.Trim();
        if (string.IsNullOrWhiteSpace(systemValue)) {
            ErrorMessage = "System name is required.";
            return null;
        }

        return new LlmModelConfig {
            Name = DisplayName,
            System = systemValue
        };
    }
}
