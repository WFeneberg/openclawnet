<!-- .slide: class="title-slide" -->

# 🦞 Sesión 2

## Tools + Flujos de Agentes

### OpenClawNet — Construye una Plataforma de Agentes AI con .NET 10

<div class="presenter-info">

**Bruno Capuano** · **Pablo Piovano** · Microsoft Reactor

</div>

Note:
¡Bienvenidos de vuelta! Sesión 2 — convertimos el chatbot en un agente con llamadas a herramientas y seguridad.

---

<!-- .slide: class="content-slide" -->

## 🔄 Recapitulación de la Sesión 1

| Lo que Construimos | Estado |
|--------------|--------|
| Models.Abstractions + proveedores LLM locales | ✅ |
| EF Core + almacenamiento SQLite | ✅ |
| Gateway API + streaming HTTP SSE | ✅ |
| Blazor UI con streaming | ✅ |
| Orquestación Aspire | ✅ |

**Resultado:** Un chatbot funcional que habla pero no puede *hacer* nada

Note:
Recapitulación rápida de la Sesión 1. Tenemos un chatbot. Responde preguntas, transmite respuestas, almacena historial. Pero está limitado a generar texto.

---

<!-- .slide: class="content-slide" -->

## 🎯 Objetivo de Hoy

> Convertir un **chatbot** en un **agente**

Un chatbot genera texto. <!-- .element: class="fragment" -->

Un agente **usa herramientas** para interactuar con el mundo. <!-- .element: class="fragment" -->

¿La diferencia? Una palabra: **tools**. <!-- .element: class="fragment" -->

Note:
El concepto más importante de hoy. Un chatbot solo puede hablar. Un agente puede actuar. El puente entre ellos es la llamada a herramientas.

---

<!-- .slide: class="section-divider" -->

# Etapa 1
## Arquitectura de Tools

🔧 12 minutos

Note:
Comencemos con la capa de abstracción de herramientas — cómo definimos, registramos y ejecutamos tools.

---

<!-- .slide: class="two-column" -->

## Chatbot vs Agente

| Chatbot | Agente |
|---------|-------|
| Recibe texto | Recibe texto |
| Genera texto | **Razona** sobre acciones |
| Devuelve texto | **Solicita llamadas a herramientas** |
| Terminado | Ejecuta herramientas → ciclo |

El modelo no ejecuta nada — emite una solicitud estructurada <!-- .element: class="fragment" -->

Note:
Distinción clave. El modelo dice "Quiero llamar a file_system con action=list". Nuestro código decide si permitirlo, lo ejecuta y devuelve el resultado. El modelo nunca toca el sistema de archivos directamente.

---

<!-- .slide: class="architecture-slide" -->

## Framework de Tools

```
┌─────────────────────────────────────────────┐
│          Orquestador de Agentes              │
├─────────────────────────────────────────────┤
│    IToolExecutor (aprobación + ejecución)   │
├──────────┬──────────┬──────────┬────────────┤
│ 📁 File  │ 💻 Shell │ 🌐 Web  │ ⏰ Sched  │
│ System   │ Tool     │ Tool    │ Tool       │
├──────────┴──────────┴──────────┴────────────┤
│      IToolRegistry (descubrimiento)         │
├─────────────────────────────────────────────┤
│    ITool + ToolMetadata + ToolResult        │
└─────────────────────────────────────────────┘
```

Note:
La arquitectura: abstracciones en la base, registro para descubrimiento, ejecutor para ejecución segura, orquestador en la cima coordinando todo.

---

<!-- .slide: class="code-slide" -->

## Interfaz ITool

```csharp
public interface ITool
{
    string Name { get; }
    string Description { get; }
    ToolMetadata Metadata { get; }
    
    Task<ToolResult> ExecuteAsync(
        ToolInput input, 
        CancellationToken cancellationToken = default);
}
```

- `Name` — cómo el modelo la llama <!-- .element: class="fragment" -->
- `Description` — cuándo usarla (¡ingeniería de prompts!) <!-- .element: class="fragment" -->
- `Metadata` — JSON Schema para parámetros <!-- .element: class="fragment" -->
- `ExecuteAsync` → `ToolResult` (¡no es un string!) <!-- .element: class="fragment" -->

Note:
Cuatro miembros. Name identifica la herramienta. Description le dice al modelo cuándo usarla — esto es ingeniería de prompts integrada en tu arquitectura. Metadata incluye el JSON Schema. ExecuteAsync devuelve un ToolResult estructurado con Success, Output, Error y Duration.

---

<!-- .slide: class="code-slide" -->

