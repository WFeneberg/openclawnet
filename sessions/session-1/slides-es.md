---
marp: true
title: "OpenClawNet — Sesión 1: Fundamentos + Chat Local"
theme: default
paginate: true
size: 16:9
---

<!-- _class: lead -->

# OpenClawNet
## Sesión 1 — Fundamentos y Chat Local

**Serie Microsoft Reactor · 75–90 min · .NET Intermedio**

<div class="speakers">

**Bruno Capuano** — Principal Cloud Advocate, Microsoft
[github.com/elbruno](https://github.com/elbruno) · [@elbruno](https://twitter.com/elbruno)

**Pablo Piovano** — Microsoft MVP
[linkedin.com/in/ppiova](https://www.linkedin.com/in/ppiova/)

</div>

---

## Por qué existe esta serie

- Construir una app .NET **agéntica** real, no un chatbot de juguete
- 100% **código abierto**, ejecuta **localmente** por defecto
- Cinco sesiones, cuatro entregadas en vivo, una bonus
- Cada línea de código está en [`elbruno/openclawnet`](https://github.com/elbruno/openclawnet)

> Hoy: chatear con un modelo. Próxima vez: darle herramientas. Luego jobs, MCP, multi-agente.

---

## Qué vas a tener al final de la sesión 1

Una app distribuida **Aspire** funcional con:

- 🧠 Un proveedor de modelos conectable (`IAgentProvider`)
- 🌐 Una UI de chat Blazor con streaming de tokens vía **HTTP NDJSON**
- 💾 Persistencia con EF Core (SQLite) para conversaciones
- 🔌 5 proveedores conectados: **Ollama, Azure OpenAI, Foundry Local, Microsoft Foundry, GitHub Copilot SDK**
- 📊 Dashboard de Aspire con logs, métricas, trazas

---

## Prerequisitos (resumen)

| Herramienta | Versión | Notas |
|------|---------|-------|
| .NET SDK | 10.0+ | `dot.net/download` |
| Aspire workload | latest | `dotnet workload install aspire` |
| Ollama (o Foundry Local) | latest | `ollama pull llama3.2` |
| VS Code / Visual Studio | current | + GitHub Copilot |

> Hardware: **16 GB RAM mínimo** para LLMs locales. 32 GB recomendados.

---

# 🏗️  Etapa 1 — Arquitectura

---

## 27 proyectos, 4 capas

```
┌──────────────────────────────────────────────┐
│           Blazor Web (chat UI)               │
├──────────────────────────────────────────────┤
│   HTTP NDJSON + Minimal APIs (Gateway)       │
├──────────────────────────────────────────────┤
│       RuntimeAgentProvider (router)          │
├────────┬────────┬────────┬────────┬──────────┤
│ Ollama │ Azure  │Foundry │Foundry │ GitHub   │
│        │ OpenAI │        │ Local  │ Copilot  │
├────────┴────────┴────────┴────────┴──────────┤
│        Storage (EF Core, SQLite)             │
└──────────────────────────────────────────────┘
```

---

## El contrato: `IAgentProvider`

```csharp
public interface IAgentProvider
{
    string Name { get; }
    IChatClient CreateChatClient(AgentProfile profile);
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
}
```

- `IChatClient` es el estándar de **Microsoft.Extensions.AI**
- Cada proveedor en la caja implementa una interfaz
- Cambia proveedores en **una línea de DI** — sin cambios en código de la app

---

## Diseño de proyecto vertical-slice

| Slice | Proyecto | LOC |
|-------|---------|-----|
| Abstractions | `Models.Abstractions` | 93 |
| Provider | `Models.Ollama` | 181 |
| Provider | `Models.AzureOpenAI` | 185 |
| Provider | `Models.GitHubCopilot` | 142 |
| Storage | `Storage` | 275 |
| Gateway | `Gateway` | 625 |
| UI | `Web` | 28 |
| Aspire | `AppHost` | 18 |

---

# 🔌  Etapa 2 — Proveedores

---

## Ollama en 8 líneas

```csharp
services.Configure<OllamaOptions>(o =>
{
    o.Endpoint = "http://localhost:11434";
    o.Model    = "llama3.2";
});
services.AddSingleton<IAgentProvider, OllamaAgentProvider>();

var provider = sp.GetRequiredService<IAgentProvider>();
var client   = provider.CreateChatClient(profile);
await foreach (var update in client.GetStreamingResponseAsync(messages))
    Console.Write(update.Text);
```

---

## Azure OpenAI — 3 modos de autenticación

| Modo | Cuándo usar |
|------|-------------|
| **API Key** | Desarrollo local, demos, secretos de CI |
| **Integrated** | Hospedado en Azure con managed identity |
| **Federated** | GitHub Actions OIDC → Azure |

El proveedor elige la credencial correcta basándose en `AzureOpenAIOptions.AuthMode`.

---

## Proveedor GitHub Copilot SDK

```csharp
services.Configure<GitHubCopilotOptions>(o =>
{
    o.Model = "gpt-5-mini"; // or claude-sonnet-4.5, gpt-5, ...
});
services.AddSingleton<IAgentProvider, GitHubCopilotAgentProvider>();
```

Auth: `gh auth login` (usa configuración del host) o `GitHubCopilot:GitHubToken` user-secret.
Requiere una **suscripción activa de GitHub Copilot** (existe tier gratuito).

---

# 🌐  Etapa 3 — Gateway + Streaming

---

## HTTP NDJSON, no SignalR

Migramos de `ChatHub` (SignalR) a **POST /api/chat/stream** retornando `application/x-ndjson`.

¿Por qué?
- Código de cliente más simple (`HttpClient` + lector de líneas)
- Funciona detrás de cualquier reverse proxy
- Sin sticky sessions
- Un round-trip por turno

---

## El endpoint de streaming

```csharp
group.MapPost("/api/chat/stream", async (
    ChatStreamRequest req, IAgentRuntime runtime, HttpContext ctx) =>
{
    ctx.Response.ContentType = "application/x-ndjson";
    await foreach (var ev in runtime.ExecuteStreamAsync(ctx))
    {
        var line = JsonSerializer.Serialize(ev) + "\n";
        await ctx.Response.WriteAsync(line);
        await ctx.Response.Body.FlushAsync();
    }
});
```

---

## Consumidor Blazor

```csharp
using var resp = await Http.PostAsJsonAsync(
    "/api/chat/stream", request,
    HttpCompletionOption.ResponseHeadersRead);

using var stream = await resp.Content.ReadAsStreamAsync();
using var reader = new StreamReader(stream);

while (!reader.EndOfStream)
{
    var line = await reader.ReadLineAsync();
    var ev   = JsonSerializer.Deserialize<StreamEvent>(line!);
    AppendToken(ev.Delta);     // re-renders Blazor cell
    StateHasChanged();
}
```

---

# 💾  Etapa 4 — Almacenamiento

---

## Entidades de EF Core (seleccionadas)

| Entidad | Propósito |
|--------|---------|
| `ChatSession` | Un hilo de conversación |
| `ChatMessageEntity` | Cada turno de usuario/asistente (FK → session) |
| `AgentProfile` | Bundle nombrado: provider + modelo + instrucciones |
| `ScheduledJob` | Job recurrente/único (sesión 3) |
| `JobRun` + `JobRunEvent` | Timeline de ejecución persistida |

---

## Migración de esquema sin EF migrations

Usamos `EnsureCreatedAsync` + un `SchemaMigrator` escrito a mano:

```csharp
await db.Database.EnsureCreatedAsync();
await SchemaMigrator.UpgradeAsync(db);
```

Razones:
- Un archivo SQLite, no necesita historial completo de migraciones
- Agrega tablas/columnas nuevas de forma idempotente
- Hace que "borrar el .db y empezar de nuevo" sea una historia de recuperación válida

---

# 🚀  Etapa 5 — Ejecutarlo

---

## `aspire start` y estás chateando

```pwsh
$env:NUGET_PACKAGES = "$env:USERPROFILE\.nuget\packages2"
aspire start src\OpenClawNet.AppHost
```

Luego:
- 📊 Dashboard de Aspire → http://localhost:15100
- 🌐 Web UI → http://localhost:5010
- 🔌 Gateway → http://localhost:5000

---

## Resumen de demos

1. **Demo 1** — Consola: cambiar proveedores en 1 línea (`code/demo1`)
2. **Demo 2** — "Inyección de bugs" con Copilot: explicar y arreglar
3. **Demo 3** — Agregar un agente con personalidad custom (pirata / chef / robot)

---

# 🎯  Hacia dónde vamos

- **Sesión 2** — Tools: sistema de archivos, shell, web, imagen, audio, scheduler
- **Sesión 3** — Jobs de larga duración + timeline de eventos de ejecución
- **Sesión 4** — Servidores MCP (in-process + remotos)
- **Bonus** — Orquestación multi-agente

---

<!-- _class: lead -->

# ¿Preguntas?

<div class="speakers">

**Bruno Capuano** — Principal Cloud Advocate, Microsoft
[github.com/elbruno](https://github.com/elbruno) · [@elbruno](https://twitter.com/elbruno)

**Pablo Piovano** — Microsoft MVP
[linkedin.com/in/ppiova](https://www.linkedin.com/in/ppiova/)

</div>

<br>

`elbruno/openclawnet` · MIT licensed · contributions welcome

