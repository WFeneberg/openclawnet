<!-- .slide: class="title-slide" -->

# OpenClawNet

## Session 1: Foundation + Local Chat

Building an AI Agent Platform with .NET

<div class="presenter-info">
Bruno Capuano | Microsoft Reactor Series
</div>

Note: Welcome everyone! Today we're starting a 4-session journey building an AI agent platform with .NET. All the code is already built — our job is to understand every layer.

---

<!-- .slide: class="content-slide" -->

## Series Roadmap

| Session | Title | Focus |
|---------|-------|-------|
| **→ 1** | **Foundation + Local Chat** | **Architecture, Ollama, SignalR streaming** |
| 2 | Tools & Agent Workflows | Tool framework, agent loop, function calling |
| 3 | Skills & Memory | Skill system, conversation memory, embeddings |
| 4 | Automation & Cloud | Scheduler, Azure OpenAI, production deployment |

Note: We have 4 sessions. Each builds on the previous. Today is the foundation — everything else depends on understanding these layers.

---

<!-- .slide: class="content-slide" -->

## What We're Building Today

A complete AI chatbot stack:

- 🧱 **18-project architecture** with clean separation <!-- .element: class="fragment" -->
- 🦙 **Local AI** with Ollama — no cloud, no API keys <!-- .element: class="fragment" -->
- ⚡ **Real-time streaming** via SignalR WebSocket <!-- .element: class="fragment" -->
- 💾 **Persistent conversations** with EF Core + SQLite <!-- .element: class="fragment" -->
- 🚀 **One-command startup** with .NET Aspire <!-- .element: class="fragment" -->

Note: Everything runs locally on your machine. No cloud accounts needed today.

---

<!-- .slide: class="content-slide" -->

## Prerequisites Check

```bash
dotnet --version          # 10.0.x ✓
ollama list               # llama3.2 ✓
code --version            # VS Code ✓
```

GitHub Copilot extension installed and active ✓

Note: Quick check — everyone has these? If not, you can follow along and set up later using the README.

---

<!-- .slide: class="section-divider" -->

# Stage 1

## Architecture & Core Abstractions

⏱️ 15 minutes

Note: Let's start with the big picture — how 18 projects fit together and why the model interface is the most important design decision.

---

<!-- .slide: class="architecture-slide" -->

## Architecture Overview

![Architecture Overview](../shared/diagrams/architecture-overview.svg)

Note: This is the full system. Today we're covering the bottom three layers: Models, Storage, and Gateway. The agent, tools, skills, and memory come in Sessions 2–3.

---

<!-- .slide: class="architecture-slide" -->

## Solution Structure: 18 Projects

![Solution Structure](../shared/diagrams/solution-structure.svg)

Note: Each project has a single responsibility. Models.Abstractions defines the interface. Models.Ollama implements it. Storage handles persistence. Gateway wires everything together.

---

<!-- .slide: class="code-slide" -->

## The Key Contract: `IModelClient`

```csharp
public interface IModelClient
{
    string ProviderName { get; }

    Task<ChatResponse> CompleteAsync(
        ChatRequest request,
        CancellationToken ct = default);

    IAsyncEnumerable<ChatResponseChunk> StreamAsync(
        ChatRequest request,
        CancellationToken ct = default);

    Task<bool> IsAvailableAsync(
        CancellationToken ct = default);
}
```

<small>📁 `src/OpenClawNet.Models.Abstractions/IModelClient.cs` — 93 LOC</small>

Note: This is the most important interface in the entire platform. Three methods: complete (batch), stream (real-time), and health check. Every provider implements this.

---

<!-- .slide: class="code-slide" -->

## Data Transfer: Records

```csharp
public sealed record ChatRequest
{
    public required string Model { get; init; }
    public required IReadOnlyList<ChatMessage> Messages { get; init; }
    public IReadOnlyList<ToolDefinition>? Tools { get; init; }
    public double? Temperature { get; init; }
    public int? MaxTokens { get; init; }
}

public sealed record ChatResponse
{
    public required string Content { get; init; }
    public required ChatMessageRole Role { get; init; }
    public IReadOnlyList<ToolCall>? ToolCalls { get; init; }
    public required string Model { get; init; }
    public UsageInfo? Usage { get; init; }
}
```