## ToolApprovalPolicy

```csharp
public interface IToolApprovalPolicy
{
    Task<bool> RequiresApprovalAsync(string toolName, string arguments);
    Task<bool> IsApprovedAsync(string toolName, string arguments);
}

// Default: everything auto-approves
public sealed class AlwaysApprovePolicy : IToolApprovalPolicy
{
    public Task<bool> RequiresApprovalAsync(...) => Task.FromResult(false);
    public Task<bool> IsApprovedAsync(...) => Task.FromResult(true);
}
```

ShellTool establece `RequiresApproval = true` <!-- .element: class="fragment" -->

Note:
La interfaz de puerta de seguridad. Dos preguntas: ¿esta herramienta requiere aprobación?, ¿ha sido aprobada? Por defecto es auto-aprobar todo. Pero ShellTool opta por aprobación — en producción mostrarías un diálogo de confirmación.

---

<!-- .slide: class="code-slide" -->

## ToolExecutor — El Punto de Control

```csharp
public async Task<ToolResult> ExecuteAsync(string toolName, string arguments, ...)
{
    // 1. Lookup
    var tool = _registry.GetTool(toolName);
    if (tool is null)
        return ToolResult.Fail(toolName, $"Tool '{toolName}' not found", ...);

    // 2. Approval check
    if (await _approvalPolicy.RequiresApprovalAsync(toolName, arguments) &&
        !await _approvalPolicy.IsApprovedAsync(toolName, arguments))
        return ToolResult.Fail(toolName, "Requires approval", ...);

    // 3. Execute with timing
    var sw = Stopwatch.StartNew();
    var result = await tool.ExecuteAsync(input, cancellationToken);
    _logger.LogInformation("Tool {Name} completed in {Duration}ms", 
        toolName, sw.ElapsedMilliseconds);
    return result;
}
```

Note:
Toda llamada a herramienta fluye por aquí. Búsqueda, aprobación, ejecución, registro. Este patrón de punto de control significa que agregamos métricas una vez, logging una vez, aprobación una vez — se aplica a TODAS las herramientas.

---

<!-- .slide: class="demo-transition" -->

## 🖥️ Demo en Vivo

### `GET /api/tools`

Mostrar el manifiesto de herramientas — lo que ve el modelo

Note:
Hora del demo. Mostrar el endpoint /api/tools. La respuesta JSON lista cada herramienta con nombre, descripción y esquema de parámetros. Esto es lo que el LLM usa para decidir qué herramienta llamar.

---

<!-- .slide: class="section-divider" -->

# Etapa 2
## Tools Integradas + Seguridad

🛡️ 15 minutos

Note:
Ahora veamos las cuatro herramientas integradas y las amenazas de seguridad contra las que cada una se defiende.

---

<!-- .slide: class="content-slide" -->

## Tres Amenazas de Seguridad

| Amenaza | Ataque | Impacto |
|--------|--------|--------|
| 🗂️ **Path Traversal** | `../../etc/passwd` | Exponer todo el sistema de archivos |
| 💻 **Command Injection** | `rm -rf /` | Destruir el servidor |
| 🌐 **SSRF** | `http://169.254.169.254/` | Filtrar credenciales de la nube |

Cada herramienta valida entradas **antes** de la ejecución <!-- .element: class="fragment" -->

Falla rápido. Falla seguro. <!-- .element: class="fragment" -->

Note:
Tres amenazas reales. Path traversal escapa del espacio de trabajo. Command injection ejecuta comandos destructivos. SSRF accede a redes internas. Cada herramienta tiene defensas específicas. El patrón es siempre el mismo: validar antes de ejecutar.

---

<!-- .slide: class="code-slide" -->

## FileSystemTool — Validación de Rutas

```csharp
private static readonly string[] BlockedPaths = 
    [".env", ".git", "appsettings.Production"];

private string? ResolvePath(string relativePath)
{
    // Check blocklist
    foreach (var blocked in BlockedPaths)
        if (relativePath.Contains(blocked, OrdinalIgnoreCase))
            return null;

    // Resolve and validate
    var fullPath = Path.GetFullPath(
        Path.Combine(_workspaceRoot, relativePath));

    // Does it stay in the workspace?
    if (!fullPath.StartsWith(_workspaceRoot, OrdinalIgnoreCase))
    {
        _logger.LogWarning("Path traversal blocked: {Path}", relativePath);
        return null;
    }
    return fullPath;
}
```

