<!-- .slide: class="title-slide" -->

# 🦀 OpenClaw .NET

## Sesión 1: Fundamentos + Chat Local

### Construyendo una Plataforma de Agentes IA con .NET

<div class="presenter-info">

Bruno Capuano · Pablo Piovano · Serie Microsoft Reactor

</div>

Note: ¡Bienvenidos! Este es un viaje de 5 sesiones. Todo el código ya está construido — estamos aquí para entender cada capa. Al final de hoy tendrás un stack completo de IA funcionando en tu máquina.

---

<!-- .slide: class="content-slide" -->

## Hoja de Ruta de la Serie

<div class="roadmap-timeline">

**→ Sesión 1:** Fundamentos + Chat Local  
*Arquitectura, origen de OpenClaw, LLMs Locales, Streaming HTTP*

**Sesiones 2–5 (Próximamente):**
- Tools & Flujos de Agentes
- Skills & Memoria  
- Automatización & Cloud
- Canales, Browser & Eventos

</div>

Note: 5 sesiones. Cada una se construye sobre la anterior. Hoy = los fundamentos de los que todo depende.

---

<!-- .slide: class="content-slide" -->

## ¿Qué es OpenClaw?

<div class="two-col">
<div>

OpenClaw es una plataforma de agentes de código abierto — una **arquitectura de referencia** para construir agentes IA con:

- Gateway persistente como plano de control <!-- .element: class="fragment" -->
- Runtime de agentes consciente del workspace <!-- .element: class="fragment" -->
- Tools, skills y memoria de primera clase <!-- .element: class="fragment" -->
- Abstracción de modelos multi-proveedor <!-- .element: class="fragment" -->
- Canales: Teams, WhatsApp, Telegram... <!-- .element: class="fragment" -->

</div>
<div>

**Los 9 Pilares:**

1. Gateway como Plano de Control
2. Runtime de Agentes + Workspace
3. Sesiones & Memoria
4. Tools de Primera Clase
5. Sistema de Skills
6. Abstracción de Modelos
7. Automatización (cron + webhooks)
8. Superficies de UI (Control + WebChat)
9. Canales & Nodos

</div>
</div>

Note: OpenClaw define la arquitectura. Nosotros la implementamos. openclaw.ai es la referencia de la comunidad.

---

<!-- .slide: class="content-slide" -->

## OpenClaw .NET: OpenClaw en .NET

| Concepto OpenClaw | OpenClawNet | .NET |
|------------------|-------------|------|
| Gateway como Plano de Control | `OpenClawNet.Gateway` | Minimal APIs + HTTP NDJSON + Scheduler |
| Runtime de Agentes + Workspace | `IAgentOrchestrator` + `WorkspaceLoader` | AGENTS.md / SOUL.md / USER.md |
| Sesiones & Memoria | `IMemoryService` + `ISummaryService` | EF Core + SQLite + compactación |
| Tools de Primera Clase | `ITool` + FileSystem, Shell, Web, Browser | Browser respaldado por Playwright |
| Sistema de Skills | `SkillLoader` | Markdown/YAML con precedencia |
| Abstracción de Modelos | `IAgentProvider` + `RuntimeAgentProvider` | Ollama, Azure OpenAI, Foundry, Foundry Local, GitHub Copilot |
| Automatización | `JobSchedulerService` + WebhookEndpoints | cron + triggers de GitHub |
| Superficies de UI | Blazor Web App (Control UI + WebChat) | Ambas desde un proyecto |
| Canales & Nodos | `IChannel` + adaptador Teams | Bot Framework; nodos móviles planeados |

Note: Esta es la tabla de traducción. Cada pilar de OpenClaw mapea a un tipo o proyecto .NET.

---

<!-- .slide: class="content-slide" -->

## Lo Que Construiremos Hoy — 3 Etapas

- **Etapa 1:** 🧱 Arquitectura & Abstracciones Core (IAgentProvider, AgentProfile, records, DI) <!-- .element: class="fragment" -->
- **Etapa 2:** 🤖 LLMs Locales + Workspace + Almacenamiento (Ollama, FoundryLocal, archivos bootstrap, EF Core) <!-- .element: class="fragment" -->
- **Etapa 3:** ⚡ Gateway + HTTP NDJSON + Blazor (streaming en tiempo real, Aspire, demo full stack) <!-- .element: class="fragment" -->

Note: 3 etapas, ~15 min cada una. Cada etapa es un checkpoint ejecutable.

---