Note: Records are immutable by default. Once created, a ChatRequest can't change. This makes the code predictable and thread-safe. Notice the `required` keyword — the compiler enforces that Model and Messages are always set.

---

<!-- .slide: class="content-slide" -->

## Dependency Injection Wiring

```csharp
// Gateway/Program.cs
builder.Services.AddModelClient<OllamaModelClient>();
builder.Services.Configure<ModelOptions>(
    builder.Configuration.GetSection("Model"));
```

<br>

**One line** swaps the entire model provider:

```csharp
// Switch to Azure OpenAI — nothing else changes
builder.Services.AddModelClient<AzureOpenAIModelClient>();
```

Note: This is the payoff of the interface design. The Gateway doesn't know or care which model provider it's using. DI handles the wiring.

---

<!-- .slide: class="demo-transition" -->

## 🤖 Copilot Moment

### Add XML Documentation to `IModelClient`

Open Copilot Chat → *"Add XML documentation comments to all methods in this interface"*

Note: Let's see Copilot in action. It should understand the semantics of each method and generate meaningful docs — not just repeat the method names.

---

<!-- .slide: class="section-divider" -->

# Stage 2

## Ollama Provider + Data Layer

⏱️ 15 minutes

Note: Now we go one level deeper. How does Ollama actually work? And how do we store conversations?

---

<!-- .slide: class="content-slide" -->

## What is Ollama?

- **Local LLM runtime** — models run on your machine <!-- .element: class="fragment" -->
- **REST API** at `http://localhost:11434` <!-- .element: class="fragment" -->
- **No cloud, no API keys, no cost** <!-- .element: class="fragment" -->
- **One command:** `ollama pull llama3.2` <!-- .element: class="fragment" -->

<br>

```bash
# Verify it's running
curl http://localhost:11434/api/tags
```
<!-- .element: class="fragment" -->

Note: Ollama is the default provider because it removes all barriers. Your data never leaves your machine.

---

<!-- .slide: class="architecture-slide" -->

## Streaming Flow

![Streaming Flow](../shared/diagrams/streaming-flow.svg)

Note: This diagram shows the complete token flow — from Ollama generating text, through NDJSON parsing, IAsyncEnumerable, SignalR, and finally into the browser. Each layer yields tokens as they arrive.

---

<!-- .slide: class="code-slide" -->

## `OllamaModelClient.StreamAsync`

```csharp
public async IAsyncEnumerable<ChatResponseChunk> StreamAsync(
    ChatRequest request,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    // POST /api/chat with stream=true
    var httpRequest = BuildRequest(request, stream: true);
    var response = await _httpClient.SendAsync(
        httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);

    // Read NDJSON stream line-by-line
    await using var stream = await response.Content.ReadAsStreamAsync(ct);
    using var reader = new StreamReader(stream);

    while (await reader.ReadLineAsync(ct) is { } line)
    {
        var chunk = JsonSerializer.Deserialize<OllamaResponse>(line);
        yield return new ChatResponseChunk { Content = chunk.Message.Content };
    }
}
```

<small>📁 `src/OpenClawNet.Models.Ollama/OllamaModelClient.cs` — 181 LOC</small>

Note: Key pattern: yield return creates a pipeline. Each token flows to the caller immediately. No buffering. The EnumeratorCancellation attribute enables cooperative cancellation.

---

<!-- .slide: class="architecture-slide" -->

## Entity Relationship Diagram

![Entity Relationships](../shared/diagrams/entity-relationship.svg)

Note: Seven entities. ChatSession is the root. Each session has messages, summaries, and tool call records. ScheduledJob and JobRun handle automation. ProviderSetting stores per-provider config.

---

<!-- .slide: class="code-slide" -->

## Storage Entities

