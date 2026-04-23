<!-- .slide: class="title-slide" -->

# 🦀 OpenClaw .NET

## Sesión 1: Fundamentos + Chat Local

### Construyendo una Plataforma de Agentes de IA con .NET

<div class="presenter-info">

Bruno Capuano · Pablo Piovano · Serie Microsoft Reactor

</div>

Note: ¡Bienvenidos! Este es un viaje de 5 sesiones. Todo el código ya está construido — estamos aquí para entender cada capa. Al final de hoy tendrás una pila de IA completa y funcionando en tu máquina.

---

<!-- .slide: class="content-slide" -->

## Hoja de Ruta de la Serie

<div class="roadmap-timeline">

**→ Sesión 1:** Fundamentos + Chat Local  
*Arquitectura, origen de OpenClaw, LLMs Locales, HTTP Streaming*

**Sesiones 2–5 (Próximamente):**
- Herramientas y Flujos de Trabajo de Agentes
- Habilidades y Memoria  
- Automatización y Nube
- Canales, Navegador y Eventos

</div>

Note: 5 sesiones. Cada una se construye sobre la anterior. Hoy = el fundamento del que todo depende.

---

<!-- .slide: class="content-slide" -->

## ¿Qué es OpenClaw?

<div class="two-col">
<div>

OpenClaw es una plataforma de agentes de código abierto — una **arquitectura de referencia** para construir agentes de IA con:

- Gateway persistente como plano de control <!-- .element: class="fragment" -->
- Runtime de agentes consciente del workspace <!-- .element: class="fragment" -->
- Herramientas, habilidades y memoria de primera clase <!-- .element: class="fragment" -->
- Abstracción de modelo multi-proveedor <!-- .element: class="fragment" -->
- Canales: Teams, WhatsApp, Telegram... <!-- .element: class="fragment" -->

</div>
<div>

**Los 9 Pilares:**

1. Gateway como Plano de Control
2. Runtime del Agente + Workspace
3. Sesiones y Memoria
4. Herramientas de Primera Clase
5. Sistema de Habilidades
6. Abstracción de Modelo
7. Automatización (cron + webhooks)
8. Superficies de UI (Control + WebChat)
9. Canales y Nodos

</div>
</div>

Note: OpenClaw define la arquitectura. Nosotros la implementamos. openclaw.ai es la referencia de la comunidad.

---

<!-- .slide: class="content-slide" -->

## OpenClaw .NET: OpenClaw en .NET

| Concepto OpenClaw | OpenClawNet | .NET |
|------------------|-------------|------|
| Gateway como Plano de Control | `OpenClawNet.Gateway` | Minimal APIs + HTTP SSE + Scheduler |
| Runtime del Agente + Workspace | `IAgentOrchestrator` + `WorkspaceLoader` | AGENTS.md / SOUL.md / USER.md |
| Sesiones y Memoria | `IMemoryService` + `ISummaryService` | EF Core + SQLite + compactación |
| Herramientas de Primera Clase | `ITool` + FileSystem, Shell, Web, Browser | Navegador con Playwright |
| Sistema de Habilidades | `SkillLoader` | Markdown/YAML con precedencia |
| Abstracción de Modelo | `IAgentProvider` + `RuntimeAgentProvider` | Ollama, Azure OpenAI, Foundry, Foundry Local, GitHub Copilot |
| Automatización | `JobSchedulerService` + WebhookEndpoints | cron + triggers de GitHub |
| Superficies de UI | Blazor Web App (Control UI + WebChat) | Ambas desde un proyecto |
| Canales y Nodos | `IChannel` + adaptador Teams | Bot Framework; nodos móviles planeados |

Note: Esta es la tabla de traducción. Cada pilar de OpenClaw se mapea a un tipo o proyecto .NET.

---

<!-- .slide: class="content-slide" -->

## ¿Qué Construiremos Hoy? — 3 Etapas

