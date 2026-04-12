# Session 1: Bonus Live Demos

> ⏱️ Use these if you finish the main content early or during Q&A. Each is **3-5 minutes** and self-contained.

---

## Demo A: Switch from Ollama to Foundry Local

**Goal:** Show that `IModelClient` abstraction lets you swap providers with zero code changes.

### Steps

1. **Stop the running app** (Ctrl+C in the AppHost terminal)

2. **Open `appsettings.json`** in `src/OpenClawNet.Gateway/`:
   ```json
   "Model": {
     "Provider": "ollama",
     "Model": "llama3.2"
   }
   ```

3. **Change the provider:**
   ```json
   "Model": {
     "Provider": "foundry-local",
     "Model": "phi-4"
   }
   ```

4. **Restart the app:**
   ```bash
   dotnet run --project src/OpenClawNet.AppHost
   ```

5. **Open the Blazor UI**, send the same message as before — streaming works identically.

6. **Key talking point:** *"One config change. No code touched. That's the power of the `IModelClient` abstraction — the Strategy pattern in action."*

### Fallback
If Foundry Local isn't installed, show the `Program.cs` switch statement and explain how the DI registration changes. Then switch back to Ollama.

---

## Demo B: Aspire Dashboard Deep Dive

**Goal:** Show attendees what they get "for free" with Aspire — distributed tracing, structured logs, health checks.

### Steps

1. **Open the Aspire Dashboard** (`https://localhost:15100` or the URL shown at startup)

2. **Show the Resources tab:**
   - Point out Gateway and Web services, their status (Running), endpoints
   - *"Two lines of code in AppHost gave us service discovery, health monitoring, and this dashboard."*

3. **Send a chat message** in the Blazor UI, then switch back to the dashboard

4. **Show the Traces tab:**
   - Find the trace for your chat request
   - Expand it — show the span from Web → Gateway → Ollama/Foundry Local
   - *"Every HTTP call, every SignalR message — automatically traced. No instrumentation code needed."*

5. **Show the Structured Logs tab:**
   - Filter by `OpenClawNet.Gateway`
   - Find the log entry: `Sending chat request to Ollama: model=llama3.2`
   - *"Structured logging with scopes — in production, this goes to App Insights with one line."*

6. **Show the Metrics tab** (if available):
   - Request duration, active connections
   - *"This is production-grade observability from day one."*

### Key talking point
*"Aspire isn't just an orchestrator — it's your local production environment. Everything you see here works the same way in Azure."*

---

## Demo C: Test the Gateway API with curl

**Goal:** Show that the Gateway is a standard REST API — no UI required. Useful for integrations, testing, and understanding the request/response format.

### Steps

1. **Health check:**
   ```bash
   curl http://localhost:5000/health
   ```
   → `{"status":"healthy","timestamp":"..."}`

2. **API version:**
   ```bash
   curl http://localhost:5000/api/version
   ```
   → `{"version":"0.1.0","name":"OpenClawNet"}`

3. **Create a new session:**
   ```bash
   curl -X POST http://localhost:5000/api/sessions \
     -H "Content-Type: application/json" \
     -d '{"title":"API Test"}'
   ```
   → Returns session ID (copy it)

4. **Send a chat message** (non-streaming):
   ```bash
   curl -X POST http://localhost:5000/api/chat \
     -H "Content-Type: application/json" \
     -d '{"sessionId":"<ID>","message":"What is .NET 10?"}'
   ```
   → Returns the full response as JSON

5. **Point out the response structure:**
   - `content`, `role`, `model`, `usage` (token counts), `finishReason`
   - *"This is the exact same `ChatResponse` record we walked through in the code. The API is just a thin layer over our abstractions."*

### Key talking point
*"Your Blazor UI is one client. But this API can power a mobile app, a CLI tool, a VS Code extension — anything that speaks HTTP."*

---

## Demo D: Live SignalR WebSocket Inspection

**Goal:** Demystify the real-time streaming — show actual WebSocket frames flowing between the browser and server.

### Steps

1. **Open the Blazor UI** in Chrome/Edge

2. **Open DevTools** (F12) → **Network** tab → filter by **WS** (WebSocket)

3. **You should see the SignalR connection** — click on it

4. **Go to the Messages tab** — this shows individual WebSocket frames

5. **Send a chat message** in the UI and watch the frames appear:
   - First frame: `SendMessage` invocation (your prompt going to the server)
   - Rapid frames: `ReceiveToken` — each one is a single token from the LLM
   - Final frame: `StreamComplete` — signals the end of the response

6. **Point out the token-by-token flow:**
   - *"Each of these frames is one `yield return` from `OllamaModelClient.StreamAsync`. The Gateway reads from the LLM stream and pushes each token to the browser via SignalR."*
   - *"This is why the response appears word-by-word instead of all at once."*

7. **Compare frame sizes:**
   - Each token frame is tiny (~50-100 bytes)
   - *"WebSocket overhead is minimal — this is far more efficient than polling."*

### Key talking point
*"This is the same pattern ChatGPT uses. Server-sent tokens over a persistent connection. We just built it with SignalR instead of SSE."*

---

## Suggested Order

If you have **5 extra minutes:** Do Demo A (Provider Switch) — it's the strongest architectural point.

If you have **10 extra minutes:** Do Demo A + Demo B (Aspire Dashboard).

If you have **15+ extra minutes:** Do all four in order A → B → C → D.