```csharp
public sealed class ChatSession
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ChatMessageEntity> Messages { get; set; }
}

public sealed class ChatMessageEntity
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Role { get; set; }       // user, assistant, system, tool
    public string Content { get; set; }
    public string? ToolCallsJson { get; set; }
    public int OrderIndex { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

<small>📁 `src/OpenClawNet.Storage/` — 275 LOC</small>

Note: Notice OrderIndex for message sequencing, nullable ToolCallsJson for structured tool data, and timestamps on everything. These design decisions pay off when we add memory and tool tracking later.

---

<!-- .slide: class="demo-transition" -->

## 🤖 Copilot Moment

### Add `GetRecentSessionsAsync` to ConversationStore

Start typing the method signature → Copilot completes the LINQ query

```csharp
public async Task<List<ChatSession>> GetRecentSessionsAsync(int count = 10)
```

Note: Watch how Copilot infers OrderByDescending + Take from just the method name and parameter. Descriptive names are prompts for AI.

---

<!-- .slide: class="section-divider" -->

# Stage 3

## Gateway + SignalR + Blazor

⏱️ 15 minutes

Note: Final stage — how all the pieces connect to deliver a real-time chat experience.

---

<!-- .slide: class="content-slide" -->

## Gateway REST API

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/chat/` | Send a chat message |
| `GET` | `/api/sessions/` | List all sessions |
| `POST` | `/api/sessions/` | Create new session |
| `GET` | `/api/sessions/{id}` | Get session + messages |
| `DELETE` | `/api/sessions/{id}` | Delete session |
| `PATCH` | `/api/sessions/{id}/title` | Update title |
| `GET` | `/api/sessions/{id}/messages` | Get message history |

<small>Minimal APIs — static extension classes, no controllers</small>

Note: Seven endpoints cover the complete chat lifecycle. Minimal APIs are faster than controllers and the code reads top-to-bottom.

---

<!-- .slide: class="code-slide" -->

## Gateway DI Setup

```csharp
// Program.cs — Full composition root
builder.AddServiceDefaults();                    // Aspire telemetry
builder.Services.AddSignalR();                   // Real-time hub
builder.Services.AddOpenClawStorage();           // EF Core + SQLite
builder.Services.AddModelClient<OllamaModelClient>(); // LLM provider
builder.Services.AddAgentRuntime();              // Orchestrator
builder.Services.AddHostedService<JobSchedulerService>();
```

<br>

**Every service registered in one place, one file.**

Note: This is the composition root. Every dependency is explicit and visible. No hidden magic, no convention-based discovery.

---

<!-- .slide: class="two-column" -->

## SignalR: Push vs Poll

<div class="col">

### ❌ Polling

```
Client: Any new tokens?  → No
Client: Any new tokens?  → No
Client: Any new tokens?  → Yes! "Hello"
Client: Any new tokens?  → Yes! " world"
```

Wasteful. Latency = poll interval.

</div>

<div class="col">

### ✅ SignalR Push

```
Server: "Hello"  → Client
Server: " world" → Client
Server: "!"      → Client
Server: [done]   → Client
```

Instant. Zero wasted requests.

</div>

Note: SignalR uses WebSocket when available. Each token pushes to the client the instant it's generated. This is what makes the "typing" effect work.

---

<!-- .slide: class="code-slide" -->

## `ChatHub.StreamChat`

```csharp
public sealed class ChatHub : Hub
{
    public async IAsyncEnumerable<ChatHubMessage> StreamChat(
        Guid sessionId, string message, string? model = null)
    {
        var request = new AgentRequest
        {
            SessionId = sessionId,
            UserMessage = message,
            Model = model
        };

        await foreach (var evt in _orchestrator.StreamAsync(request))
        {
            yield return evt.Type switch
            {
                AgentStreamEventType.ContentDelta =>
                    new ChatHubMessage("content", evt.Content ?? ""),
                AgentStreamEventType.Complete =>
                    new ChatHubMessage("complete", evt.Content ?? ""),
                AgentStreamEventType.Error =>
                    new ChatHubMessage("error", evt.Content ?? ""),
                _ => new ChatHubMessage("unknown", "")
            };
        }
    }
}
```