<!-- .slide: class="content-slide" -->

## Verificación de Prerequisitos

```bash
dotnet --version   # 10.0.x
code --version     # VS Code
```

**LLM Local** — elige uno:

```bash
ollama list          # llama3.2
foundry model list   # phi
```

GitHub Copilot extension instalada y activa ✓

Note: Verificación rápida. .NET 10 y un LLM local son requeridos. Copilot para los momentos en vivo.

---

<!-- .slide: class="section-divider" -->

# Etapa 1

## Arquitectura & Abstracciones Core

⏱️ 15 minutos

Note: Primero el panorama general — cómo está diseñada la plataforma.

---

<!-- .slide: class="architecture-slide" -->

## Arquitectura de un Vistazo

```
┌──────────────────────────────────────────────────────────┐
│                      Blazor Web UI                        │
│                   (OpenClawNet.Web)                        │
├──────────────────────────────────────────────────────────┤
│            HTTP NDJSON + REST API                          │
│              (OpenClawNet.Gateway)                         │
├──────────────────────────────────────────────────────────┤
│              RuntimeAgentProvider (router)                 │
├────────┬──────────┬─────────┬────────────┬──────────────┤
│ Ollama │Azure AOAI│ Foundry │FoundryLocal│GitHub Copilot│
├────────┴──────────┴─────────┴────────────┴──────────────┤
│    Storage (EF Core)    │     ServiceDefaults (Aspire)    │
└─────────────────────────┴────────────────────────────────┘
```

**Arquitectura completa en la guía de la sesión — hoy nos enfocamos en las capas fundamentales**

Note: El Gateway es el centro nervioso — persistente, con estado, todo pasa a través de él. RuntimeAgentProvider enruta al proveedor activo. 6 proveedores totales incluyendo RuntimeAgentProvider.

---

<!-- .slide: class="content-slide" -->

## 27 Proyectos, Una Responsabilidad Cada Uno

| Proyecto | Capa | Propósito |
|---------|-------|---------|
| AppHost | Orquestación | Host Aspire |
| ServiceDefaults | Orquestación | Telemetría + health |
| Gateway | Gateway | APIs, streaming HTTP NDJSON, scheduler, canales |
| Web | UI | Blazor Control UI + WebChat |
| Agent | Agent | Orquestador, compositor de prompts, cargador de workspace |
| Models.Abstractions | Provider | `IAgentProvider`, `AgentProfile`, `ChatMessage`, `ToolDefinition` |
| Models.Ollama | Provider | Proveedor REST Ollama |
| Models.FoundryLocal | Provider | Proveedor Foundry en dispositivo |
| Models.AzureOpenAI | Provider | SDK Azure OpenAI |
| Models.Foundry | Provider | Proveedor Foundry cloud |
| Models.GitHubCopilot | Provider | Proveedor SDK GitHub Copilot |
| Tools.Abstractions | Tools | `ITool`, `IToolRegistry`, `IToolExecutor` |
| Tools.Core | Tools | Registry + executor |
| Tools.FileSystem | Tools | Lectura/escritura segura de archivos |
| Tools.Shell | Tools | Ejecución de comandos |
| Tools.Web | Tools | Fetch HTTP |
| Tools.Scheduler | Tools | Tool de programación de jobs |
| Tools.Browser | Tools | Browser headless Playwright |
| Skills | Skills | Parser + cargador de Markdown |
| Memory | Memory | Resumen, embeddings, búsqueda |
| Storage | Storage | EF Core + SQLite |
| Adapters.Teams | Channels | Adaptador Bot Framework |
| UnitTests + IntegrationTests | Tests | Suite de tests xUnit |

Note: Cada proyecto = una preocupación. Esto hace el código navegable y testeable. 27 proyectos totales incluyendo scheduler, shell, browser, canales y servicios de memoria.

---

<!-- .slide: class="content-slide" -->

## Los 9 Pilares en Código

- **1. Gateway** → `OpenClawNet.Gateway` (proceso persistente) <!-- .element: class="fragment" -->
- **2. Runtime de Agentes + Workspace** → `IAgentOrchestrator` + `WorkspaceLoader` <!-- .element: class="fragment" -->
- **3. Sesiones & Memoria** → `IMemoryService` + `ISummaryService` <!-- .element: class="fragment" -->
- **4. Tools** → `ITool` + FileSystem, Shell, Web, Browser, Scheduler <!-- .element: class="fragment" -->
- **5. Skills** → `SkillLoader` (workspace > local > bundle) <!-- .element: class="fragment" -->
- **6. Abstracción de Modelos** → `IAgentProvider` + `RuntimeAgentProvider` (6 proveedores) <!-- .element: class="fragment" -->
- **7. Automatización** → `JobSchedulerService` + WebhookEndpoints <!-- .element: class="fragment" -->
- **8. Superficies de UI** → Blazor (Control UI + WebChat, misma app) <!-- .element: class="fragment" -->
- **9. Canales & Nodos** → `IChannel` + Teams + concepto de nodo <!-- .element: class="fragment" -->

