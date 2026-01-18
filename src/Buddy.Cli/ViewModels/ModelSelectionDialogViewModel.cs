using System.Collections.ObjectModel;
using Buddy.Core.Configuration;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Buddy.Cli.ViewModels;

/// <summary>
/// Result of a model selection dialog.
/// </summary>
public sealed class ModelSelectionResult {
    public required LlmProviderConfig Provider { get; init; }
    public required LlmModelConfig Model { get; init; }
}

/// <summary>
/// ViewModel for the model selection dialog.
/// </summary>
public partial class ModelSelectionDialogViewModel : ReactiveObject {
    private readonly IReadOnlyList<LlmProviderConfig> _providers;
    private readonly List<ModelEntry> _entries = [];

    [Reactive]
    private int _selectedIndex;

    public ReadOnlyObservableCollection<string> Items { get; }
    private readonly ObservableCollection<string> _items = [];

    public ModelSelectionDialogViewModel(IReadOnlyList<LlmProviderConfig> providers) {
        _providers = providers;
        Items = new ReadOnlyObservableCollection<string>(_items);
        
        BuildEntries();
    }

    private void BuildEntries() {
        _entries.Clear();
        _items.Clear();

        for (var p = 0; p < _providers.Count; p++) {
            var provider = _providers[p];
            for (var m = 0; m < provider.Models.Count; m++) {
                _entries.Add(new ModelEntry(p, m));
                _items.Add(FormatEntry(provider, provider.Models[m]));
            }
        }
    }

    private static string FormatEntry(LlmProviderConfig provider, LlmModelConfig model) {
        var providerLabel = string.IsNullOrWhiteSpace(provider.Name) ? "(unnamed)" : provider.Name;
        var modelLabel = string.IsNullOrWhiteSpace(model.Name) ? model.System : model.Name;
        return $"{modelLabel} : {providerLabel}";
    }

    public bool HasItems => _entries.Count > 0;

    public ModelSelectionResult? GetSelectedResult() {
        if (_selectedIndex < 0 || _selectedIndex >= _entries.Count) {
            return null;
        }

        var entry = _entries[_selectedIndex];
        var provider = _providers[entry.ProviderIndex];
        var model = provider.Models[entry.ModelIndex];

        return new ModelSelectionResult {
            Provider = provider,
            Model = model
        };
    }

    private readonly record struct ModelEntry(int ProviderIndex, int ModelIndex);
}