Note:
Dos defensas. Primero, una lista de bloqueo para archivos sensibles. Segundo, Path.GetFullPath resuelve todos los segmentos punto-punto, luego verificamos si el resultado aún comienza con la raíz de nuestro espacio de trabajo. Si alguien intenta ../../etc/passwd, GetFullPath lo resuelve y no comenzará con el workspace. Bloqueado.

---

<!-- .slide: class="code-slide" -->

## ShellTool — Lista de Bloqueo de Comandos

```csharp
private static readonly HashSet<string> BlockedCommands = 
    new(StringComparer.OrdinalIgnoreCase)
{
    "rm", "del", "format", "fdisk", "mkfs", "dd", 
    "shutdown", "reboot", "kill", "taskkill", 
    "net", "reg", "regedit", "powershell", "cmd"
};

private static bool IsSafeCommand(string command)
{
    var firstWord = command
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .FirstOrDefault()?.ToLowerInvariant();
    firstWord = Path.GetFileNameWithoutExtension(firstWord);
    return !BlockedCommands.Contains(firstWord);
}
```

+ timeout de 30s · límite de salida de 10K caracteres · `RequiresApproval = true` <!-- .element: class="fragment" -->

Note:
14 comandos bloqueados. Extraer primera palabra, quitar prefijo de ruta, verificar lista de bloqueo. Además un timeout de 30 segundos con muerte del árbol de procesos, límite de 10K caracteres de salida, y aprobación requerida. Defensa en profundidad.

---

<!-- .slide: class="code-slide" -->

## WebTool — Protección SSRF

```csharp
private static bool IsLocalUri(Uri uri)
{
    var host = uri.Host.ToLowerInvariant();
    return host == "localhost" ||
           host == "127.0.0.1" ||
           host == "::1" ||
           host.StartsWith("192.168.") ||
           host.StartsWith("10.") ||
           host.StartsWith("172.16.");
}
```

También: solo HTTP/HTTPS · límite de 50K caracteres · timeout de 15s <!-- .element: class="fragment" -->

Note:
Bloquear todas las direcciones privadas y locales. No localhost, no 127.0.0.1, no rangos privados. Solo esquemas HTTP y HTTPS — no file://, no ftp://. En producción también resolverías DNS para detectar trucos de CNAME.

---

<!-- .slide: class="two-column" -->

## Ataque → Bloqueado

| Intento de Ataque | Resultado |
|---------------|--------|
| "Lee `../../etc/passwd`" | ❌ Ruta fuera del espacio de trabajo |
| "Ejecuta `rm -rf /`" | ❌ Comando bloqueado por política |
| "Obtén `http://127.0.0.1:8080`" | ❌ Dirección local bloqueada |
| "Lee archivo `.env`" | ❌ Ruta bloqueada |
| "Ejecuta `shutdown`" | ❌ Comando bloqueado por política |

Todos los ataques capturados. Sin ejecución. <!-- .element: class="fragment" -->

Note:
Cinco intentos de ataque, cinco rechazos. La acción peligrosa nunca se ejecuta. Este es el modelo de seguridad en acción.

---

<!-- .slide: class="demo-transition" -->

## 🤖 Momento Copilot #1

### Agregar comandos bloqueados a ShellTool

> "Agrega `wget` y `curl` a la lista de comandos bloqueados. Estos podrían usarse para exfiltrar datos."

Note:
Primer momento Copilot. Extendemos la lista de bloqueo de ShellTool para incluir herramientas de red que podrían exfiltrar datos. Pequeño cambio, gran impacto en seguridad.

---

<!-- .slide: class="section-divider" -->

# Etapa 3
## Ciclo del Agente + Integración

🔄 15 minutos

Note:
El framework de herramientas está listo. Ahora veamos cómo el orquestador de agentes une todo con el ciclo de razonamiento.

---

<!-- .slide: class="architecture-slide" -->

## El Ciclo de Razonamiento del Agente

```
  ┌──────────────────┐
  │  Mensaje Usuario  │
  └────────┬─────────┘
           ▼
  ┌──────────────────┐
  │ Componer Prompt  │ ◄── system + history + tools
  └────────┬─────────┘
           ▼
  ┌──────────────────┐
  │  Llamar Modelo   │
  └────────┬─────────┘
           ▼
     ┌─────────────┐
     │ ¿Tool calls? │
     └──┬───────┬───┘
    YES │       │ NO
        ▼       ▼
  ┌──────────┐  ┌──────────────┐
  │ Ejecutar │  │ Devolver     │
  │ tools    │  │ respuesta    │
  └────┬─────┘  │ final        │
       │        └──────────────┘
       └──────► volver a "Llamar Modelo"

  Seguridad: máximo 10 iteraciones
```

