# Session 1: Copilot Prompts

These are the 2 Copilot completions used during the live session. Each is a small, focused moment that demonstrates Copilot's ability to understand .NET patterns and generate correct code from context.

> **Tip for presenters:** Practice each prompt 2–3 times before the session. Copilot output can vary slightly — know what "good enough" looks like and be ready to explain the result.

---

## Prompt 1: XML Documentation (Stage 1, ~minute 17)

### Setup

- **File open:** `src/OpenClawNet.Models.Abstractions/IModelClient.cs`
- **Mode:** Copilot Chat (Ctrl+Shift+I)
- **Copilot context:** The full `IModelClient` interface is visible in the editor

### Prompt

```
Add XML documentation comments to all methods in this interface
```

### Expected Result

```csharp
public interface IModelClient
{
    /// <summary>
    /// Gets the name of the model provider (e.g., "ollama", "azure-openai").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Sends a chat request and returns the complete response.
    /// </summary>
    /// <param name="request">The chat request containing messages and configuration.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The complete chat response from the model.</returns>
    Task<ChatResponse> CompleteAsync(ChatRequest request, CancellationToken ct = default);

    /// <summary>
    /// Sends a chat request and streams response chunks as they are generated.
    /// </summary>
    /// <param name="request">The chat request containing messages and configuration.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>An async enumerable of response chunks for real-time streaming.</returns>
    IAsyncEnumerable<ChatResponseChunk> StreamAsync(ChatRequest request, CancellationToken ct = default);

    /// <summary>
    /// Checks whether the model provider is available and responding.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>True if the provider is available; otherwise, false.</returns>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
}
```

### Why This Prompt

- **Demonstrates:** Copilot understanding interface contracts and generating semantically meaningful documentation — not just repeating method names
- **Teaching point:** Copilot infers purpose from naming conventions. `StreamAsync` gets docs about "real-time streaming", `IsAvailableAsync` gets docs about "provider availability" — because the names are descriptive
- **Audience takeaway:** Good naming + Copilot = instant documentation that's actually useful

### Presenter Notes

- Point out that Copilot correctly identifies `IAsyncEnumerable` as a streaming pattern
- If the output says "Checks if the model is available" instead of "provider", that's fine — the semantics are correct
- Emphasize: "This saves time, but always review. Copilot is a first draft, not a final draft."

---

## Prompt 2: New Repository Method (Stage 2, ~minute 34)

### Setup

- **File open:** `src/OpenClawNet.Storage/ConversationStore.cs`
- **Mode:** Inline completion (Tab to accept)
- **Cursor position:** After the last method in the `ConversationStore` class, on a new line

### Prompt

Start typing the method signature:

```csharp
public async Task<List<ChatSession>> GetRecentSessionsAsync(int count = 10)
{
```

Then pause and wait for Copilot's inline suggestion (grey text).

### Expected Result

```csharp
public async Task<List<ChatSession>> GetRecentSessionsAsync(int count = 10)
{
    return await _context.ChatSessions
        .OrderByDescending(s => s.UpdatedAt)
        .Take(count)
        .ToListAsync();
}
```

### Why This Prompt

- **Demonstrates:** Copilot generating data access code from a method signature alone
- **Teaching point:** The method name `GetRecentSessions` + parameter `count` + return type `List<ChatSession>` encode enough intent for Copilot to produce the correct LINQ chain: `OrderByDescending` → `Take` → `ToListAsync`
- **Audience takeaway:** Descriptive method signatures are not just for readability — they're prompts for AI-assisted development

### Presenter Notes

- If Copilot suggests `OrderByDescending(s => s.CreatedAt)` instead of `UpdatedAt`, accept it — both are reasonable. Mention that `UpdatedAt` is slightly better because it reflects the last activity
- If inline completion doesn't trigger, press Ctrl+Space or use Copilot Chat: "Implement the body of this method"
- Point out the surrounding code: Copilot knows about `_context`, `ChatSessions`, and the entity properties because they're all in scope
- This is a real pattern — the existing `ListSessionsAsync` method uses similar LINQ, and Copilot picks up on that consistency

### Acceptable Variations

All of these are correct completions:

```csharp
// Variation A: Using AsNoTracking (performance optimization)
return await _context.ChatSessions
    .AsNoTracking()
    .OrderByDescending(s => s.UpdatedAt)
    .Take(count)
    .ToListAsync();

// Variation B: Using CreatedAt instead of UpdatedAt
return await _context.ChatSessions
    .OrderByDescending(s => s.CreatedAt)
    .Take(count)
    .ToListAsync();

// Variation C: With cancellation token parameter
public async Task<List<ChatSession>> GetRecentSessionsAsync(
    int count = 10, CancellationToken cancellationToken = default)
{
    return await _context.ChatSessions
        .OrderByDescending(s => s.UpdatedAt)
        .Take(count)
        .ToListAsync(cancellationToken);
}
```

---

## Timing Summary

| # | Prompt | Stage | Minute | Duration |
|---|--------|-------|--------|----------|
| 1 | XML Documentation | Stage 1 | ~17 | 2–3 min |
| 2 | Repository Method | Stage 2 | ~34 | 2–3 min |

**Total Copilot time:** ~5 minutes out of 50 minutes session

---

## Troubleshooting

| Issue | Fix |
|-------|-----|
| Copilot not responding | Check extension status in VS Code bottom bar. Restart: Ctrl+Shift+P → "Copilot: Restart" |
| Inline suggestion not appearing | Press Ctrl+Space to trigger. Ensure cursor is inside the class body |
| Chat generates incorrect code | Accept and fix manually — use it as a teaching moment about reviewing AI output |
| Copilot generates too much | Accept the relevant part and delete the rest. Say: "Copilot is eager — we only need this piece" |
| No internet connection | Show the expected output from this document on the slides |
