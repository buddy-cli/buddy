using Buddy.Core.Agents;
using Buddy.Core.Configuration;
using Buddy.Core.Application;
using Buddy.LLM;
using Terminal.Gui;

namespace Buddy.Cli.Ui;

internal sealed class ChatController {
    //
    // public void UpdateLogStyle() {
    //     _context.LayoutParts.History.ColorScheme = _state.HistoryBuffer.Length == 0 ? _context.IdleLogScheme : _context.ActiveLogScheme;
    // }
    //
    // private void AppendHistory(string text) {
    //     _state.HistoryBuffer.Append(text);
    //     _context.LayoutParts.History.MoveEnd();
    //     _context.LayoutParts.History.InsertText(text);
    //     UpdateLogStyle();
    //     _context.LayoutParts.History.MoveEnd();
    //     _context.LayoutParts.History.SetNeedsDraw();
    //     Application.LayoutAndDraw(forceDraw: false);
    // }
    //
    // private void AppendHistoryOnUi(string text) {
    //     Application.Invoke(() => AppendHistory(text));
    // }

    // private bool TryHandleCommand(string rawInput) {
    //     if (!rawInput.StartsWith("/", StringComparison.Ordinal)) {
    //         return false;
    //     }
    //
    //     var parts = rawInput.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    //     var cmd = parts[0];
    //     var arg = parts.Length > 1 ? parts[1] : null;
    //
    //     switch (cmd) {
    //         case "/help":
    //         case "/clear":
    //             _agent.ClearHistory();
    //             _state.HistoryBuffer.Clear();
    //             _context.LayoutParts.History.Text = string.Empty;
    //             UpdateLogStyle();
    //             AppendHistoryOnUi("\n(history cleared)\n");
    //             return true;
    //         case "/model":
    //             ShowModelDialog();
    //             return true;
    //         case "/provider":
    //             ShowProviderDialog();
    //             return true;
    //         case "/exit":
    //         case "/quit":
    //             if (_state.TurnCts is not null && !_state.TurnCts.IsCancellationRequested) {
    //                 _state.TurnCts.Cancel();
    //             }
    //             Application.RequestStop(_context.LayoutParts.Window);
    //             return true;
    //         default:
    //             AppendHistoryOnUi("\nunknown command — try /help\n");
    //             return true;
    //     }
    // }
}

