using Buddy.Core.Configuration;

namespace Buddy.Cli.ViewModels;

/// <summary>
/// Default implementation of <see cref="IDialogViewModelFactory"/>.
/// </summary>
public class DialogViewModelFactory : IDialogViewModelFactory {
    public ModelSelectionDialogViewModel CreateModelSelection(IReadOnlyList<LlmProviderConfig> providers) {
        return new ModelSelectionDialogViewModel(providers);
    }

    public ProviderConfigDialogViewModel CreateProviderConfig(IReadOnlyList<LlmProviderConfig> providers) {
        return new ProviderConfigDialogViewModel(providers);
    }

    public ProviderEditorDialogViewModel CreateProviderEditor(LlmProviderConfig? existing) {
        return new ProviderEditorDialogViewModel(existing);
    }

    public ModelEditorDialogViewModel CreateModelEditor(LlmModelConfig? existing) {
        return new ModelEditorDialogViewModel(existing);
    }
}