Note: The hub is thin — it delegates to the orchestrator and maps events to client messages. Pattern matching makes the mapping explicit. IAsyncEnumerable enables server-to-client streaming.

---

<!-- .slide: class="code-slide" -->

## Aspire Orchestration

```csharp
// AppHost.cs — 18 lines, entire topology
var gateway = builder.AddProject<Projects.OpenClawNet_Gateway>("gateway")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithEnvironment("ConnectionStrings__DefaultConnection", connStr)
    .WithEnvironment("Model__Endpoint", ollamaEndpoint);

builder.AddProject<Projects.OpenClawNet_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(gateway)     // ← Service discovery
    .WaitFor(gateway)           // ← Startup ordering
    .WithEnvironment("OpenClawNet__OllamaBaseUrl", ollamaEndpoint);
```

<small>This replaces Docker Compose + startup scripts</small>

Note: WithReference gives Web a service reference to Gateway. WaitFor ensures Gateway is healthy before Web starts. One command runs everything.

---

<!-- .slide: class="demo-transition" -->

## 🎬 Live Demo

### Run the Full Stack

```bash
dotnet run --project src/OpenClawNet.AppHost
```

1. Open Aspire Dashboard → `https://localhost:15100`
2. Open Blazor UI → `http://localhost:5001`
3. Create a session → Send a message
4. Watch tokens stream in real-time! 🚀

Note: This is the payoff. Everything we've explained — the interface, the streaming, the storage, the SignalR hub — all comes together in one running application.

---

<!-- .slide: class="content-slide" -->

## What We Built ✓

- ✅ **18-project architecture** with interface-driven design <!-- .element: class="fragment" -->
- ✅ **`IModelClient`** — pluggable model providers <!-- .element: class="fragment" -->
- ✅ **Ollama streaming** — NDJSON → `IAsyncEnumerable` → real-time tokens <!-- .element: class="fragment" -->
- ✅ **EF Core storage** — 7 entities, repository pattern <!-- .element: class="fragment" -->
- ✅ **SignalR ChatHub** — push tokens to the browser instantly <!-- .element: class="fragment" -->
- ✅ **Aspire orchestration** — one command, full stack <!-- .element: class="fragment" -->

Note: We covered a lot of ground. Every layer has a clear purpose, and they all connect through well-defined interfaces.

---

<!-- .slide: class="content-slide" -->

## Next: Session 2

### Tools & Agent Workflows

> "We have a chatbot that answers questions. Next session: we give it **superpowers**."

- 🔧 Tool framework: `ITool`, `IToolRegistry`, `IToolExecutor` <!-- .element: class="fragment" -->
- 📂 Built-in tools: FileSystem, Shell, Web, Scheduler <!-- .element: class="fragment" -->
- 🔄 The agent loop: think → act → observe → repeat <!-- .element: class="fragment" -->

Note: The chatbot is smart but passive. In Session 2, it learns to DO things — read files, run commands, fetch web pages. That's the agent pattern.

---

<!-- .slide: class="content-slide" -->

## Resources

| Resource | Link |
|----------|------|
| 📦 GitHub Repo | [github.com/elbruno/openclawnet](https://github.com/elbruno/openclawnet) |
| 📖 .NET Aspire | [learn.microsoft.com/dotnet/aspire](https://learn.microsoft.com/dotnet/aspire) |
| 🦙 Ollama | [ollama.com](https://ollama.com) |
| 💬 SignalR | [learn.microsoft.com/aspnet/signalr](https://learn.microsoft.com/aspnet/core/signalr) |
| 🤖 GitHub Copilot | [github.com/features/copilot](https://github.com/features/copilot) |

Note: All links are in the session README. The repo has everything — code, docs, diagrams, and setup instructions.

---

<!-- .slide: class="closing-slide" -->

# Thank You!

## Questions?

<div class="social-links">

🐙 [github.com/elbruno](https://github.com/elbruno)
🐦 [@elaboruno](https://twitter.com/elbruno)

</div>

<div class="series-info">

**Next session:** Tools & Agent Workflows

</div>

Note: Thank you! We'll take questions now. Don't forget to star the repo and pull the code to follow along in Session 2.