- **Etapa 1:** 🧱 Arquitectura y Abstracciones Clave (IAgentProvider, AgentProfile, records, DI) <!-- .element: class="fragment" -->
- **Etapa 2:** 🤖 LLMs Locales + Workspace + Almacenamiento (Ollama, FoundryLocal, archivos bootstrap, EF Core) <!-- .element: class="fragment" -->
- **Etapa 3:** ⚡ Gateway + HTTP SSE + Blazor (streaming en tiempo real, Aspire, demo del stack completo) <!-- .element: class="fragment" -->

Note: 3 etapas, ~15 min cada una. Cada etapa es un checkpoint ejecutable.

---

<!-- .slide: class="content-slide" -->

## Verificación de Requisitos

```bash
dotnet --version   # 10.0.x
code --version     # VS Code
```

**LLM Local** — elige uno:

```bash
ollama list          # llama3.2
foundry model list   # phi
```

Extensión de GitHub Copilot instalada y activa ✓

Note: Verificación rápida. Se requieren .NET 10 y un LLM local. Copilot para los momentos en vivo.

---

<!-- .slide: class="section-divider" -->

# Etapa 1

## Arquitectura y Abstracciones Clave

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
│              HTTP SSE + REST API                          │
│              (OpenClawNet.Gateway)                         │
├──────────────────────────────────────────────────────────┤
│              RuntimeAgentProvider (enrutador)              │
├────────┬──────────┬─────────┬────────────┬──────────────┤
│ Ollama │Azure AOAI│ Foundry │FoundryLocal│GitHub Copilot│
├────────┴──────────┴─────────┴────────────┴──────────────┤
│    Storage (EF Core)    │     ServiceDefaults (Aspire)    │
└─────────────────────────┴────────────────────────────────┘
```

**Arquitectura completa en la guía de la sesión — hoy nos enfocamos en las capas fundamentales**

Note: El Gateway es el centro neurálgico — persistente, con estado, todo pasa por él. RuntimeAgentProvider enruta al proveedor activo.

---

<!-- .slide: class="content-slide" -->

## 22 Proyectos, Una Responsabilidad Cada Uno

| Proyecto | Capa | Propósito |
|---------|-------|---------|
| AppHost | Orquestación | Host de Aspire |
| ServiceDefaults | Orquestación | Telemetría + salud |
| Gateway | Gateway | APIs, HTTP SSE streaming, planificador, canales |
| Web | UI | Blazor Control UI + WebChat |
| Agent | Agente | Orquestador, compositor de prompts, cargador de workspace |
| Models.Abstractions | Proveedor | `IAgentProvider`, `AgentProfile`, `ChatMessage`, `ToolDefinition` |
| Models.Ollama | Proveedor | Proveedor REST de Ollama |
| Models.FoundryLocal | Proveedor | Proveedor Foundry en dispositivo |
| Models.AzureOpenAI | Proveedor | SDK de Azure OpenAI |
| Models.Foundry | Proveedor | Proveedor cloud de Foundry |
| Models.GitHubCopilot | Proveedor | Proveedor SDK de GitHub Copilot |
| Tools.Abstractions | Herramientas | `ITool`, `IToolRegistry`, `IToolExecutor` |
| Tools.Core | Herramientas | Registro + ejecutor |
| Tools.FileSystem | Herramientas | Lectura/escritura segura de archivos |
| Tools.Shell | Herramientas | Ejecución de comandos |
| Tools.Web | Herramientas | Fetch HTTP |
| Tools.Scheduler | Herramientas | Herramienta de planificación de tareas |
| Tools.Browser | Herramientas | Navegador headless con Playwright |
| Skills | Habilidades | Parser + cargador Markdown |
| Memory | Memoria | Resumen, embeddings, búsqueda |
| Storage | Almacenamiento | EF Core + SQLite |
| Adapters.Teams | Canales | Adaptador Bot Framework |
| UnitTests + IntegrationTests | Pruebas | Suite de pruebas xUnit |

Note: Cada proyecto = una responsabilidad. Esto hace el código navegable y testeable.

---

<!-- .slide: class="content-slide" -->

## Los 9 Pilares en Código

- **1. Gateway** → `OpenClawNet.Gateway` (proceso persistente) <!-- .element: class="fragment" -->
- **2. Runtime del Agente + Workspace** → `IAgentOrchestrator` + `WorkspaceLoader` <!-- .element: class="fragment" -->
- **3. Sesiones y Memoria** → `IMemoryService` + `ISummaryService` <!-- .element: class="fragment" -->
- **4. Herramientas** → `ITool` + FileSystem, Shell, Web, Browser, Scheduler <!-- .element: class="fragment" -->
- **5. Habilidades** → `SkillLoader` (workspace > local > bundle) <!-- .element: class="fragment" -->
- **6. Abstracción de Modelo** → `IAgentProvider` + `RuntimeAgentProvider` (5 proveedores) <!-- .element: class="fragment" -->
- **7. Automatización** → `JobSchedulerService` + WebhookEndpoints <!-- .element: class="fragment" -->
- **8. Superficies de UI** → Blazor (Control UI + WebChat, misma app) <!-- .element: class="fragment" -->
- **9. Canales y Nodos** → `IChannel` + Teams + concepto de nodo <!-- .element: class="fragment" -->

Note: Una diapositiva, 9 pilares. Cada uno es un tema de sesión en la serie.

---

<!-- .slide: class="content-slide" -->

## Herramientas y Habilidades

**Herramientas** — capacidades de llamada a funciones
- `file_system`, `web_search`, `shell_exec`
- Implementadas como `ITool` + `ToolAIFunction` (Agent Framework)

**Habilidades Agent Framework** — revelación progresiva <!-- .element: class="fragment" -->
- Especificación `SKILL.md` (agentskills.io) <!-- .element: class="fragment" -->
- Patrón Advertir → Cargar → Ejecutar <!-- .element: class="fragment" -->
- `AgentSkillsProvider` de `Microsoft.Agents.AI` <!-- .element: class="fragment" -->

**Resultado**: El agente sabe QUÉ puede hacer (habilidades), CÓMO hacerlo (herramientas) <!-- .element: class="fragment" -->

Note: Las herramientas son las acciones. Las habilidades son el contexto. AgentSkillsProvider le dice al modelo qué habilidades están disponibles antes de cada llamada — revelación progresiva.

---

<!-- .slide: class="code-slide" -->

## El Contrato Clave: `IAgentProvider`

```csharp
public interface IAgentProvider
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

