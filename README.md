# buddy

A research-grade (but intentionally minimal) CLI coding agent built in **.NET 10 / C#**.

This repository is primarily a **research/learning project** for the author to understand how to build **CLI coding agents**:

- OpenAI-compatible chat completion client
- Streaming terminal UX
- Tool calling loop (LLM → tools → results → LLM)
- Deterministic file edits via exact search/replace
- Local slash commands
- Project instruction injection via `AGENTS.md`

It is **not** a production-ready agent and makes trade-offs for simplicity and hackability.

## What works today

- Interactive chat loop with streaming output
- OpenAI-compatible streaming via `POST /v1/chat/completions` (SSE)
- Tools:
  - `read_file`
  - `write_file`
  - `edit_file` (exact search/replace)
  - `list_directory`
  - `glob`
  - `grep`
  - `run_terminal`
- Slash commands (handled locally, not sent to the LLM):
  - `/clear` — clear chat history
  - `/model` — select AI model
  - `/provider` — configure LLM providers
  - `/exit` — exit Buddy
- Instruction loading (nearest wins while traversing up from working dir):
  - `AGENTS.md`

## Configuration

Buddy reads configuration from a JSON config file at `~/.buddy/config.json`. The file is created automatically on first run.

### Config file structure

```json
{
  "Providers": [
    {
      "Name": "OpenAI",
      "BaseUrl": "https://api.openai.com/v1",
      "ApiKey": "sk-...",
      "Models": [
        { "Name": "GPT-4o", "System": "gpt-4o" },
        { "Name": "GPT-4o Mini", "System": "gpt-4o-mini" }
      ]
    },
    {
      "Name": "Ollama",
      "BaseUrl": "http://localhost:11434/v1",
      "ApiKey": "",
      "Models": [
        { "Name": "Llama 3", "System": "llama3" }
      ]
    }
  ]
}
```

- **Providers** — list of LLM provider configurations
  - **Name** — display name for the provider
  - **BaseUrl** — OpenAI-compatible API endpoint
  - **ApiKey** — API key (can be empty for local endpoints like Ollama)
  - **Models** — list of models available from this provider
    - **Name** — display name for the model
    - **System** — model identifier sent to the API

The first provider and its first model are used as defaults. You can switch providers and models at runtime using slash commands.

## Run locally

- `dotnet run --project src/Buddy.Cli/Buddy.Cli.csproj`

If you’re using VS Code, there is also a task:

- **Terminal → Run Task… →** `buddy: run (CLI)`

## Build and pack

- `dotnet build`
- `dotnet test`
- `dotnet pack -c Release`

The tool package is written to `nupkg/`.

## Repo structure

- `src/Buddy.Cli` — global tool entry point + terminal UX
- `src/Buddy.Core` — agent orchestration, tools, instruction loading
- `src/Buddy.LLM` — OpenAI-compatible client
- `tests/*` — unit tests
- `docs/roadmap.md` — roadmap and design notes

## Notes / non-goals (for now)

- No safety sandboxing / confirmations
- Minimal token budgeting / context compaction
- No git integration
- No persistent sessions

## License

TBD.