Note: Una slide, 9 pilares. Cada uno es un tema de sesión en la serie.

---

<!-- .slide: class="content-slide" -->

## Tools & Skills

**Tools** — capacidades de function-calling
- `file_system`, `web_search`, `shell_exec`
- Implementados como `ITool` + `ToolAIFunction` (Agent Framework)

**Agent Framework Skills** — divulgación progresiva <!-- .element: class="fragment" -->
- Especificación `SKILL.md` (agentskills.io) <!-- .element: class="fragment" -->
- Patrón Anunciar → Cargar → Ejecutar <!-- .element: class="fragment" -->
- `AgentSkillsProvider` de `Microsoft.Agents.AI` <!-- .element: class="fragment" -->

**Resultado**: El agente sabe QUÉ puede hacer (skills), CÓMO hacerlo (tools) <!-- .element: class="fragment" -->

Note: Los tools son las acciones. Los skills son el contexto. AgentSkillsProvider informa al modelo qué skills están disponibles antes de cada llamada — divulgación progresiva.

---

<!-- .slide: class="code-slide" -->

## El Contrato Clave: `IAgentProvider`

```csharp
public interface IAgentProvider
{
    string ProviderName { get; }

    IChatClient CreateChatClient(AgentProfile profile);

    Task<bool> IsAvailableAsync(
        CancellationToken ct = default);
}
```

<small>📁 `src/OpenClawNet.Models.Abstractions/IAgentProvider.cs`</small>

Note: Dos métodos. Cada proveedor implementa exactamente este contrato. CreateChatClient devuelve un IChatClient configurado para el perfil del agente — este es el punto de integración con MAF. RuntimeAgentProvider enruta al proveedor activo — cambia Ollama por Azure o GitHub Copilot en tiempo de ejecución.

---

<!-- .slide: class="architecture-slide" -->

## Dónde Estamos en el Stack

```
┌──────────────────────────────────────────────────────────┐
│                      Blazor Web UI                        │
│                   (OpenClawNet.Web)                        │
├──────────────────────────────────────────────────────────┤
│            HTTP NDJSON + REST API                          │
│              (OpenClawNet.Gateway)                         │
├──────────────────────────────────────────────────────────┤
│              RuntimeAgentProvider (router)                 │
├────────┬──────────┬─────────┬────────────┬──────────────┤
│ Ollama │Azure AOAI│ Foundry │FoundryLocal│GitHub Copilot│
└────────┴──────────┴─────────┴────────────┴──────────────┘
```

**Ahora veámoslo en vivo — cambiando proveedores de modelo en tiempo de ejecución**

Note: Esta vista compacta muestra dónde estamos. RuntimeAgentProvider enruta al proveedor activo. Ahora demostraremos la flexibilidad en tiempo de ejecución.

---

<!-- .slide: class="demo-transition" -->

## 🎬 Demo en Vivo 1: Cambio de Provider — Sin Código

**Lo que haremos:** Cambiar de Ollama local → Azure OpenAI → mismo chat, diferente backend

**Cómo:** Usar la página de Model Providers — configurar proveedor, guardar, iniciar nuevo chat

**Punto de enseñanza:** La abstracción `IAgentProvider` habilita flexibilidad en tiempo de ejecución entre 6 proveedores. Las definiciones de Model Provider ahora controlan el endpoint de chat real vía sincronización `ProviderResolver` → `RuntimeModelSettings`.

**Sin cambios de código. Sin reinicios. Solo configuración.**

Note: Este es el poder de la abstracción limpia. Misma interfaz, diferente implementación, intercambio en tiempo de ejecución. El ProviderResolver conecta definiciones de BD al runtime — lo que configuras en la UI es exactamente lo que usa el chat.

---

<!-- .slide: class="code-slide" -->

## Microsoft Agent Framework