<small>📁 `src/OpenClawNet.Models.Abstractions/IAgentProvider.cs`</small>

Note: Tres métodos. Cada proveedor implementa exactamente este contrato. RuntimeAgentProvider enruta al proveedor activo — cambia Ollama por Azure o GitHub Copilot en tiempo de ejecución.

---

<!-- .slide: class="architecture-slide" -->

## Dónde Estamos en el Stack

```
┌──────────────────────────────────────────────────────────┐
│                      Blazor Web UI                        │
│                   (OpenClawNet.Web)                        │
├──────────────────────────────────────────────────────────┤
│              HTTP SSE + REST API                          │
│              (OpenClawNet.Gateway)                         │
├──────────────────────────────────────────────────────────┤
│              RuntimeAgentProvider (enrutador)              │
├────────┬──────────┬─────────┬────────────┬──────────────┤
│ Ollama │Azure AOAI│ Foundry │FoundryLocal│GitHub Copilot│
└────────┴──────────┴─────────┴────────────┴──────────────┘
```

**Ahora veámoslo en vivo — cambiando proveedores de modelo en tiempo de ejecución**

Note: Esta vista compacta muestra dónde estamos. RuntimeAgentProvider enruta al proveedor activo. Ahora demostraremos la flexibilidad en tiempo de ejecución.

---

<!-- .slide: class="demo-transition" -->

## 🎬 Demo en Vivo 1: Cambio de Proveedor — Sin Código

**Qué haremos:** Cambiar de Ollama local → Azure OpenAI → mismo chat, diferente backend

**Cómo:** Usar la página de Configuración — cambiar proveedor, guardar, iniciar nuevo chat

