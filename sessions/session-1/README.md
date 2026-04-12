# Session 1: Foundation + Local Chat

**Duration:** 50 minutes | **Level:** Intermediate .NET | **Series:** OpenClawNet — Microsoft Reactor

## Goal

Build a working AI chatbot with .NET Aspire, Ollama, and SignalR streaming — then understand every layer of the architecture that makes it work.

## Prerequisites

| Tool | Version | Install |
|------|---------|---------|
| .NET SDK | 10.0+ | [dot.net/download](https://dot.net/download) |
| Ollama | Latest | [ollama.com](https://ollama.com) |
| VS Code | Latest | [code.visualstudio.com](https://code.visualstudio.com) |
| GitHub Copilot | Active subscription | VS Code extension |

**Pre-session setup:**

```bash
ollama pull llama3.2
dotnet workload install aspire
```

## What You'll Learn

1. **Architecture & Core Abstractions** — How 18 projects are organized into clean vertical slices, and why `IModelClient` is the contract that makes everything pluggable
2. **Ollama Provider + Data Layer** — How SSE streaming works with `IAsyncEnumerable`, and how EF Core entities store conversations
3. **Gateway + SignalR + Blazor** — How Minimal APIs, real-time push, and Aspire orchestration come together to deliver a complete chat experience

## Session Materials

| Resource | Description |
|----------|-------------|
| [slides.md](slides.md) | Reveal.js presentation slides |
| [speaker-script.md](speaker-script.md) | Minute-by-minute dual-presenter timeline |
| [copilot-prompts.md](copilot-prompts.md) | Copilot moments used during the session |
| [Session Guide](../../../openclawnet-plan/docs/sessions/session-1-guide.md) | Detailed walkthrough guide |
| [Guía en Español](../../../openclawnet-plan/docs/sessions/session-1-guide-es.md) | Spanish translation |

## Projects Covered

| Project | LOC | Description |
|---------|-----|-------------|
| `OpenClawNet.Models.Abstractions` | 93 | `IModelClient` interface, `ChatRequest`, `ChatResponse`, `ChatMessage` records |
| `OpenClawNet.Models.Ollama` | 181 | Ollama provider with NDJSON streaming via `IAsyncEnumerable` |
| `OpenClawNet.Storage` | 275 | EF Core DbContext, 7 entities (ChatSession, ChatMessageEntity, SessionSummary, etc.) |
| `OpenClawNet.ServiceDefaults` | 105 | Aspire service defaults — telemetry, health checks, OpenAPI |
| `OpenClawNet.AppHost` | 18 | Aspire orchestration — wires Gateway + Web with service discovery |
| `OpenClawNet.Gateway` | 611 | Minimal APIs, SignalR `ChatHub`, `JobSchedulerService` |
| `OpenClawNet.Web` | 22 | Blazor web app — chat UI with real-time streaming |

## Quick Start

```bash
# Clone and build
git clone https://github.com/elbruno/openclawnet.git
cd openclawnet
dotnet build

# Verify Ollama is running
ollama list   # Should show llama3.2

# Launch the full stack
dotnet run --project src/OpenClawNet.AppHost
```

**Expected endpoints:**

- 🌐 Web UI: `http://localhost:5001`
- 🔌 Gateway API: `http://localhost:5000`
- 📊 Aspire Dashboard: `https://localhost:15100`

## Architecture at a Glance

```
┌─────────────────────────────────────────────────┐
│                  Blazor Web UI                   │
│              (OpenClawNet.Web)                   │
├─────────────────────────────────────────────────┤
│            SignalR Hub + REST API                │
│           (OpenClawNet.Gateway)                  │
├──────────────┬──────────────┬───────────────────┤
│  IModelClient│   Storage    │  ServiceDefaults   │
│   (Ollama)   │  (EF Core)  │    (Aspire)        │
└──────────────┴──────────────┴───────────────────┘
```

## Next Session

**Session 2: Tools & Agent Workflows** — Give the chatbot superpowers with file system access, web fetching, shell execution, and the agent tool-call loop.
