using Terminal.Gui;

namespace Buddy.Cli.Ui;

internal sealed class ChatLayoutManager {
    private readonly ChatLayoutMetrics _metrics;

    public ChatLayoutManager(ChatLayoutMetrics metrics) {
        _metrics = metrics;
    }

    public void ApplyLayout(ChatSessionState state, ChatLayoutParts parts) {
        parts.Input.Height = state.InputHeight;
        parts.InputPanel.Height = state.InputHeight + 1;
        parts.InputPanel.Y = Pos.AnchorEnd(state.InputHeight + _metrics.FooterHeight + 1);
        parts.InfoLayer.Height = _metrics.InfoLayerHeight;
        parts.InfoLayer.Y = Pos.AnchorEnd(state.InputHeight + _metrics.FooterHeight + _metrics.InfoLayerHeight + 1);
        if (state.SessionStarted) {
            parts.History.Height = Dim.Fill(_metrics.SessionHeaderHeight + _metrics.StageHeight + _metrics.InfoLayerHeight + state.InputHeight + _metrics.FooterHeight + 3);
            parts.History.Y = Pos.Bottom(parts.StageLabel);
        }
        if (parts.SuggestionOverlay.Visible) {
            parts.SuggestionOverlay.X = 1;
            parts.SuggestionOverlay.Width = Dim.Fill(12);
            parts.SuggestionOverlay.Height = state.SuggestionOverlayRows;
            parts.SuggestionOverlay.Y = Pos.AnchorEnd(state.InputHeight + _metrics.FooterHeight + state.SuggestionOverlayRows + 2);
        }
        parts.Footer.SetNeedsLayout();
        parts.InfoLayer.SetNeedsLayout();
        parts.InputPanel.SetNeedsLayout();
        parts.History.SetNeedsLayout();
        parts.StartView.SetNeedsLayout();
        parts.SessionView.SetNeedsLayout();
        parts.Window.SetNeedsLayout();
        Application.LayoutAndDraw(forceDraw: false);
    }
}