Note:
El algoritmo central. Componer prompt, llamar modelo, verificar llamadas a herramientas. Si se solicitan herramientas, ejecutarlas y volver al ciclo. Si no hay herramientas, tenemos la respuesta final. Máximo 10 iteraciones como válvula de seguridad.

---

<!-- .slide: class="code-slide" -->

## AgentOrchestrator

```csharp
public async Task<AgentResponse> ProcessAsync(
    AgentRequest request, CancellationToken ct)
{
    var context = new AgentContext
    {
        SessionId = request.SessionId,
        UserMessage = request.UserMessage,
        ModelName = request.Model ?? "llama3.2",
        ProviderName = request.Provider
    };

    // Delegate to runtime (swappable implementation)
    var executedContext = await _runtime.ExecuteAsync(context, ct);

    return new AgentResponse
    {
        Content = executedContext.FinalResponse ?? string.Empty,
        ToolResults = executedContext.ToolResults,
        ToolCallCount = executedContext.ExecutedToolCalls.Count,
        TotalTokens = executedContext.TotalTokens
    };
}
```

Note:
El orquestador es la API pública. Crea el contexto, delega al runtime. Separación limpia — el orquestador no conoce herramientas, modelos ni prompts.

---

<!-- .slide: class="code-slide" -->

## DefaultAgentRuntime — El Ciclo

```csharp
while (iterations < MaxToolIterations)  // max = 10
{
    var response = await InvokeHostedAgentAsync(
        currentMessages, context.ModelName, toolDefs, ...);

    if (response.ToolCalls is { Count: > 0 })
    {
        foreach (var toolCall in response.ToolCalls)
        {
            var result = await _toolExecutor.ExecuteAsync(
                toolCall.Name, toolCall.Arguments, ct);

            currentMessages.Add(new ChatMessage
            {
                Role = ChatMessageRole.Tool,
                Content = result.Success ? result.Output 
                    : $"Error: {result.Error}",
                ToolCallId = toolCall.Id
            });
        }
        iterations++;
    }
    else
    {
        // No tool calls = final response
        context.FinalResponse = response.Content;
        return context;
    }
}
```

Note:
El corazón del agente. Ciclo while con máximo de iteraciones. Si el modelo devuelve llamadas a herramientas, ejecutar cada una a través del ejecutor, agregar resultados como mensajes Tool, volver al ciclo. Si el modelo devuelve solo texto, esa es la respuesta final.

---

<!-- .slide: class="code-slide" -->

## DefaultPromptComposer

```csharp
public async Task<IReadOnlyList<ChatMessage>> ComposeAsync(
    PromptContext context, CancellationToken ct)
{
    var messages = new List<ChatMessage>();

    // 1. System prompt + skills
    var systemContent = DefaultSystemPrompt;
    var skills = await _skillLoader.GetActiveSkillsAsync(ct);
    if (skills.Count > 0)
        systemContent += $"\n\n# Active Skills\n{skillText}";

    // 2. Session summary (long-term memory)
    if (!string.IsNullOrEmpty(context.SessionSummary))
        systemContent += $"\n\n# Summary\n{context.SessionSummary}";

    messages.Add(new ChatMessage { Role = System, Content = systemContent });

    // 3. History  +  4. Current message
    foreach (var msg in context.History) messages.Add(msg);
    messages.Add(new ChatMessage { Role = User, Content = context.UserMessage });

    return messages;
}
```

Definiciones de herramientas pasadas por separado vía API del modelo <!-- .element: class="fragment" -->

Note:
Cuatro capas: prompt de sistema con skills, resumen de sesión, historial de conversación, mensaje actual. Importante: las definiciones de herramientas NO están en el prompt de sistema — son objetos estructurados pasados vía la API del modelo.

---

<!-- .slide: class="demo-transition" -->

## 🖥️ Demo en Vivo

### El Agente Usa Herramientas

1. 📁 "Lista archivos en el directorio actual" → FileSystem
2. 🌐 "¿Qué hay en Hacker News?" → Web fetch + resumir
3. 🚫 "Ejecuta `rm -rf /`" → ¡Bloqueado!

Note:
Tres demos mostrando el ciclo del agente en acción. Listado de archivos, obtención web, y un comando peligroso bloqueado. Observa cómo las llamadas a herramientas fluyen a través del ejecutor.

---

<!-- .slide: class="demo-transition" -->

## 🤖 Momento Copilot #2

### Agregar estadísticas de ejecución a ToolExecutor

> "Agrega un método `GetExecutionStats()` que devuelva nombre de herramienta → duración promedio. Rastrea en un `ConcurrentDictionary`."

