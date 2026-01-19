using System.Collections.ObjectModel;
using Buddy.Core.Configuration;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Buddy.Cli.ViewModels;

/// <summary>
/// ViewModel for editing a single provider.
/// </summary>
public partial class ProviderEditorDialogViewModel : ReactiveObject {
    private readonly List<LlmModelConfig> _models;

    [Reactive]
    private string _name = string.Empty;

    [Reactive]
    private string _baseUrl = string.Empty;

    [Reactive]
    private string _apiKey = string.Empty;

    [Reactive]
    private int _selectedModelIndex;

    [Reactive]
    private string _errorMessage = string.Empty;

    public bool IsNew { get; }

    public ReadOnlyObservableCollection<string> ModelItems { get; }
    private readonly ObservableCollection<string> _modelItems = [];

    public ProviderEditorDialogViewModel(LlmProviderConfig? existing) {
        IsNew = existing is null;
        _models = existing?.Models.Select(CloneModel).ToList() ?? [];

        if (existing is not null) {
            _name = existing.Name;
            _baseUrl = existing.BaseUrl;
            _apiKey = existing.ApiKey;
        }

        ModelItems = new ReadOnlyObservableCollection<string>(_modelItems);
        RefreshModelItems();
    }

    public LlmModelConfig? GetSelectedModel() {
        if (_selectedModelIndex < 0 || _selectedModelIndex >= _models.Count) {
            return null;
        }
        return _models[_selectedModelIndex];
    }

    public void AddModel(LlmModelConfig model) {
        _models.Add(CloneModel(model));
        RefreshModelItems();
        SelectedModelIndex = _models.Count - 1;
        ErrorMessage = string.Empty;
    }

    public void UpdateModel(int index, LlmModelConfig model) {
        if (index >= 0 && index < _models.Count) {
            _models[index] = CloneModel(model);
            RefreshModelItems();
            SelectedModelIndex = index;
            ErrorMessage = string.Empty;
        }
    }

    public void RemoveSelectedModel() {
        if (_selectedModelIndex < 0 || _selectedModelIndex >= _models.Count) {
            ErrorMessage = "Select a model to remove.";
            return;
        }

        _models.RemoveAt(_selectedModelIndex);
        RefreshModelItems();
        SelectedModelIndex = Math.Min(_selectedModelIndex, _models.Count - 1);
        ErrorMessage = string.Empty;
    }

    public LlmProviderConfig? GetResult() {
        var baseUrl = BaseUrl.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl)) {
            ErrorMessage = "Base URL is required.";
            return null;
        }

        if (_models.Count == 0) {
            ErrorMessage = "Add at least one model.";
            return null;
        }

        return new LlmProviderConfig {
            Name = Name,
            BaseUrl = baseUrl,
            ApiKey = ApiKey,
            Models = _models.Select(CloneModel).ToList()
        };
    }

    private void RefreshModelItems() {
        _modelItems.Clear();
        foreach (var model in _models) {
            _modelItems.Add(GetModelDisplay(model));
        }
    }

    private static string GetModelDisplay(LlmModelConfig model) {
        if (string.IsNullOrWhiteSpace(model.Name)) {
            return model.System;
        }
        return $"{model.Name} ({model.System})";
    }

    private static LlmModelConfig CloneModel(LlmModelConfig model) {
        return new LlmModelConfig {
            Name = model.Name,
            System = model.System
        };
    }
}
