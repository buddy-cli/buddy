using System.Collections.ObjectModel;
using Buddy.Core.Configuration;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Buddy.Cli.Ui;

/// <summary>
/// Result of a model selection dialog.
/// </summary>
public sealed class ModelSelectionResult {
    public required LlmProviderConfig Provider { get; init; }
    public required LlmModelConfig Model { get; init; }
}

internal static class ModelSelectionDialog {
    public static bool TrySelectModel(
        IReadOnlyList<LlmProviderConfig> providers,
        out ModelSelectionResult? result) {
        result = null;

        var entries = new List<ModelEntry>();
        for (var p = 0; p < providers.Count; p++) {
            var provider = providers[p];
            for (var m = 0; m < provider.Models.Count; m++) {
                entries.Add(new ModelEntry(p, m));
            }
        }

        if (entries.Count == 0) {
            return false;
        }

        var dialog = new Dialog {
            Title = "Select Model",
            Width = Dim.Percent(60),
            Height = Dim.Percent(70)
        };

        var listView = new ListView {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Height = Dim.Fill(3),
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            AllowsMarking = false
        };
        var items = new ObservableCollection<string>(entries.Select(entry => FormatEntry(entry, providers)).ToList());
        listView.SetSource(items);

        dialog.Add(new Label { Text = "Models", X = 1, Y = 0 }, listView);

        ModelSelectionResult? selection = null;
        var saved = false;
        var saveButton = new Button {
            Text = "Use Model",
            IsDefault = true
        };
        saveButton.Accepting += (_, e) => {
            var selected = listView.SelectedItem;
            if (selected is null || selected < 0 || selected >= entries.Count) {
                return;
            }

            var entry = entries[selected.Value];
            var provider = providers[entry.ProviderIndex];
            var model = provider.Models[entry.ModelIndex];
            selection = new ModelSelectionResult {
                Provider = provider,
                Model = model
            };
            saved = true;
            e.Handled = true;
            Application.RequestStop(dialog);
        };

        var cancelButton = new Button {
            Text = "Cancel"
        };
        cancelButton.Accepting += (_, e) => {
            e.Handled = true;
            Application.RequestStop(dialog);
        };

        dialog.AddButton(saveButton);
        dialog.AddButton(cancelButton);

        listView.SelectedItem = 0;
        Application.Run(dialog);
        dialog.Dispose();

        if (!saved || selection is null) {
            return false;
        }

        result = selection;
        return true;
    }

    private static string FormatEntry(ModelEntry entry, IReadOnlyList<LlmProviderConfig> providers) {
        var provider = providers[entry.ProviderIndex];
        var model = provider.Models[entry.ModelIndex];
        var providerLabel = string.IsNullOrWhiteSpace(provider.Name) ? "(unnamed)" : provider.Name;
        var modelLabel = string.IsNullOrWhiteSpace(model.Name) ? model.System : model.Name;
        return $"{modelLabel} : {providerLabel}";
    }

    private readonly record struct ModelEntry(int ProviderIndex, int ModelIndex);
}