**`AIAgent`** — la abstracción core
- `ChatClientAgent`: envuelve cualquier `IChatClient` (Ollama, Azure, Foundry)
- `AgentSkillsProvider`: agrega contexto de skills (divulgación progresiva)
- `RunAsync()` / `RunStreamingAsync()`: ejecución unificada

**En OpenClawNet**: <!-- .element: class="fragment" -->

```csharp
var agent = new ChatClientAgent(
    agentProviderAdapter, // IAgentProvider → bridge IChatClient
    new ChatClientAgentOptions {
        AIContextProviders = [skillsProvider]
    });
await foreach (var update in agent.RunStreamingAsync(messages))
    // stream a UI vía HTTP NDJSON
```
<!-- .element: class="fragment" -->

Note: Agent Framework proporciona el bucle de ejecución. Conectamos nuestro adaptador IAgentProvider y AgentSkillsProvider. El agente maneja inyección de skills, llamadas a tools y streaming — nosotros poseemos el modelo y los skills.

---

<!-- .slide: class="section-divider" -->

# Etapa 2

## LLMs Locales + Workspace + Almacenamiento

⏱️ 15 minutos

Note: ¿Por qué modelos locales? El concepto de workspace. Cómo almacenamos todo.

---

<!-- .slide: class="two-column" -->

## Opciones de LLM Local

| | Ollama | Foundry Local |
|---|--------|---------------|
| **Instalación** | CLI + pull de modelos | Paquete NuGet (configuración cero) |
| **API** | REST (compatible con OpenAI) | SDK en proceso |
| **Modelos** | Llama, Phi, Mistral... | Phi, Qwen, DeepSeek... |
| **Aceleración HW** | Config GPU manual | Auto GPU / NPU / CPU |
| **Privacidad** | Solo local | Solo local |

Ambos implementan `IAgentProvider` → **intercambia con una línea** <!-- .element: class="fragment" -->

Note: Ollama es el favorito de la comunidad. Foundry Local se envía como NuGet — auto-detecta hardware. Configuración cero para el usuario.

---

<!-- .slide: class="content-slide" -->

## Archivos Bootstrap de Workspace

El comportamiento del agente vive en **archivos markdown**, no en código:

| Archivo | Propósito |
|------|---------|
| `AGENTS.md` | Persona del agente, instrucciones, reglas de comportamiento |
| `SOUL.md` | Valores del agente, principios, barandas éticas |
| `USER.md` | Perfil de usuario, preferencias, personalización |

Cargados por `WorkspaceLoader` al inicio de sesión → inyectados en el system prompt <!-- .element: class="fragment" -->

```json
// WorkspaceLoader lee del directorio de workspace de sesión
// Precedencia: workspace/ > local/ > defaults del bundle
{
  "agentsMarkdown": "Eres un asistente .NET útil...",
  "soulMarkdown": "Siempre sé honesto. Nunca dañes a usuarios...",
  "userMarkdown": "El usuario prefiere respuestas concisas. Zona horaria: UTC-3..."
}
```

Note: No se necesitan cambios de código para cambiar el comportamiento del agente. Edita los archivos markdown. Así es como personalizas agentes para diferentes proyectos o usuarios.

---

<!-- .slide: class="architecture-slide" -->

## `IAsyncEnumerable`: El Pipeline de Streaming

```
┌─────────────────┐
│  LLM Provider   │  Ollama / Azure AOAI / Foundry / Local / Copilot
│  (genera)       │
└────────┬────────┘
         ▼ IAsyncEnumerable<ChatResponseChunk>
┌─────────────────┐
│ IAgentProvider  │  CreateChatClient → IChatClient por perfil
│  (abstrae)      │
└────────┬────────┘
         ▼ IAsyncEnumerable<AgentStreamEvent>
┌─────────────────┐
│  IAgentOrch.    │  Bucle de tools + composición de prompt
│  (orquesta)     │
└────────┬────────┘
         ▼ stream HTTP NDJSON
┌─────────────────┐
│  Blazor UI      │  Renderizado token por token
│  (renderiza)    │
└─────────────────┘
```

Note: Cada capa usa IAsyncEnumerable. Sin buffering en ningún lugar. Los tokens fluyen de arriba a abajo en tiempo real.

---

<!-- .slide: class="content-slide" -->

## Capa de Almacenamiento — 7 Entidades

