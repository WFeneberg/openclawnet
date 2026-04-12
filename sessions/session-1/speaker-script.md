# Session 1: Speaker Script

## Presenters

- **Bruno** (Lead): Architecture explanations, code walkthroughs, Copilot demos
- **Co-presenter**: Live demos, audience engagement, chat monitoring, Q&A facilitation

## Timeline

| Time | Min | Who | Activity | Notes |
|------|-----|-----|----------|-------|
| 0:00 | 2 | Bruno | Welcome + series overview | Share repo link in chat: `github.com/elbruno/openclawnet` |
| 0:02 | 2 | Bruno | Introduce OpenClawNet — what it is | "18 projects, 4200 LOC, already built — we're here to understand it" |
| 0:04 | 1 | Co-presenter | Verify audience prerequisites | Quick poll: .NET 10? Ollama installed? VS Code + Copilot? |
| | | | **STAGE 1: Architecture & Core Abstractions** | |
| 0:05 | 3 | Bruno | Show architecture overview | Display `architecture-overview.svg` — explain the layer diagram |
| 0:08 | 2 | Bruno | Solution structure walkthrough | Display `solution-structure.svg` — 18 projects by responsibility |
| 0:10 | 3 | Bruno | IModelClient interface deep-dive | Open `IModelClient.cs` — explain each method's purpose |
| 0:13 | 2 | Bruno | ChatRequest/ChatResponse records | Show immutable record pattern, explain why records > classes |
| 0:15 | 2 | Bruno | DI wiring in Gateway Program.cs | Show `AddModelClient<OllamaModelClient>()` registration |
| 0:17 | 3 | Bruno | 🤖 **Copilot Moment #1**: XML docs | Open Copilot Chat → add XML docs to IModelClient |
| | | | **STAGE 2: Ollama Provider + Data Layer** | |
| 0:20 | 2 | Bruno | What is Ollama? | Local LLM, no cloud, no keys, REST API at localhost:11434 |
| 0:22 | 3 | Bruno | OllamaModelClient.StreamAsync | Walk through NDJSON streaming + yield return pattern |
| 0:25 | 2 | Bruno | IAsyncEnumerable explained | "Each token flows to the caller as it arrives — no buffering" |
| 0:27 | 2 | Co-presenter | Quick Ollama verification | Terminal: `ollama list`, `curl http://localhost:11434/api/tags` |
| 0:29 | 3 | Bruno | Storage entities overview | Display `entity-relationship.svg` — 7 entities explained |
| 0:32 | 2 | Bruno | ConversationStore repository | Show CRUD methods, explain repository pattern |
| 0:34 | 3 | Bruno | 🤖 **Copilot Moment #2**: Repo method | Type `GetRecentSessionsAsync` signature → Copilot completes LINQ |
| | | | **STAGE 3: Gateway + SignalR + Blazor** | |
| 0:37 | 2 | Bruno | Minimal API endpoint map | Show the 7 REST endpoints, explain static extension pattern |
| 0:39 | 3 | Bruno | ChatHub.SendMessage deep-dive | Walk through SignalR streaming + pattern matching on event types |
| 0:42 | 2 | Bruno | SignalR: push vs polling | "Why WebSocket? Because every millisecond matters for UX" |
| 0:44 | 2 | Bruno | AppHost orchestration | Show 18 lines of Aspire topology — `WithReference`, `WaitFor` |
| 0:46 | 4 | Co-presenter | 🎬 **Live Demo**: Full stack | Run AppHost → Aspire Dashboard → Blazor UI → send chat message |
| | | | **CLOSING** | |
| 0:50 | 2 | Bruno | Recap + end-to-end flow | Walk the data path: UI → SignalR → Orchestrator → Ollama → back |
| 0:52 | 2 | Bruno | Session 2 preview | "Next: give the chatbot superpowers with tools and the agent loop" |
| 0:54 | 1 | Co-presenter | Resources + links | Share: repo, Aspire docs, Ollama docs in chat |
| 0:55 | 5 | Both | Q&A | Bruno answers technical, co-presenter manages chat queue |

> **Total: ~60 minutes** (50 min content + 5 min buffer + 5 min Q&A)

---

## Demo Checkpoints

### Checkpoint 1: Solution Overview (minute 5–8)

**What should be working:**
- VS Code open with the full solution
- Solution Explorer showing all 18 projects
- `architecture-overview.svg` and `solution-structure.svg` ready to display

**Fallback:** If VS Code is slow to load, use the pre-rendered SVG diagrams directly in the slides.

### Checkpoint 2: Copilot Moment #1 — XML Docs (minute 17)

**What should be working:**
- `IModelClient.cs` is open in the editor
- GitHub Copilot extension is active and authenticated
- Copilot Chat panel is accessible (Ctrl+Shift+I)

