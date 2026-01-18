using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Buddy.Core.Configuration;
using Terminal.Gui;

namespace Buddy.Cli.Ui;

internal static class ProviderConfigDialog {
    // public static bool TryEditProviders(
    //     IReadOnlyList<LlmProviderConfig> providers,
    //     out List<LlmProviderConfig> updatedProviders) {
    //     var providersResult = providers.Select(CloneProvider).ToList();
    //     updatedProviders = providersResult.Select(CloneProvider).ToList();
    //     var workingProviders = updatedProviders.Select(CloneProvider).ToList();
    //     var providerItems = new ObservableCollection<string>();
    //     var errorLabel = new Label {
    //         Text = string.Empty,
    //         X = 1,
    //         Y = Pos.AnchorEnd(2),
    //         Width = Dim.Fill(),
    //         Height = 1
    //     };
    //
    //     var listView = new ListView {
    //         X = 1,
    //         Y = 2,
    //         Width = Dim.Percent(60),
    //         Height = Dim.Fill(5),
    //         CanFocus = true,
    //         TabStop = TabBehavior.TabStop,
    //         AllowsMarking = false
    //     };
    //
    //     var hintLabel = new Label {
    //         Text = "Select a provider to edit.",
    //         X = 1,
    //         Y = Pos.AnchorEnd(3),
    //         Width = Dim.Fill(),
    //         Height = 1
    //     };
    //     listView.SetSource(providerItems);
    //
    //     var addButton = new Button {
    //         Text = "Add",
    //         X = Pos.Right(listView) + 2,
    //         Y = 2
    //     };
    //
    //     var editButton = new Button {
    //         Text = "Edit",
    //         X = Pos.Right(listView) + 2,
    //         Y = Pos.Bottom(addButton) + 1
    //     };
    //
    //     var dialog = new Dialog {
    //         Title = "Provider Configuration",
    //         Width = Dim.Percent(80),
    //         Height = Dim.Percent(80)
    //     };
    //
    //     dialog.Add(new Label {
    //         Text = "Providers",
    //         X = 1,
    //         Y = 0
    //     });
    //
    //     dialog.Add(listView, addButton, editButton, hintLabel, errorLabel);
    //
    //     var saved = false;
    //
    //     var saveButton = new Button {
    //         Text = "Save",
    //         IsDefault = true
    //     };
    //     saveButton.Accepting += (_, _) => {
    //         if (workingProviders.Count == 0) {
    //             ShowError("Add at least one provider.");
    //             return;
    //         }
    //
    //         providersResult = workingProviders.Select(CloneProvider).ToList();
    //         saved = true;
    //         Application.RequestStop(dialog);
    //     };
    //
    //     var cancelButton = new Button {
    //         Text = "Cancel"
    //     };
    //     cancelButton.Accepting += (_, _) => {
    //         Application.RequestStop(dialog);
    //     };
    //
    //     dialog.AddButton(saveButton);
    //     dialog.AddButton(cancelButton);
    //
    //     void RefreshProviderItems() {
    //         providerItems.Clear();
    //         foreach (var provider in workingProviders) {
    //             providerItems.Add(GetProviderDisplay(provider));
    //         }
    //     }
    //
    //     void ShowError(string message) {
    //         errorLabel.Text = message;
    //     }
    //
    //     addButton.Accepting += (_, _) => {
    //         if (!ProviderEditorDialog.TryEdit(null, out var provider)) {
    //             return;
    //         }
    //
    //         workingProviders.Add(provider);
    //         RefreshProviderItems();
    //         listView.SelectedItem = workingProviders.Count - 1;
    //         ShowError(string.Empty);
    //     };
    //
    //     editButton.Accepting += (_, _) => {
    //         var selectedIndex = listView.SelectedItem;
    //         if (selectedIndex < 0 || selectedIndex >= workingProviders.Count) {
    //             ShowError("Select a provider to edit.");
    //             return;
    //         }
    //
    //         if (!ProviderEditorDialog.TryEdit(workingProviders[selectedIndex], out var provider)) {
    //             return;
    //         }
    //
    //         workingProviders[selectedIndex] = provider;
    //         RefreshProviderItems();
    //         listView.SelectedItem = selectedIndex;
    //         ShowError(string.Empty);
    //     };
    //
    //     RefreshProviderItems();
    //     if (workingProviders.Count > 0) {
    //         listView.SelectedItem = 0;
    //     }
    //
    //     Application.Run(dialog);
    //     dialog.Dispose();
    //
    //     if (!saved) {
    //         updatedProviders = providers.Select(CloneProvider).ToList();
    //         return false;
    //     }
    //
    //     updatedProviders = providersResult.Select(CloneProvider).ToList();
    //     return true;
    // }
    //
    // private static string GetProviderDisplay(LlmProviderConfig provider) {
    //     if (!string.IsNullOrWhiteSpace(provider.Name)) {
    //         return provider.Name;
    //     }
    //
    //     if (!string.IsNullOrWhiteSpace(provider.BaseUrl)) {
    //         return provider.BaseUrl;
    //     }
    //
    //     return "(unnamed provider)";
    // }
    //
    // private static LlmProviderConfig CloneProvider(LlmProviderConfig provider) {
    //     return new LlmProviderConfig {
    //         Name = provider.Name,
    //         BaseUrl = provider.BaseUrl,
    //         ApiKey = provider.ApiKey,
    //         Models = provider.Models.Select(CloneModel).ToList()
    //     };
    // }
    //
    // private static LlmModelConfig CloneModel(LlmModelConfig model) {
    //     return new LlmModelConfig {
    //         Name = model.Name,
    //         System = model.System
    //     };
    // }
    //
    // private static class ProviderEditorDialog {
    //     public static bool TryEdit(LlmProviderConfig? existing, out LlmProviderConfig provider) {
    //         var providerResult = existing is null ? new LlmProviderConfig() : CloneProvider(existing);
    //         provider = CloneProvider(providerResult);
    //         var models = providerResult.Models.Select(CloneModel).ToList();
    //         var modelItems = new ObservableCollection<string>();
    //         var errorLabel = new Label {
    //             Text = string.Empty,
    //             X = 1,
    //             Y = Pos.AnchorEnd(2),
    //             Width = Dim.Fill(),
    //             Height = 1
    //         };
    //
    //         var nameField = new TextField {
    //             X = 1,
    //             Y = 1,
    //             Width = Dim.Fill(2),
    //             Text = providerResult.Name
    //         };
    //
    //         var baseUrlField = new TextField {
    //             X = 1,
    //             Y = Pos.Bottom(nameField) + 1,
    //             Width = Dim.Fill(2),
    //             Text = providerResult.BaseUrl
    //         };
    //
    //         var apiKeyField = new TextField {
    //             X = 1,
    //             Y = Pos.Bottom(baseUrlField) + 1,
    //             Width = Dim.Fill(2),
    //             Text = providerResult.ApiKey,
    //             Secret = true
    //         };
    //
    //         var modelsLabel = new Label {
    //             Text = "Models",
    //             X = 1,
    //             Y = Pos.Bottom(apiKeyField) + 1
    //         };
    //
    //         var modelsView = new ListView {
    //             X = 1,
    //             Y = Pos.Bottom(modelsLabel),
    //             Width = Dim.Fill(20),
    //             Height = Dim.Fill(7),
    //             CanFocus = true,
    //             TabStop = TabBehavior.TabStop,
    //             AllowsMarking = false
    //         };
    //         modelsView.SetSource(modelItems);
    //
    //         var addModelButton = new Button {
    //             Text = "Add",
    //             X = Pos.AnchorEnd(15),
    //             Y = Pos.Top(modelsView)
    //         };
    //
    //         var editModelButton = new Button {
    //             Text = "Edit",
    //             X = Pos.AnchorEnd(15),
    //             Y = Pos.Bottom(addModelButton) + 1
    //         };
    //
    //         var removeModelButton = new Button {
    //             Text = "Remove",
    //             X = Pos.AnchorEnd(15),
    //             Y = Pos.Bottom(editModelButton) + 1
    //         };
    //
    //         var dialog = new Dialog {
    //             Title = existing is null ? "Add Provider" : "Edit Provider",
    //             Width = Dim.Percent(70),
    //             Height = Dim.Percent(80)
    //         };
    //
    //         dialog.Add(
    //             new Label { Text = "Provider Name", X = 1, Y = 0 },
    //             nameField,
    //             new Label { Text = "Base URL", X = 1, Y = Pos.Bottom(nameField) },
    //             baseUrlField,
    //             new Label { Text = "API Key", X = 1, Y = Pos.Bottom(baseUrlField) },
    //             apiKeyField,
    //             modelsLabel,
    //             modelsView,
    //             addModelButton,
    //             editModelButton,
    //             removeModelButton,
    //             errorLabel);
    //
    //         var saved = false;
    //         var saveButton = new Button {
    //             Text = "Save",
    //             IsDefault = true
    //         };
    //
    //         saveButton.Accepting += (_, _) => {
    //             var baseUrl = baseUrlField.Text?.ToString().Trim() ?? string.Empty;
    //             if (string.IsNullOrWhiteSpace(baseUrl)) {
    //                 errorLabel.Text = "Base URL is required.";
    //                 return;
    //             }
    //
    //             if (models.Count == 0) {
    //                 errorLabel.Text = "Add at least one model.";
    //                 return;
    //             }
    //
    //             providerResult = new LlmProviderConfig {
    //                 Name = nameField.Text?.ToString() ?? string.Empty,
    //                 BaseUrl = baseUrl,
    //                 ApiKey = apiKeyField.Text?.ToString() ?? string.Empty,
    //                 Models = models.Select(CloneModel).ToList()
    //             };
    //
    //             saved = true;
    //             Application.RequestStop(dialog);
    //         };
    //
    //         var cancelButton = new Button {
    //             Text = "Cancel"
    //         };
    //         cancelButton.Accepting += (_, _) => Application.RequestStop(dialog);
    //
    //         dialog.AddButton(saveButton);
    //         dialog.AddButton(cancelButton);
    //
    //         void RefreshModelItems() {
    //             modelItems.Clear();
    //             foreach (var model in models) {
    //                 modelItems.Add(GetModelDisplay(model));
    //             }
    //         }
    //
    //         addModelButton.Accepting += (_, _) => {
    //             if (!ModelEditorDialog.TryEdit(null, out var model)) {
    //                 return;
    //             }
    //
    //             models.Add(model);
    //             RefreshModelItems();
    //             modelsView.SelectedItem = models.Count - 1;
    //             errorLabel.Text = string.Empty;
    //         };
    //
    //         editModelButton.Accepting += (_, _) => {
    //             var selectedIndex = modelsView.SelectedItem;
    //             if (selectedIndex < 0 || selectedIndex >= models.Count) {
    //                 errorLabel.Text = "Select a model to edit.";
    //                 return;
    //             }
    //
    //             if (!ModelEditorDialog.TryEdit(models[selectedIndex], out var model)) {
    //                 return;
    //             }
    //
    //             models[selectedIndex] = model;
    //             RefreshModelItems();
    //             modelsView.SelectedItem = selectedIndex;
    //             errorLabel.Text = string.Empty;
    //         };
    //
    //         removeModelButton.Accepting += (_, _) => {
    //             var selectedIndex = modelsView.SelectedItem;
    //             if (selectedIndex < 0 || selectedIndex >= models.Count) {
    //                 errorLabel.Text = "Select a model to remove.";
    //                 return;
    //             }
    //
    //             models.RemoveAt(selectedIndex);
    //             RefreshModelItems();
    //             modelsView.SelectedItem = Math.Min(selectedIndex, models.Count - 1);
    //             errorLabel.Text = string.Empty;
    //         };
    //
    //         RefreshModelItems();
    //         if (models.Count > 0) {
    //             modelsView.SelectedItem = 0;
    //         }
    //
    //         Application.Run(dialog);
    //         dialog.Dispose();
    //
    //         if (!saved) {
    //             provider = CloneProvider(providerResult);
    //             return false;
    //         }
    //
    //         provider = CloneProvider(providerResult);
    //         return true;
    //     }
    //
    //     private static string GetModelDisplay(LlmModelConfig model) {
    //         if (string.IsNullOrWhiteSpace(model.Name)) {
    //             return model.System;
    //         }
    //
    //         return $"{model.Name} ({model.System})";
    //     }
    // }
    //
    // private static class ModelEditorDialog {
    //     public static bool TryEdit(LlmModelConfig? existing, out LlmModelConfig model) {
    //         var modelResult = existing is null ? new LlmModelConfig() : CloneModel(existing);
    //         model = CloneModel(modelResult);
    //         var nameField = new TextField {
    //             X = 1,
    //             Y = 1,
    //             Width = Dim.Fill(2),
    //             Text = modelResult.Name
    //         };
    //
    //         var systemField = new TextField {
    //             X = 1,
    //             Y = Pos.Bottom(nameField) + 1,
    //             Width = Dim.Fill(2),
    //             Text = modelResult.System
    //         };
    //
    //         var errorLabel = new Label {
    //             Text = string.Empty,
    //             X = 1,
    //             Y = Pos.Bottom(systemField) + 1,
    //             Width = Dim.Fill(),
    //             Height = 1
    //         };
    //
    //         var dialog = new Dialog {
    //             Title = existing is null ? "Add Model" : "Edit Model",
    //             Width = Dim.Percent(50),
    //             Height = 12
    //         };
    //
    //         dialog.Add(
    //             new Label { Text = "Display Name (optional)", X = 1, Y = 0 },
    //             nameField,
    //             new Label { Text = "System Name", X = 1, Y = Pos.Bottom(nameField) },
    //             systemField,
    //             errorLabel);
    //
    //         var saved = false;
    //
    //         var saveButton = new Button {
    //             Text = "Save",
    //             IsDefault = true
    //         };
    //         saveButton.Accepting += (_, _) => {
    //             var systemValue = systemField.Text?.ToString().Trim() ?? string.Empty;
    //             if (string.IsNullOrWhiteSpace(systemValue)) {
    //                 errorLabel.Text = "System name is required.";
    //                 return;
    //             }
    //
    //             modelResult = new LlmModelConfig {
    //                 Name = nameField.Text?.ToString() ?? string.Empty,
    //                 System = systemValue
    //             };
    //
    //             saved = true;
    //             Application.RequestStop(dialog);
    //         };
    //
    //         var cancelButton = new Button {
    //             Text = "Cancel"
    //         };
    //         cancelButton.Accepting += (_, _) => Application.RequestStop(dialog);
    //
    //         dialog.AddButton(saveButton);
    //         dialog.AddButton(cancelButton);
    //
    //         Application.Run(dialog);
    //         dialog.Dispose();
    //
    //         if (!saved) {
    //             model = CloneModel(modelResult);
    //             return false;
    //         }
    //
    //         model = CloneModel(modelResult);
    //         return true;
    //     }
    // }
}
