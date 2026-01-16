using System.Collections.ObjectModel;
using Buddy.Core.Configuration;
using Terminal.Gui;

namespace Buddy.Cli.Ui;

internal static class TerminalGuiLayout {
    public static string BannerText { get; } = string.Join("\n", new[] {
        "██████╗ ██╗   ██╗██████╗ ██████╗ ██╗   ██╗",
        "██╔══██╗██║   ██║██╔══██╗██╔══██╗╚██╗ ██╔╝",
        "██████╔╝██║   ██║██║  ██║██║  ██║ ╚████╔╝ ",
        "██╔══██╗██║   ██║██║  ██║██║  ██║  ╚██╔╝  ",
        "██████╔╝╚██████╔╝██████╔╝██████╔╝   ██║   ",
        "╚═════╝  ╚═════╝ ╚═════╝ ╚═════╝    ╚═╝   "
    });

    public static int BannerLines => BannerText.Split('\n').Length;

    public static ChatLayoutParts Build(
        BuddyOptions options,
        string version,
        string bannerText,
        int bannerLines,
        ChatLayoutMetrics metrics,
        string currentStage,
        int inputHeight,
        ObservableCollection<string> suggestionItems) {
        var window = new Window {
            Title = "buddy - coding agent (Esc to quit)",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        var startView = new View {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        var headerTitle = new Label {
            Text = bannerText,
            X = 1,
            Y = 0
        };

        var headerInfo = new Label {
            Text = $"model {options.Model}  •  base url {options.BaseUrl ?? "(default)"}",
            X = 1,
            Y = bannerLines
        };

        startView.Add(headerTitle, headerInfo);

        var sessionView = new View {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Visible = false
        };

        var sessionHeader = new View {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = metrics.SessionHeaderHeight
        };

        var sessionTitle = new Label {
            Text = $"buddy  • model {options.Model}",
            X = 1,
            Y = 0
        };

        sessionHeader.Add(sessionTitle);

        var stageSpinner = new SpinnerView {
            X = 1,
            Y = Pos.Bottom(sessionHeader),
            Width = 1,
            Height = 1,
            AutoSpin = false,
            Visible = false
        };

        var stageLabel = new Label {
            Text = $"Stage: {currentStage}",
            X = Pos.Right(stageSpinner) + 1,
            Y = Pos.Top(stageSpinner)
        };

        var history = new TextView {
            X = 0,
            Y = Pos.Bottom(stageLabel),
            Width = Dim.Fill(),
            Height = Dim.Fill(metrics.SessionHeaderHeight + metrics.StageHeight + metrics.InfoLayerHeight + inputHeight + metrics.FooterHeight + 3),
            WordWrap = true,
            CanFocus = false,
            TabStop = TabBehavior.NoStop
        };

        var inputPanel = new View {
            X = 0,
            Y = Pos.AnchorEnd(inputHeight + metrics.FooterHeight + 1),
            Width = Dim.Fill(),
            Height = inputHeight + 1,
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };

        var infoLayer = new View {
            X = 0,
            Y = Pos.AnchorEnd(inputHeight + metrics.FooterHeight + metrics.InfoLayerHeight + 1),
            Width = Dim.Fill(),
            Height = metrics.InfoLayerHeight,
            CanFocus = false,
            TabStop = TabBehavior.NoStop
        };

        var footer = new View {
            X = 0,
            Y = Pos.AnchorEnd(metrics.FooterHeight),
            Width = Dim.Fill(),
            Height = metrics.FooterHeight,
            CanFocus = false,
            TabStop = TabBehavior.NoStop
        };

        var footerLeft = new Label {
            Text = options.WorkingDirectory,
            X = 1,
            Y = 0
        };

        var footerRight = new Label {
            Text = version,
            X = Pos.AnchorEnd(version.Length + 1),
            Y = 0
        };

        footer.Add(footerLeft, footerRight);

        var inputHint = new Label {
            Text = "Type a message or /command (e.g., /help) ",
            X = 1,
            Y = 0
        };

        var suggestionOverlay = new ListView {
            X = 1,
            Y = 0,
            Width = Dim.Fill(12),
            Height = 0,
            CanFocus = false,
            TabStop = TabBehavior.NoStop,
            AllowsMarking = false,
            Visible = false
        };
        suggestionOverlay.SetSource(suggestionItems);

        infoLayer.Add(inputHint);

        var input = new TextView {
            X = 1,
            Y = 1,
            Width = Dim.Fill(12),
            Height = inputHeight,
            WordWrap = true,
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };

        var sendButton = new Button {
            Text = "Send",
            X = Pos.AnchorEnd(10),
            Y = Pos.Bottom(input) - 1,
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };

        inputPanel.Add(input, sendButton);
        sessionView.Add(sessionHeader, stageSpinner, stageLabel, history);
        window.Add(startView, sessionView, infoLayer, inputPanel, footer, suggestionOverlay);

        return new ChatLayoutParts(
            window,
            startView,
            sessionView,
            stageSpinner,
            stageLabel,
            history,
            inputPanel,
            infoLayer,
            footer,
            suggestionOverlay,
            input,
            sendButton);
    }
}