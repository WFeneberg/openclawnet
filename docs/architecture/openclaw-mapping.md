# OpenClaw to OpenClawNet: Architecture Mapping

## Overview

OpenClaw is a free, open-source agent platform that defines a modular architecture for building AI agents with tool calling, memory, scheduling, and extensible skills. **OpenClawNet** is the .NET 10 implementation of this architecture, preserving the core concepts while using modern .NET patterns and tooling.

---

## Core Concepts Mapping

| OpenClaw Concept | OpenClawNet Implementation | Notes |
|---|---|---|
| **Agent Runtime** | `IAgentOrchestrator` / `IAgentRuntime` | Prompt composition, tool loop, response generation |
| **Model Provider** | `IModelClient` + providers (Ollama, AzureOpenAI, Foundry) | Pluggable abstraction for swapping LLM backends |
| **Tool System** | `ITool` / `IToolRegistry` / `IToolExecutor` | File, shell, web, and scheduled tools with safety guards |
| **Skills** | Markdown + YAML frontmatter loaded from `skills/` | Behavior customization injected into system prompt |
| **Memory** | `IMemoryService` + `ISummaryService` + `IEmbeddingsService` | Session persistence, conversation summarization, semantic search |
| **Persistence** | SQLite via EF Core | ChatSession, ChatMessage, SessionSummary, ToolCallRecord, ScheduledJob |
| **Job Scheduler** | `JobSchedulerService` BackgroundService | Recurring tasks with cron syntax |
| **Web Interface** | Blazor Web App + SignalR | Real-time chat streaming and status updates |
| **API Gateway** | ASP.NET Core Minimal APIs | REST endpoints + SignalR hub for client communication |

---

## Architecture Layers

### 1. **Orchestration Layer** (AppHost)

- **Aspire** orchestrates the entire system
- Service discovery and health checks
- Aspire Dashboard for local observability
- Manages startup order and dependencies

### 2. **Gateway Layer** (Gateway + SignalR Hub)

- REST API for chat, sessions, tools, skills, jobs, memory
- SignalR for real-time token streaming
- Request/response contracts via `OpenClawNet.Models.Abstractions`

### 3. **Agent Layer** (Agent + Runtime)

- **IAgentOrchestrator**: Public boundary, stable contract
- **IAgentRuntime**: Internal runtime (extensible for future Agent Framework integration)
- **IPromptComposer**: System prompt assembly with skills, history, summary
- **IToolExecutor**: Tool calling loop with iteration limits
- **Tool definitions** built from registry manifest

### 4. **Provider Layer** (Models)

- **IModelClient**: Abstract model interface
- **OllamaModelClient**: Local REST API calls to Ollama
- **AzureOpenAIModelClient**: Azure OpenAI SDK integration
- **FoundryModelClient**: Foundry-compatible OpenAI endpoint

### 5. **Tool Layer** (Tools)

- **ITool** interface for pluggable tools
- **FileSystemTool**: Safe file read/write/list
- **ShellTool**: Command execution with approval policy
- **WebTool**: HTTP fetch with SSRF protection
- **SchedulerTool**: Job creation and management

### 6. **Memory Layer** (Memory + Embeddings)

- **IMemoryService**: Session and conversation management
- **ISummaryService**: Long-context summarization
- **IEmbeddingsService**: Local embeddings via Ollama
- Supports semantic search via cosine similarity

### 7. **Persistence Layer** (Storage)

- EF Core DbContext
- SQLite by default (easily swappable)
- Entities: ChatSession, ChatMessage, SessionSummary, ToolCallRecord, ScheduledJob, JobRun

### 8. **UI Layer** (Blazor Web App)

- Server-rendered interactive component
- Real-time streaming via SignalR
- Chat history, session management, tool logs, job monitoring

---

## Data Flow Example: Chat Request

```
Blazor UI (POST /api/chat)
    ↓
Gateway (MapChatEndpoints)
    ↓
IAgentOrchestrator.ProcessAsync()
    ├── Store user message
    ├── Load history
    ├── Check summarization need
    ↓
IPromptComposer.ComposeAsync()
    ├── System prompt
    ├── Active skills
    ├── Session summary
    └── Conversation history
    ↓
IModelClient.CompleteAsync() or StreamAsync()
    ├── If response has tool_calls → Tool Loop
    │   ├── IToolExecutor.ExecuteAsync()
    │   ├── Call model again (max 10x)
    │   └── Return tool results
    └── If no tools → Final response
    ↓
Store assistant message
    ↓
Return to UI via SignalR or HTTP
```

---

## Key Design Decisions

### 1. **Interface-Driven Everything**

Following OpenClaw's principle: every major component is behind an interface. This enables:

- Swapping providers (Ollama → Azure → Foundry) without code changes
- Testing with mocks
- Future integration with frameworks (e.g., Microsoft Agent Framework)

### 2. **Local-First, Cloud-Optional**

- Default configuration uses local Ollama
- Azure/Foundry are additive, not required
- No paid service is a blocker for getting started

### 3. **Safety by Design**

- Path traversal protection in FileSystemTool
- Command blocklist in ShellTool
- SSRF protection in WebTool
- Tool approval policy for sensitive operations
- Max iteration limit (10) to prevent infinite loops

### 4. **Educational Structure**

- Modular 20-project layout (one responsibility per project)
- 4-session teaching path with clear checkpoints
- Aspire Dashboard visible at startup (immediate feedback)
- Demo-friendly with realistic prompts and examples

### 5. **Pluggable DI Architecture**

- Every service registered in DI container
- Easy to swap implementations at startup
- No tight coupling to specific providers or tools
- Future-proof for framework evolution

---

## Comparison Table: Concept → Implementation

| OpenClaw Principle | OpenClawNet Realization | Why This Design |
|---|---|---|
| Modular agent | IAgentOrchestrator + IAgentRuntime | Stable public API, internal evolution possible |
| Pluggable models | IModelClient + provider implementations | Easy demo switches; local dev doesn't need Azure |
| Safe tool calling | ITool + registry + approval policy | Production-ready; prevents misuse |
| Skill customization | Markdown + YAML + runtime loading | Non-technical users can extend behavior |
| Persistent memory | SQLite + EF Core + summarization | Simple, reliable, no external DB needed |
| Scheduling | BackgroundService + cron parser | .NET idiomatic; can be Docker-friendly |
| Real-time UI | Blazor + SignalR | Full C# stack; rich client without Node.js |
| Observability | Aspire Dashboard + structured logging | Built-in; visible from day one |

---

## Future Evolution

OpenClawNet is architected to support:

- **Microsoft Agent Framework** integration (behind IAgentRuntime)
- **Distributed caching** (add IMemoryCache layer)
- **Multi-agent workflows** (Stateful Agent pattern)
- **Custom tool development** (ITool interface is extensible)
- **Auth/RBAC** (Layer into Gateway endpoints)
- **Persistent embeddings** (Swap IEmbeddingsService implementation)

All without breaking the `IAgentOrchestrator` public contract.

---

## Conclusion

OpenClawNet is a faithful .NET translation of OpenClaw's modular agent architecture. By using interfaces, dependency injection, and layered design, it preserves the conceptual elegance of OpenClaw while taking advantage of .NET 10's async/await, Minimal APIs, Blazor, and Aspire orchestration. The result is a reference implementation that is both a **working application** and a **teaching asset**.