**Steps:**
1. Select the entire `IModelClient` interface
2. Open Copilot Chat
3. Type: "Add XML documentation comments to all methods in this interface"
4. Review the generated docs — point out how Copilot understands the semantics

**Fallback:** If Copilot is unresponsive, show pre-written XML docs from the slides and explain what Copilot would generate.

### Checkpoint 3: Ollama Verification (minute 27)

**What should be working:**
- Ollama is running (`ollama serve` in background)
- `llama3.2` model is pulled
- HTTP endpoint responds at `http://localhost:11434`

**Steps:**
1. Run `ollama list` — should show `llama3.2`
2. Run `curl http://localhost:11434/api/tags` — should return JSON with model list

**Fallback:** If Ollama is not running, start it with `ollama serve` and wait 10 seconds. If the model isn't pulled, use `ollama pull llama3.2` (takes ~2 min for 2GB model).

### Checkpoint 4: Copilot Moment #2 — Repo Method (minute 34)

**What should be working:**
- `ConversationStore.cs` is open in the editor
- Cursor is positioned after the last method in the class

**Steps:**
1. Start typing: `public async Task<List<ChatSession>> GetRecentSessionsAsync(int count = 10)`
2. Pause after the opening `{`
3. Wait for Copilot inline suggestion (grey text)
4. Press Tab to accept
5. Point out: Copilot chose `OrderByDescending(s => s.UpdatedAt)` + `Take(count)` — exactly right

**Fallback:** If inline completion doesn't trigger, use Copilot Chat: "Implement the GetRecentSessionsAsync method that returns the most recent chat sessions ordered by UpdatedAt".

### Checkpoint 5: Full Stack Demo (minute 46)

**What should be working:**
- All prerequisites installed and verified
- No other processes on ports 5000, 5001, 15100

**Steps:**
1. Terminal: `dotnet run --project src/OpenClawNet.AppHost`
2. Wait for "Application started" message (~10 seconds)
3. Open Aspire Dashboard at `https://localhost:15100`
   - Show the service topology (Gateway + Web)
   - Point out health check status (green)
4. Open Blazor UI at `http://localhost:5001`
5. Click "New Chat"
6. Type: "What is .NET Aspire and why should I use it?"
7. Watch tokens stream in — point out the real-time "typing" effect
8. Open browser DevTools → Network → filter "ws" → show SignalR WebSocket frames

**Fallback options:**
- If AppHost fails to start: run Gateway and Web separately with `dotnet run --project src/OpenClawNet.Gateway` and `dotnet run --project src/OpenClawNet.Web`
- If Ollama is slow: have a pre-recorded GIF/video of the streaming demo ready
- If ports are busy: kill existing processes or use `--urls http://localhost:5100` override

---

## Key Talking Points

### Stage 1: Architecture

- "OpenClawNet has 18 projects but only ~4,200 lines of code. Each project does one thing well."
- "The `IModelClient` interface is the most important design decision in the entire platform. It means you can swap Ollama for Azure OpenAI or any future provider without changing a single line of business logic."
- "Records are immutable by default — once you create a `ChatRequest`, it can't change. This makes the code predictable and thread-safe."
- "Notice every async method takes a `CancellationToken`. This isn't academic — when a user navigates away mid-stream, cancellation stops the work immediately."

### Stage 2: Ollama + Storage

- "Ollama runs locally — your data never leaves your machine. This is huge for enterprise scenarios and for learning without API costs."
- "NDJSON streaming is simpler than SSE — each line is a complete JSON object. No event types, no retry logic, just read lines."
- "`IAsyncEnumerable` is the .NET primitive for streaming data. Combined with `yield return`, it creates a natural pipeline: HTTP stream → parse → yield → SignalR → browser."
- "Seven entities might seem like a lot for a chat app, but each one earns its place: sessions, messages, summaries, tool calls, jobs, job runs, and provider settings."

### Stage 3: Gateway + UI

- "Minimal APIs aren't just shorter than controllers — they're faster. No model binding overhead, no filter pipeline unless you need it."
- "SignalR chooses the best transport automatically: WebSocket if available, then Server-Sent Events, then Long Polling. You write the same code regardless."
- "This 18-line AppHost replaces what would be a Docker Compose file, environment variable scripts, and a startup checklist. One command, everything runs."
- "Open the Aspire Dashboard — this is your production observability for free. Traces, logs, metrics, health checks — all from that one `AddServiceDefaults()` call."

### Closing

- "Let's trace the full path one more time: Your message goes from the browser via WebSocket to the ChatHub, which calls the orchestrator, which calls Ollama, which generates tokens that flow back through every layer in real-time."
- "Everything we showed today — the interface, the streaming, the storage, the orchestration — these are the foundation. In Session 2, we'll add tools and the agent loop, and suddenly this chatbot can *do things* in the real world."