| Entidad | Propósito |
|--------|---------|
| `ChatSession` | Raíz — título, proveedor, modelo, timestamps |
| `ChatMessageEntity` | Rol, contenido, llamadas a tools, índice de orden |
| `SessionSummary` | Contexto de conversación compactado |
| `ToolCallRecord` | Pista de auditoría de ejecución de tools |
| `ScheduledJob` | Definición de job cron (nombre, expresión cron) |
| `JobRun` | Historial de ejecución y resultados de jobs |
| `ProviderSetting` | Configuración por proveedor |

Note: 7 entidades. Cada una se gana su lugar. SessionSummary es cómo manejamos conversaciones largas sin explotar las ventanas de contexto.

---

<!-- .slide: class="demo-transition" -->

## 🎬 Demo en Vivo 3: Cambio de Personalidad del Agente

**Lo que haremos:** Cambiar el comportamiento del agente editando archivos de workspace — sin código

**Cómo:** Editar `AGENTS.md` → guardar → iniciar nuevo chat → observar el cambio de personalidad

**Punto de enseñanza:** Los archivos de workspace (`AGENTS.md`, `SOUL.md`, `USER.md`) controlan el comportamiento del agente sin tocar C# ni reiniciar servicios

**Personalización de comportamiento dirigida por Markdown**

Note: La arquitectura workspace-first significa cambios de comportamiento sin tocar código. Esta es personalización de agentes sin código.

---

<!-- .slide: class="section-divider" -->

# Etapa 3

## Gateway + HTTP NDJSON + Blazor

⏱️ 15 minutos

Note: Todas las piezas se conectan para chat en tiempo real.

---

<!-- .slide: class="content-slide" -->

## Gateway: El Plano de Control

- 🔌 Gestiona conexiones de canal persistentes (Teams, adaptadores futuros) <!-- .element: class="fragment" -->
- 🌐 Expone APIs REST para todas las operaciones de gestión <!-- .element: class="fragment" -->
- ⚡ Expone endpoint HTTP NDJSON para streaming de tokens en tiempo real <!-- .element: class="fragment" -->
- 🖥️ Sirve Control UI + WebChat (ambas desde una app Blazor) <!-- .element: class="fragment" -->
- 🔗 Maneja endpoints de webhook para triggers de eventos externos <!-- .element: class="fragment" -->
- ⏰ Ejecuta scheduler cron persistente para jobs automatizados <!-- .element: class="fragment" -->
- 🔥 Dispara eventos de sistema (job iniciado, mensaje recibido, sesión creada) <!-- .element: class="fragment" -->

> "Todo pasa a través del Gateway. Es el centro nervioso del sistema." <!-- .element: class="fragment" -->

Note: NO es un proxy sin estado. Un proceso persistente con estado que posee conexiones de canal, estado de jobs y enrutamiento de eventos.

---

<!-- .slide: class="code-slide" -->

## `ChatStreamEndpoints` — Tokens al Browser

```csharp
app.MapPost("/api/chat/stream", async (
    ChatStreamRequest request,
    IAgentOrchestrator orchestrator,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    httpContext.Response.ContentType = "application/x-ndjson";
    httpContext.Response.Headers["Cache-Control"] = "no-cache";

    var agentRequest = new AgentRequest
    {
        SessionId = request.SessionId,
        UserMessage = request.Message,
        Model = request.Model
    };

    await foreach (var evt in orchestrator.StreamAsync(
        agentRequest, cancellationToken))
    {
        var streamEvent = new ChatStreamEvent
        {
            Type = MapEventType(evt.Type),
            Content = evt.Content,
            SessionId = request.SessionId
        };
        var line = JsonSerializer.Serialize(streamEvent, JsonOpts);
        await httpContext.Response.WriteAsync(line + "\n", cancellationToken);
        await httpContext.Response.Body.FlushAsync(cancellationToken);
    }
});
```

Note: Endpoint de Minimal API. Devuelve NDJSON — cada línea es un evento JSON discreto. Los errores surgen como códigos de estado HTTP. Más simple y debuggeable que WebSocket.

---

<!-- .slide: class="code-slide" -->

## Aspire: 18 Líneas Reemplazan Docker Compose

```csharp
// AppHost.cs — toda la topología
var gateway = builder.AddProject<Projects.OpenClawNet_Gateway>("gateway")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithEnvironment("ConnectionStrings__DefaultConnection", connStr)
    .WithEnvironment("Model__Endpoint", modelEndpoint);

builder.AddProject<Projects.OpenClawNet_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(gateway)    // ← Service discovery
    .WaitFor(gateway)          // ← Orden de arranque
    .WithEnvironment("OpenClawNet__GatewayUrl", gatewayUrl);

builder.Build().Run();
```

