using Buddy.Core.Configuration;

namespace Buddy.Cli.ViewModels;

/// <summary>
/// Factory for creating dialog ViewModels.
/// Enables proper separation of concerns - ViewModels don't create other ViewModels directly.
/// </summary>
public interface IDialogViewModelFactory {
    ModelSelectionDialogViewModel CreateModelSelection(IReadOnlyList<LlmProviderConfig> providers);
    ProviderConfigDialogViewModel CreateProviderConfig(IReadOnlyList<LlmProviderConfig> providers);
    ProviderEditorDialogViewModel CreateProviderEditor(LlmProviderConfig? existing);
    ModelEditorDialogViewModel CreateModelEditor(LlmModelConfig? existing);
}