**Punto de enseñanza:** La abstracción `IAgentProvider` habilita flexibilidad en tiempo de ejecución con 5 proveedores

**Sin cambios de código. Sin reinicios. Solo configuración.**

Note: Este es el poder de la abstracción limpia. Misma interfaz, diferente implementación, cambio en tiempo de ejecución.

---

<!-- .slide: class="code-slide" -->

## Microsoft Agent Framework

**`AIAgent`** — la abstracción central
- `ChatClientAgent`: envuelve cualquier `IChatClient` (Ollama, Azure, Foundry)
- `AgentSkillsProvider`: agrega contexto de habilidades (revelación progresiva)
- `RunAsync()` / `RunStreamingAsync()`: ejecución unificada

**En OpenClawNet**: <!-- .element: class="fragment" -->

```csharp
var agent = new ChatClientAgent(
    agentProviderAdapter, // Puente IAgentProvider → IChatClient
    new ChatClientAgentOptions {
        AIContextProviders = [skillsProvider]
    });
await foreach (var update in agent.RunStreamingAsync(messages))
    // transmitir a la UI vía HTTP NDJSON
```
<!-- .element: class="fragment" -->

Note: Agent Framework provee el bucle de ejecución. Conectamos nuestro adaptador IAgentProvider y AgentSkillsProvider. El agente maneja la inyección de habilidades, las llamadas a herramientas y el streaming — nosotros controlamos el modelo y las habilidades.

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
| **Instalación** | CLI + descarga de modelos | Paquete NuGet (sin configuración) |
| **API** | REST (compatible con OpenAI) | SDK en proceso |
| **Modelos** | Llama, Phi, Mistral... | Phi, Qwen, DeepSeek... |
| **Aceleración HW** | Config GPU manual | GPU / NPU / CPU automático |
| **Privacidad** | Solo local | Solo local |

Ambos implementan `IAgentProvider` → **intercambia con una línea** <!-- .element: class="fragment" -->

Note: Ollama es el favorito de la comunidad. Foundry Local se distribuye como NuGet — detecta hardware automáticamente. Cero configuración del usuario.

---

<!-- .slide: class="content-slide" -->

## Archivos de Arranque del Workspace

El comportamiento del agente vive en **archivos markdown**, no en código:

| Archivo | Propósito |
|------|---------|
| `AGENTS.md` | Persona del agente, instrucciones, reglas de comportamiento |
| `SOUL.md` | Valores del agente, principios, salvaguardas éticas |
| `USER.md` | Perfil del usuario, preferencias, personalización |

Cargado por `WorkspaceLoader` al inicio de la sesión → inyectado en el system prompt <!-- .element: class="fragment" -->

```json
// WorkspaceLoader reads from the session workspace directory
// Precedence: workspace/ > local/ > bundle defaults
{
  "agentsMarkdown": "You are a helpful .NET assistant...",
  "soulMarkdown": "Always be honest. Never harm users...",
  "userMarkdown": "User prefers concise answers. Timezone: UTC-3..."
}
```

Note: No se necesitan cambios de código para cambiar el comportamiento del agente. Edita los archivos markdown. Así es como personalizas agentes para diferentes proyectos o usuarios.

---

<!-- .slide: class="architecture-slide" -->

## `IAsyncEnumerable`: El Pipeline de Streaming

```
┌─────────────────┐
│  Proveedor LLM  │  Ollama / Azure AOAI / Foundry / Local / Copilot
│  (generates)    │
└────────┬────────┘
         ▼ IAsyncEnumerable<ChatResponseChunk>
┌─────────────────┐
│ IAgentProvider  │  StreamAsync → yield return per token
│  (abstracts)    │
└────────┬────────┘
         ▼ IAsyncEnumerable<AgentStreamEvent>
┌─────────────────┐
│  IAgentOrch.    │  Tool loop + prompt composition
│  (orchestrates) │
└────────┬────────┘
         ▼ NDJSON HTTP stream
┌─────────────────┐
│  Blazor UI      │  Token-by-token rendering
│  (renders)      │
└─────────────────┘
```

