using System.Collections.ObjectModel;
using Buddy.Core.Configuration;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Buddy.Cli.ViewModels;

/// <summary>
/// Result of the provider configuration dialog.
/// </summary>
public sealed class ProviderConfigResult {
    public required List<LlmProviderConfig> Providers { get; init; }
}

/// <summary>
/// ViewModel for the provider configuration dialog that lists all providers.
/// </summary>
public partial class ProviderConfigDialogViewModel : ReactiveObject {
    private readonly List<LlmProviderConfig> _providers;

    [Reactive]
    private int _selectedIndex;

    [Reactive]
    private string _errorMessage = string.Empty;

    public ReadOnlyObservableCollection<string> Items { get; }
    private readonly ObservableCollection<string> _items = [];

    public ProviderConfigDialogViewModel(IReadOnlyList<LlmProviderConfig> providers) {
        _providers = providers.Select(CloneProvider).ToList();
        Items = new ReadOnlyObservableCollection<string>(_items);
        RefreshItems();
    }

    public bool HasItems => _providers.Count > 0;

    public bool CanSave => _providers.Count > 0;

    public LlmProviderConfig? GetSelectedProvider() {
        if (_selectedIndex < 0 || _selectedIndex >= _providers.Count) {
            return null;
        }
        return _providers[_selectedIndex];
    }

    public void AddProvider(LlmProviderConfig provider) {
        _providers.Add(CloneProvider(provider));
        RefreshItems();
        SelectedIndex = _providers.Count - 1;
        ErrorMessage = string.Empty;
    }

    public void UpdateProvider(int index, LlmProviderConfig provider) {
        if (index >= 0 && index < _providers.Count) {
            _providers[index] = CloneProvider(provider);
            RefreshItems();
            SelectedIndex = index;
            ErrorMessage = string.Empty;
        }
    }

    public void RemoveSelectedProvider() {
        if (_selectedIndex < 0 || _selectedIndex >= _providers.Count) {
            ErrorMessage = "Select a provider to remove.";
            return;
        }

        _providers.RemoveAt(_selectedIndex);
        RefreshItems();
        SelectedIndex = Math.Min(_selectedIndex, _providers.Count - 1);
        ErrorMessage = string.Empty;
    }

    public ProviderConfigResult? GetResult() {
        if (_providers.Count == 0) {
            ErrorMessage = "Add at least one provider.";
            return null;
        }

        return new ProviderConfigResult {
            Providers = _providers.Select(CloneProvider).ToList()
        };
    }

    private void RefreshItems() {
        _items.Clear();
        foreach (var provider in _providers) {
            _items.Add(GetProviderDisplay(provider));
        }
    }

    private static string GetProviderDisplay(LlmProviderConfig provider) {
        if (!string.IsNullOrWhiteSpace(provider.Name)) {
            return provider.Name;
        }

        if (!string.IsNullOrWhiteSpace(provider.BaseUrl)) {
            return provider.BaseUrl;
        }

        return "(unnamed provider)";
    }

    private static LlmProviderConfig CloneProvider(LlmProviderConfig provider) {
        return new LlmProviderConfig {
            Name = provider.Name,
            BaseUrl = provider.BaseUrl,
            ApiKey = provider.ApiKey,
            Models = provider.Models.Select(CloneModel).ToList()
        };
    }

    private static LlmModelConfig CloneModel(LlmModelConfig model) {
        return new LlmModelConfig {
            Name = model.Name,
            System = model.System
        };
    }
}
