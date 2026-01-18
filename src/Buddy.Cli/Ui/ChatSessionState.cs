using System.Text;
using Buddy.LLM;

namespace Buddy.Cli.Ui;

internal sealed class ChatSessionState {
    public ChatSessionState(ILlmMClient currentMClient, int inputHeight, string currentStage) {
        HistoryBuffer = new StringBuilder();
        CurrentMClient = currentMClient;
        InputHeight = inputHeight;
        CurrentStage = currentStage;
    }

    public StringBuilder HistoryBuffer { get; }

    public bool TurnInFlight { get; set; }

    public CancellationTokenSource? TurnCts { get; set; }

    public ILlmMClient CurrentMClient { get; set; }

    public bool SessionStarted { get; set; }

    public int InputHeight { get; set; }

    public int SuggestionOverlayRows { get; set; }

    public string CurrentStage { get; set; }
}