Note: Cada capa usa IAsyncEnumerable. Sin búfer en ningún lugar. Los tokens fluyen de arriba a abajo en tiempo real.

---

<!-- .slide: class="content-slide" -->

## Capa de Almacenamiento — 7 Entidades

| Entidad | Propósito |
|--------|---------|
| `ChatSession` | Raíz — título, proveedor, modelo, marcas de tiempo |
| `ChatMessageEntity` | Rol, contenido, llamadas a herramientas, índice de orden |
| `SessionSummary` | Contexto de conversación compactado |
| `ToolCallRecord` | Registro de auditoría de ejecución de herramientas |
| `ScheduledJob` | Definición de tarea cron (nombre, expresión cron) |
| `JobRun` | Historial y resultados de ejecución de tareas |
| `ProviderSetting` | Configuración por proveedor |

Note: 7 entidades. Cada una justifica su lugar. SessionSummary es cómo manejamos conversaciones largas sin agotar las ventanas de contexto.

---

<!-- .slide: class="demo-transition" -->

## 🎬 Demo en Vivo 3: Cambio de Personalidad del Agente

**Qué haremos:** Cambiar el comportamiento del agente editando archivos de workspace — sin código

**Cómo:** Editar `AGENTS.md` → guardar → iniciar nuevo chat → ver el cambio de personalidad

**Punto de enseñanza:** Los archivos de workspace (`AGENTS.md`, `SOUL.md`, `USER.md`) controlan el comportamiento del agente sin tocar C# ni reiniciar servicios

**Personalización de comportamiento impulsada por Markdown**

Note: La arquitectura workspace-first significa que el comportamiento cambia sin tocar el código. Esto es personalización de agente sin código.

---

<!-- .slide: class="section-divider" -->

# Etapa 3

## Gateway + HTTP SSE + Blazor

⏱️ 15 minutos

Note: Todas las piezas se conectan para el chat en tiempo real.

---

<!-- .slide: class="content-slide" -->

## Gateway: El Plano de Control

- 🔌 Gestiona conexiones persistentes de canales (Teams, futuros adaptadores) <!-- .element: class="fragment" -->
- 🌐 Expone APIs REST para todas las operaciones de gestión <!-- .element: class="fragment" -->
- ⚡ Expone endpoint HTTP SSE para streaming de tokens en tiempo real <!-- .element: class="fragment" -->
- 🖥️ Sirve Control UI + WebChat (ambas desde una app Blazor) <!-- .element: class="fragment" -->
- 🔗 Maneja endpoints webhook para triggers de eventos externos <!-- .element: class="fragment" -->
- ⏰ Ejecuta el planificador cron persistente para tareas automatizadas <!-- .element: class="fragment" -->
- 🔥 Dispara eventos del sistema (tarea iniciada, mensaje recibido, sesión creada) <!-- .element: class="fragment" -->

> "Todo pasa por el Gateway. Es el centro neurálgico del sistema." <!-- .element: class="fragment" -->

Note: NO es un proxy sin estado. Es un proceso persistente y con estado que gestiona conexiones de canales, estado de tareas y enrutamiento de eventos.

---

<!-- .slide: class="code-slide" -->

## `ChatStreamEndpoints` — Tokens al Navegador

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

Note: Endpoint de Minimal API. Devuelve NDJSON — cada línea es un evento JSON discreto. Los errores se muestran como códigos de estado HTTP. Más simple y depurable que WebSocket.

---

<!-- .slide: class="code-slide" -->

## Aspire: 18 Líneas Reemplazan Docker Compose

```csharp
// AppHost.cs — the entire topology
var gateway = builder.AddProject<Projects.OpenClawNet_Gateway>("gateway")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithEnvironment("ConnectionStrings__DefaultConnection", connStr)
    .WithEnvironment("Model__Endpoint", modelEndpoint);

builder.AddProject<Projects.OpenClawNet_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(gateway)    // ← Service discovery
    .WaitFor(gateway)          // ← Startup ordering
    .WithEnvironment("OpenClawNet__GatewayUrl", gatewayUrl);

builder.Build().Run();
```