<small>Reemplaza Docker Compose + scripts de inicio · [aspire.dev](https://aspire.dev)</small>

Note: WithReference = inyección automática de URL de servicio. WaitFor = Gateway debe estar saludable antes de que Web inicie. Un comando: aspire run.

---

<!-- .slide: class="demo-transition" -->

## 🎬 Demo en Vivo 2: Copilot CLI + Aspire

**Lo que haremos:** Debugging asistido por IA con datos de observabilidad reales

**Cómo:** Navegar a feature roto → Dashboard de Aspire muestra error → Copilot CLI lee trazas y sugiere fix

**Punto de enseñanza:** Aspire provee la señal de error; Copilot CLI la lee y propone la solución

**Observabilidad + IA = debugging más rápido**

Note: Debugging asistido por IA. El dashboard de Aspire provee telemetría; Copilot CLI la analiza y genera fixes.

---

<!-- .slide: class="demo-transition" -->

## Ejecutar el Full Stack

```bash
aspire run
```

1. Abrir Dashboard de Aspire → `https://localhost:15100`
2. Abrir UI Blazor → `http://localhost:5001`
3. Crear una sesión → Enviar un mensaje
4. ¡Observa tokens en streaming en tiempo real! 🚀
5. DevTools → Network → ver respuesta NDJSON streaming

Note: Todo lo que explicamos — interfaz, streaming, workspace, almacenamiento, streaming HTTP — todo en vivo.

---

<!-- .slide: class="content-slide" -->

## Lo Que Construimos ✓ + Vista Previa Sesión 2

<div class="two-col">
<div>

- ✅ 9 pilares de OpenClaw mapeados a .NET <!-- .element: class="fragment" -->
- ✅ `IAgentProvider` — 6 proveedores conectables (basados en MAF) <!-- .element: class="fragment" -->
- ✅ Archivos bootstrap de workspace — personalización de comportamiento sin código <!-- .element: class="fragment" -->
- ✅ Streaming de LLM local — `IAsyncEnumerable` → tokens en tiempo real <!-- .element: class="fragment" -->
- ✅ Almacenamiento EF Core — 7 entidades <!-- .element: class="fragment" -->
- ✅ Streaming HTTP NDJSON — tokens NDJSON al browser <!-- .element: class="fragment" -->
- ✅ Orquestación Aspire — un comando, full stack <!-- .element: class="fragment" -->

</div>
<div>

**Siguiente: darle superpoderes al chatbot**

- 🔧 Framework de tools: `ITool`, `IToolRegistry`, `IToolExecutor` <!-- .element: class="fragment" -->
- 📂 Tools integrados: FileSystem, Shell, Web, Scheduler <!-- .element: class="fragment" -->
- 🔄 El bucle del agente: pensar → actuar → observar → repetir <!-- .element: class="fragment" -->

</div>
</div>

Note: Fundamento sólido. En la Sesión 2, el agente aprende a HACER cosas en el mundo real.

---

<!-- .slide: class="content-slide" -->

## Recursos

| Recurso | Link |
|----------|------|
| 📦 GitHub Repo | [github.com/elbruno/openclawnet](https://github.com/elbruno/openclawnet) |
| 🦞 OpenClaw | [openclaw.ai](https://openclaw.ai) |
| 🚀 Aspire | [aspire.dev](https://aspire.dev) |
| 🤖 Foundry Local | [learn.microsoft.com/azure/foundry-local](https://learn.microsoft.com/azure/foundry-local) |
| 🦙 Ollama | [ollama.com](https://ollama.com) |
| 📡 HTTP NDJSON | [developer.mozilla.org/en-US/docs/Web/API/Server-sent_events](https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events) |
| 🤖 GitHub Copilot | [github.com/features/copilot](https://github.com/features/copilot) |

---

<!-- .slide: class="content-slide" -->

# ¡Gracias! 💜

## ¿Preguntas?

<div class="social-links">

**Bruno Capuano** — 🐦 [@elbruno](https://x.com/elbruno) · 💼 [LinkedIn](https://www.linkedin.com/in/elbruno/)

**Pablo Piovano** — 💼 [LinkedIn](https://www.linkedin.com/in/ppiova/)

</div>

<div class="series-info">

📦 [github.com/elbruno/openclawnet](https://github.com/elbruno/openclawnet)

**Próxima sesión:** Tools & Flujos de Agentes

</div>

Note: Todos los links en el README de la sesión. Dale star al repo y descarga el código para la Sesión 2.