El **patrón de punto de control** hace esto trivial <!-- .element: class="fragment" -->

Note:
Segundo momento Copilot. Porque todas las herramientas pasan por el ejecutor, agregamos métricas en un solo lugar y obtenemos estadísticas para todo. Muestra el beneficio arquitectónico.

---

<!-- .slide: class="content-slide" -->

## ✅ Lo que Construimos

- ✅ Capa de abstracción de herramientas (ITool, IToolExecutor, IToolRegistry) <!-- .element: class="fragment" -->
- ✅ Puerta de política de aprobación (IToolApprovalPolicy) <!-- .element: class="fragment" -->
- ✅ FileSystemTool con prevención de path traversal <!-- .element: class="fragment" -->
- ✅ ShellTool con lista de bloqueo de comandos + timeout <!-- .element: class="fragment" -->
- ✅ WebTool con protección SSRF <!-- .element: class="fragment" -->
- ✅ SchedulerTool con CRUD de trabajos <!-- .element: class="fragment" -->
- ✅ Ciclo de razonamiento del agente (prompt → modelo → tool → ciclo) <!-- .element: class="fragment" -->
- ✅ Composición de prompt con inyección de herramientas <!-- .element: class="fragment" -->

Note:
Ocho marcas de verificación. Un framework completo de herramientas, cuatro herramientas con seguridad, y el ciclo del agente que une todo.

---

<!-- .slide: class="content-slide" -->

## 🛡️ Recapitulación de Seguridad

| Amenaza | Herramienta | Defensa |
|--------|------|---------|
| Path Traversal | FileSystemTool | `Path.GetFullPath` + límite de espacio de trabajo |
| Command Injection | ShellTool | HashSet de comandos bloqueados + timeout |
| SSRF | WebTool | Lista de bloqueo de IPs privadas + verificación de esquema |

**Patrón:** Validar entradas *antes* de la ejecución <!-- .element: class="fragment" -->

**Principio:** Falla rápido. Falla seguro. <!-- .element: class="fragment" -->

Note:
Tres amenazas, tres defensas. El patrón es siempre el mismo: validar antes de ejecutar. Esto se aplica a cualquier herramienta que construyas, no solo a estas cuatro.

---

<!-- .slide: class="content-slide" -->

## 🔮 Avance de la Próxima Sesión

### Sesión 3: Skills + Memoria

> "El agente ahora tiene manos. Siguiente: dale personalidad y memoria."

- **Skills** — Archivos YAML de personalidad que personalizan el comportamiento <!-- .element: class="fragment" -->
- **Memoria** — Resumen de conversación para contexto de largo plazo <!-- .element: class="fragment" -->
- **Carga de skills** — Descubrimiento dinámico + inyección en prompt de sistema <!-- .element: class="fragment" -->

Note:
Avance de la Sesión 3. El agente ahora puede actuar en el mundo. A continuación le damos personalidad a través de skills y memoria mediante resumen de conversación.

---

<!-- .slide: class="content-slide" -->

## Recursos

| Recurso | Enlace |
|----------|------|
| 📦 GitHub Repo | [github.com/elbruno/openclawnet](https://github.com/elbruno/openclawnet) |
| 🦀 OpenClaw | [openclaw.ai](https://openclaw.ai) |
| 🧠 NVIDIA NemoClaw | [nvidia.com/en-us/ai/nemoclaw](https://www.nvidia.com/en-us/ai/nemoclaw/) |
| 🔗 NemoClaw GitHub | [github.com/NVIDIA/NemoClaw](https://github.com/NVIDIA/NemoClaw) |
| 🚀 Aspire | [aspire.dev](https://aspire.dev) |
| 🤖 GitHub Copilot | [github.com/features/copilot](https://github.com/features/copilot) |

Note: Todos los enlaces están en el README de la sesión.

---

<!-- .slide: class="closing-slide" -->

# ¡Gracias! 💜

## OpenClawNet — Sesión 2 Completa

**Bruno Capuano** — 🐦 [@elbruno](https://x.com/elbruno) · 💼 [LinkedIn](https://www.linkedin.com/in/elbruno/)

**Pablo Piovano** — 💼 [LinkedIn](https://www.linkedin.com/in/ppiova/)

📦 [github.com/elbruno/openclawnet](https://github.com/elbruno/openclawnet)

⭐ Dale estrella · 🍴 Haz fork · 🔨 Rómpelo · 🔧 Arréglalo

¡Nos vemos en la **Sesión 3**!

Note:
¡Gracias! Todo el código está en el repositorio. Nos vemos la próxima para Skills y Memoria.
