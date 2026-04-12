# OpenClawNet Components

## Agent Runtime

The agent runtime (`OpenClawNet.Agent`) orchestrates the complete AI interaction loop:

1. **Prompt Composition** (`IPromptComposer`) — Assembles system prompt + skills + memory + history
2. **Model Invocation** (`IModelClient`) — Sends to the configured LLM provider
3. **Tool Loop** (`IToolExecutor`) — Parses tool calls, executes them, feeds results back
4. **Response** — Final assistant response after all tool iterations
5. **Summarization** (`ISummaryService`) — Condenses long conversations (>20 messages)

Max tool iterations: 10 (prevents infinite loops).

**Internal Design:** The agent runtime uses an internal `IAgentRuntime` abstraction (with `DefaultAgentRuntime` implementation) that separates the public `IAgentOrchestrator` boundary from the implementation details. This allows future integration with frameworks like Microsoft Agent Framework without breaking the public API.

## Model Providers

All providers implement `IModelClient`:

- **Ollama** — Local REST API at `http://localhost:11434`
- **Azure OpenAI** — Via `Azure.AI.OpenAI` SDK
- **Foundry** — OpenAI-compatible HTTP endpoint

Provider switching is configured in `appsettings.json`.

## Tool Framework

Tools implement `ITool` and register with `IToolRegistry`:

| Tool | Category | Approval Required |
|------|----------|-------------------|
| `file_system` | filesystem | No |
| `shell` | shell | Yes |
| `web_fetch` | web | No |
| `schedule` | scheduler | No |

Safety features:

- Path traversal protection (FileSystem)
- Blocked command list (Shell)
- SSRF protection (Web)
- Tool approval policy interface

## Skills System

Markdown files with YAML frontmatter:

- Built-in skills: `skills/built-in/`
- Sample skills: `skills/samples/`
- Workspace overrides (later directories win)
- Runtime enable/disable via API

## Memory and Embeddings

### Memory Service

`IMemoryService` tracks sessions and conversation state:

- Load conversation history by session ID
- Store chat messages with metadata
- Query past conversations by timestamp or session

### Summarization Service

`ISummaryService` keeps long contexts manageable:

- Summarize conversations >20 messages automatically
- Keep recent messages in full context
- Compress older messages into semantic summaries
- Improves token efficiency and model focus

### Embeddings Service

`IEmbeddingsService` provides semantic search:

- **Default:** Ollama embeddings endpoint (local, free, no API keys)
- Converts text to vector embeddings for similarity search
- Supports batch embedding operations
- Cosine similarity calculation for finding relevant past conversations
- Future: Swappable implementations (Azure, Foundry embeddings)

**Local-First Advantage:** Embeddings stay local; no external API needed for semantic search.

## Storage

SQLite via EF Core with these entities:

- `ChatSession` — Chat sessions with metadata
- `ChatMessageEntity` — Messages with role, order index
- `SessionSummary` — Conversation summaries
- `ToolCallRecord` — Tool execution audit log
- `ScheduledJob` — Job definitions with cron support
- `JobRun` — Job execution history
- `ProviderSetting` — Key-value settings
