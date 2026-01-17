using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Buddy.Core.Configuration;
using Terminal.Gui;

namespace Buddy.Cli.Ui;

internal static class ModelSelectionDialog {
    public static bool TrySelectModel(
        IReadOnlyList<LlmProviderConfig> providers,
        out int providerIndex,
        out int modelIndex) {
        var selection = new ModelSelection { ProviderIndex = -1, ModelIndex = -1 };
        providerIndex = selection.ProviderIndex;
        modelIndex = selection.ModelIndex;

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

        var saved = false;
        var saveButton = new Button {
            Text = "Use Model",
            IsDefault = true
        };
        saveButton.Accepting += (_, _) => {
            var selected = listView.SelectedItem;
            if (selected < 0 || selected >= entries.Count) {
                return;
            }

            selection = new ModelSelection {
                ProviderIndex = entries[selected].ProviderIndex,
                ModelIndex = entries[selected].ModelIndex
            };
            saved = true;
            Application.RequestStop(dialog);
        };

        var cancelButton = new Button {
            Text = "Cancel"
        };
        cancelButton.Accepting += (_, _) => Application.RequestStop(dialog);

        dialog.AddButton(saveButton);
        dialog.AddButton(cancelButton);

        listView.SelectedItem = 0;
        Application.Run(dialog);
        dialog.Dispose();

        if (!saved) {
            return false;
        }

        providerIndex = selection.ProviderIndex;
        modelIndex = selection.ModelIndex;
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

    private struct ModelSelection {
        public int ProviderIndex { get; init; }
        public int ModelIndex { get; init; }
    }
}
