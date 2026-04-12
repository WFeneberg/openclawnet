# OpenClawNet Architecture Overview

## What is OpenClawNet?

OpenClawNet is a free, open-source agent platform built with .NET 10. It provides a local-first AI assistant with optional cloud provider support, designed as both a real working application and a teaching asset for a multi-session live series.

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│              Aspire Orchestrator (AppHost)              │
│    (Service discovery, health checks, local dashboard)       │
└──────────────┬────────────────────────────┬─────────────────┘
               │                            │
        ┌──────▼────────┐          ┌────────▼───────┐
        │ Gateway API   │          │  Blazor Web    │
        │ (Minimal APIs │◀────────▶│  UI            │
        │  + SignalR)   │  WS/HTTP │                │
        └──────┬────────┘          └────────────────┘
               │
        ┌──────▼─────────────┐
        │  Agent Orchestrator │
        │  (Runs: Prompt      │
        │   Composition,      │
        │   Tool Calling,     │
        │   Memory Mgmt)      │
        └──┬────┬────┬───────┘
           │    │    │
    ┌──────┘    │    └──────┐
    ▼           ▼           ▼
┌─────────┐ ┌──────────┐ ┌────────────┐
│ Model   │ │  Tools   │ │  Skills +  │
│Provider │ │Framework │ │  Memory    │
├─────────┤ ├──────────┤ ├────────────┤
│ Ollama  │ │FileSystem│ │  Markdown  │
│ Azure OAI│ │ Shell    │ │ Skills +   │
│ Foundry │ │ Web      │ │ Embeddings │
└────┬────┘ │ Scheduler│ │(Ollama)    │
     │      └──────────┘ └────┬───────┘
     │                        │
     └────────┬───────────────┘
              ▼
        ┌────────────────┐
        │ SQLite Storage │
        │   (EF Core)    │
        └────────────────┘
```

## Key Principles

1. **Local-first**: Runs fully offline with Ollama. No cloud required.
2. **Pluggable providers**: Swap between Ollama, Azure OpenAI, and Foundry via DI configuration.
3. **Interface-driven**: Clean abstractions at every boundary — no vendor lock-in.
4. **Aspire-orchestrated**: Aspire manages service startup, health checks, discovery, and observability (dashboard visible at startup).
5. **Educational**: Code is structured for teaching, not just shipping. 4-session incremental progression.
6. **Modular**: 20 focused projects, each with a single responsibility.

## Project Structure

| Project | Purpose |
|---------|---------|
| `OpenClawNet.AppHost` | Aspire orchestration host |
| `OpenClawNet.ServiceDefaults` | Aspire service defaults (telemetry, health) |
| `OpenClawNet.Gateway` | Backend API, SignalR hub, job scheduler |
| `OpenClawNet.Agent` | Agent orchestration, prompt composition, summarization |
| `OpenClawNet.Models.Abstractions` | IModelClient, ChatMessage, ToolDefinition |
| `OpenClawNet.Models.Ollama` | Ollama REST API provider |
| `OpenClawNet.Models.AzureOpenAI` | Azure OpenAI SDK provider |
| `OpenClawNet.Models.Foundry` | Foundry OpenAI-compatible provider |
| `OpenClawNet.Tools.Abstractions` | ITool, IToolRegistry, IToolExecutor |
| `OpenClawNet.Tools.Core` | Tool registry and executor |
| `OpenClawNet.Tools.FileSystem` | File read/write/list tool |
| `OpenClawNet.Tools.Shell` | Safe shell execution tool |
| `OpenClawNet.Tools.Web` | HTTP fetch tool |
| `OpenClawNet.Tools.Scheduler` | Job scheduling tool |
| `OpenClawNet.Skills` | Markdown skill parser and loader |
| `OpenClawNet.Memory` | Session summary, conversation memory, local embeddings |
| `OpenClawNet.Storage` | EF Core + SQLite persistence |
| `OpenClawNet.Web` | Blazor Web App UI |
| `OpenClawNet.UnitTests` | xUnit tests |
| `OpenClawNet.IntegrationTests` | Integration tests |

## Orchestration: Aspire

**AppHost** (`OpenClawNet.AppHost`) is the single source of truth for service orchestration:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var gateway = builder
    .AddProject<Projects.OpenClawNet_Gateway>("gateway")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.OpenClawNet_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();
```

**Benefits:**

- Service discovery is declarative and code-first
- Health checks are built-in (automatic retry/restart)
- Aspire Dashboard gives visibility into both services at startup
- Local development just works: `dotnet run --project AppHost`
- Same orchestration code works locally and in cloud deployment

## Technology Stack

- **.NET 10** / C# 14
- **ASP.NET Core Minimal APIs** — Gateway backend
- **Blazor Web App** — Interactive server-rendered UI
- **SignalR** — Real-time streaming
- **Aspire** — Service orchestration and observability
- **Entity Framework Core** — SQLite persistence
- **xUnit** — Testing
- **Microsoft.Agents.Core** — Internal agent runtime foundation (future evolution)