<small>Reemplaza Docker Compose + scripts de inicio · [aspire.dev](https://aspire.dev)</small>

Note: WithReference = inyección automática de URL del servicio. WaitFor = Gateway debe estar sano antes de que Web inicie. Un comando: aspire run.

---

<!-- .slide: class="demo-transition" -->

## 🎬 Demo en Vivo 2: Copilot CLI + Aspire

**Qué haremos:** Depuración asistida por IA con datos de observabilidad reales

**Cómo:** Navegar a función rota → Panel de Aspire muestra error → Copilot CLI lee traces y sugiere corrección

**Punto de enseñanza:** Aspire proporciona la señal de error; Copilot CLI la lee y propone la solución

**Observabilidad + IA = depuración más rápida**

Note: Depuración asistida por IA. El panel de Aspire proporciona telemetría; Copilot CLI la analiza y genera correcciones.

---

<!-- .slide: class="demo-transition" -->

## Ejecutar el Stack Completo

```bash
aspire run
```

1. Abrir Panel de Aspire → `https://localhost:15100`
2. Abrir UI de Blazor → `http://localhost:5001`
3. Crear una sesión → Enviar un mensaje
4. ¡Ver los tokens en streaming en tiempo real! 🚀
5. DevTools → Network → ver respuesta streaming NDJSON

Note: Todo lo que explicamos — interfaz, streaming, workspace, almacenamiento, HTTP streaming — todo en vivo.

---

<!-- .slide: class="content-slide" -->

## Qué Construimos ✓ + Vista Previa Sesión 2

<div class="two-col">
<div>

- ✅ 9 pilares de OpenClaw mapeados a .NET <!-- .element: class="fragment" -->
- ✅ `IAgentProvider` — 5 proveedores intercambiables (basado en MAF) <!-- .element: class="fragment" -->
- ✅ Archivos bootstrap del workspace — personalización de comportamiento sin código <!-- .element: class="fragment" -->
- ✅ Streaming LLM local — `IAsyncEnumerable` → tokens en tiempo real <!-- .element: class="fragment" -->
- ✅ Almacenamiento EF Core — 7 entidades <!-- .element: class="fragment" -->
- ✅ HTTP SSE streaming — tokens NDJSON al navegador <!-- .element: class="fragment" -->
- ✅ Orquestación con Aspire — un comando, stack completo <!-- .element: class="fragment" -->

</div>
<div>

**Próximo: dale superpoderes al chatbot**

- 🔧 Framework de herramientas: `ITool`, `IToolRegistry`, `IToolExecutor` <!-- .element: class="fragment" -->
- 📂 Herramientas integradas: FileSystem, Shell, Web, Scheduler <!-- .element: class="fragment" -->
- 🔄 El bucle del agente: pensar → actuar → observar → repetir <!-- .element: class="fragment" -->

</div>
</div>

Note: Base sólida. En la Sesión 2, el agente aprende a HACER cosas en el mundo real.

---

<!-- .slide: class="content-slide" -->

## Recursos

| Recurso | Enlace |
|----------|------|
| 📦 Repositorio GitHub | [github.com/elbruno/openclawnet](https://github.com/elbruno/openclawnet) |
| 🦞 OpenClaw | [openclaw.ai](https://openclaw.ai) |
| 🚀 Aspire | [aspire.dev](https://aspire.dev) |
| 🤖 Foundry Local | [learn.microsoft.com/azure/foundry-local](https://learn.microsoft.com/azure/foundry-local) |
| 🦙 Ollama | [ollama.com](https://ollama.com) |
| 📡 HTTP SSE | [developer.mozilla.org/en-US/docs/Web/API/Server-sent_events](https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events) |
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

**Próxima sesión:** Herramientas y Flujos de Trabajo de Agentes

</div>

Note: Todos los enlaces en el README de la sesión. Dale una estrella al repositorio y descarga el código para la Sesión 2.
