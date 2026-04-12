# Agent Runtime Flow

## Initialization Flow (Aspire Orchestration)

```
User: dotnet run --project AppHost
    │
    ▼
AppHost.cs (DistributedApplication builder)
    │
    ├── Configure Gateway service
    │   ├── Health check endpoint: /health
    │   └── External HTTP endpoints enabled
    │
    ├── Configure Web service
    │   ├── Health check endpoint: /health
    │   ├── WaitFor(Gateway)
    │   └── External HTTP endpoints enabled
    │
    ├── Register all services in DI (Gateway Program.cs)
    │   ├── Storage (SQLite + EF Core)
    │   ├── Model provider (Ollama by default)
    │   ├── Agent runtime components
    │   ├── Tool framework + individual tools
    │   ├── Skills loader
    │   ├── Memory + embeddings services
    │   └── SignalR hub for real-time streaming
    │
    └── Start both services in parallel
        │
        ▼
    Aspire Dashboard available at https://localhost:15100
```

```
User Message
    │
    ▼
Gateway API (POST /api/chat)
    │
    ▼
AgentOrchestrator.ProcessAsync()  [Public boundary - stable API]
    │
    └──▶ IAgentRuntime.ExecuteAsync()  [Internal runtime abstraction]
           │
           ├── Store user message in DB
           ├── Load conversation history
           ├── Check if summarization needed (>20 messages)
           │
           ▼
           PromptComposer.ComposeAsync()
           │
           ├── System prompt
           ├── Active skills injection
           ├── Session summary (if available)
           └── Conversation history + user message
           │
           ▼
           ModelClient.CompleteAsync()
           │
           ├── If response has tool_calls ──▶ Tool Loop
           │       │
           │       ├── ToolExecutor.ExecuteAsync()
           │       ├── Append tool results to messages
           │       └── Call model again (up to 10 iterations)
           │
           └── Final response (no tool calls)
                   │
                   ├── Store assistant message in DB
                   └── Return to IAgentOrchestrator
    │
    └──▶ Return to Gateway → UI
```

## Streaming Flow

For real-time token streaming via SignalR:

```
Chat.razor (Blazor) ──SignalR──▶ ChatHub.StreamChat()
                                     │
                                     ▼
                              AgentOrchestrator.StreamAsync()
                                     │
                                     ▼
                              ModelClient.StreamAsync()
                                     │
                         ┌───────────┼──────────┐
                         ▼           ▼          ▼
                   ContentDelta  ToolStart  Complete
                         │           │          │
                         ▼           ▼          ▼
              IAsyncEnumerable<ChatHubMessage>
```

## Scheduler Flow

```
JobSchedulerService (BackgroundService)
    │
    ├── Poll every 30 seconds
    ├── Query: active jobs where NextRunAt <= now
    │
    ▼
    For each due job:
    ├── Create JobRun record (status: running)
    ├── Create dedicated chat session
    ├── Execute via AgentOrchestrator
    ├── Update JobRun (completed/failed)
    └── Update job NextRunAt (if recurring)
```
