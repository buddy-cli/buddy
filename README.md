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
  - `run_terminal`
- Slash commands (handled locally, not sent to the LLM):
  - `/help`, `/clear`, `/model <name>`, `/exit`, `/quit`
- Instruction loading (nearest wins while traversing up from working dir):
  - `AGENTS.md`

## Configuration

Buddy reads configuration from environment variables (and `.env` for local dev):

- `BUDDY_API_KEY` — API key for an OpenAI-compatible endpoint (optional for local gateways)
- `BUDDY_MODEL` — model name (e.g., `gpt-4o-mini`)
- `BUDDY_BASE_URL` — endpoint base URL (e.g., `https://api.openai.com/v1` or `http://localhost:11434/v1`)

A `.env.example` is included. For local dev:

1. Copy `.env.example` to `.env`
2. Fill in values

Note: `.env` is loaded with **no clobber**, meaning real environment variables override `.env`.

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
