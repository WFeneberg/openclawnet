<!-- .slide: class="title-slide" -->

# 🦞 Sesión 2

## Herramientas + Flujos de Agente

### OpenClawNet — Construye una Plataforma de Agentes IA con .NET 10

<div class="presenter-info">

**Bruno Capuano** · **Pablo Piovano** · Microsoft Reactor

</div>

Note:
¡Bienvenidos de vuelta! Sesión 2 — convertimos el chatbot en un agente con llamadas a herramientas y seguridad.

---

<!-- .slide: class="content-slide" -->

## 🔄 Resumen de la Sesión 1

| Qué Construimos | Estado |
|----------------|--------|
| Models.Abstractions + proveedores LLM locales | ✅ |
| EF Core + almacenamiento SQLite | ✅ |
| Gateway API + HTTP SSE streaming | ✅ |
| Interfaz Blazor con streaming | ✅ |
| Orquestación con Aspire | ✅ |

**Resultado:** Un chatbot funcional que habla pero no puede *hacer* nada

Note:
Breve resumen de la Sesión 1. Tenemos un chatbot. Responde preguntas, hace streaming de respuestas, almacena el historial. Pero está limitado a generar texto.

---

<!-- .slide: class="content-slide" -->

## 🎯 Objetivo de Hoy

> Convertir un **chatbot** en un **agente**

Un chatbot genera texto. <!-- .element: class="fragment" -->

Un agente **usa herramientas** para interactuar con el mundo. <!-- .element: class="fragment" -->

¿La diferencia? Una sola palabra: **herramientas**. <!-- .element: class="fragment" -->

Note:
El concepto más importante de hoy. Un chatbot solo puede hablar. Un agente puede actuar. El puente entre ellos es la llamada a herramientas.

---

<!-- .slide: class="section-divider" -->

# Etapa 1
## Arquitectura de Herramientas

🔧 12 minutos

Note:
Empecemos con la capa de abstracción de herramientas — cómo las definimos, registramos y ejecutamos.

---

<!-- .slide: class="two-column" -->

## Chatbot vs Agente

| Chatbot | Agente |
|---------|--------|
| Recibe texto | Recibe texto |
| Genera texto | **Razona** sobre acciones |
| Devuelve texto | **Solicita llamadas a herramientas** |
| Listo | Ejecuta herramientas → itera |

El modelo no ejecuta nada — emite una solicitud estructurada <!-- .element: class="fragment" -->

Note:
Distinción clave. El modelo dice "quiero llamar a file_system con action=list". Nuestro código decide si permitirlo, lo ejecuta y devuelve el resultado. El modelo nunca toca el sistema de archivos directamente.

---

<!-- .slide: class="architecture-slide" -->

## Framework de Herramientas

```
┌─────────────────────────────────────────────┐
│              Agent Orchestrator              │
├─────────────────────────────────────────────┤
│    IToolExecutor (approval + execution)     │
├──────────┬──────────┬──────────┬────────────┤
│ 📁 File  │ 💻 Shell │ 🌐 Web  │ ⏰ Sched  │
│ System   │ Tool     │ Tool    │ Tool       │
├──────────┴──────────┴──────────┴────────────┤
│         IToolRegistry (discovery)           │
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

- `Name` — cómo lo llama el modelo <!-- .element: class="fragment" -->
- `Description` — cuándo usarlo (¡ingeniería de prompts!) <!-- .element: class="fragment" -->
- `Metadata` — esquema JSON para parámetros <!-- .element: class="fragment" -->
- `ExecuteAsync` → `ToolResult` (¡no un string!) <!-- .element: class="fragment" -->

Note:
Cuatro miembros. Name identifica la herramienta. Description le dice al modelo cuándo usarla — esto es ingeniería de prompts integrada en tu arquitectura. Metadata incluye el esquema JSON. ExecuteAsync devuelve un ToolResult estructurado con Success, Output, Error y Duration.

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
La interfaz de seguridad. Dos preguntas: ¿esta herramienta requiere aprobación, y ha sido aprobada? El valor por defecto es aprobar todo automáticamente. Pero ShellTool opta por requerir aprobación — en producción mostrarías un diálogo de confirmación.

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
Toda llamada a herramienta pasa por aquí. Búsqueda, aprobación, ejecución, registro. Este patrón de punto de control significa que agregamos métricas una vez, registro una vez, aprobación una vez — se aplica a TODAS las herramientas.

---

<!-- .slide: class="demo-transition" -->

## 🖥️ Demo en Vivo

### `GET /api/tools`

Mostrar el manifiesto de herramientas — lo que ve el modelo

Note:
Hora del demo. Mostrar el endpoint /api/tools. La respuesta JSON lista cada herramienta con nombre, descripción y esquema de parámetros. Esto es lo que usa el LLM para decidir qué herramienta llamar.

---

<!-- .slide: class="section-divider" -->

# Etapa 2
## Herramientas Integradas + Seguridad

🛡️ 15 minutos

Note:
Ahora veamos las cuatro herramientas integradas y las amenazas de seguridad contra las que cada una se defiende.

---

<!-- .slide: class="content-slide" -->

## Tres Amenazas de Seguridad

| Amenaza | Ataque | Impacto |
|---------|--------|---------|
| 🗂️ **Path Traversal** | `../../etc/passwd` | Expone todo el sistema de archivos |
| 💻 **Inyección de Comandos** | `rm -rf /` | Destruye el servidor |
| 🌐 **SSRF** | `http://169.254.169.254/` | Filtra credenciales de nube |

Cada herramienta valida las entradas **antes** de la ejecución <!-- .element: class="fragment" -->

Fallar rápido. Fallar de forma segura. <!-- .element: class="fragment" -->

Note:
Tres amenazas reales. El path traversal escapa del workspace. La inyección de comandos ejecuta comandos destructivos. El SSRF accede a redes internas. Cada herramienta tiene defensas específicas. El patrón es siempre el mismo: validar antes de ejecutar.

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
Dos defensas. Primero, una lista negra para archivos sensibles. Segundo, Path.GetFullPath resuelve todos los segmentos punto-punto, luego verificamos si el resultado todavía comienza con nuestra raíz de workspace. Si alguien intenta ../../etc/passwd, GetFullPath lo resuelve, y no comenzará con el workspace. Bloqueado.

---

<!-- .slide: class="code-slide" -->

## ShellTool — Lista Negra de Comandos

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

+ tiempo límite de 30s · límite de 10K caracteres de salida · `RequiresApproval = true` <!-- .element: class="fragment" -->

Note:
14 comandos bloqueados. Extrae la primera palabra, quita el prefijo de ruta, verifica la lista negra. Más un tiempo límite de 30 segundos con eliminación del árbol de procesos, límite de 10K caracteres de salida, y aprobación requerida. Defensa en profundidad.

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

También: solo HTTP/HTTPS · límite de 50K caracteres · tiempo límite de 15s <!-- .element: class="fragment" -->

Note:
Bloquea todas las direcciones privadas y locales. Sin localhost, sin 127.0.0.1, sin rangos privados. Solo esquemas HTTP y HTTPS — sin file://, sin ftp://. En producción también resolverías DNS para detectar trucos con CNAME.

---

<!-- .slide: class="two-column" -->

## Ataque → Bloqueado

| Intento de Ataque | Resultado |
|------------------|-----------|
| "Leer `../../etc/passwd`" | ❌ Ruta fuera del workspace |
| "Ejecutar `rm -rf /`" | ❌ Comando bloqueado por política |
| "Obtener `http://127.0.0.1:8080`" | ❌ Dirección local bloqueada |
| "Leer archivo `.env`" | ❌ Ruta bloqueada |
| "Ejecutar `shutdown`" | ❌ Comando bloqueado por política |

Todos los ataques detectados. Sin ejecución. <!-- .element: class="fragment" -->

Note:
Cinco intentos de ataque, cinco rechazos. La acción peligrosa nunca se ejecuta. Así funciona el modelo de seguridad en la práctica.

---

<!-- .slide: class="demo-transition" -->

## 🤖 Momento Copilot #1

### Agregar comandos bloqueados a ShellTool

> "Agrega `wget` y `curl` a la lista de comandos bloqueados. Estos podrían usarse para exfiltrar datos."

Note:
Primer momento Copilot. Extendemos la lista negra de ShellTool para incluir herramientas de red que podrían exfiltrar datos. Cambio pequeño, gran impacto en seguridad.

---

<!-- .slide: class="section-divider" -->

# Etapa 3
## Bucle de Agente + Integración

🔄 15 minutos

Note:
El framework de herramientas está listo. Ahora veamos cómo el orquestador de agente conecta todo con el bucle de razonamiento.

---

<!-- .slide: class="architecture-slide" -->

## El Bucle de Razonamiento del Agente

```
  ┌──────────────────┐
  │  Mensaje Usuario  │
  └────────┬─────────┘
           ▼
  ┌──────────────────┐
  │ Componer Prompt   │ ◄── system + history + tools
  └────────┬─────────┘
           ▼
  ┌──────────────────┐
  │  Llamar Modelo   │
  └────────┬─────────┘
           ▼
     ┌─────────────┐
     │ ¿Herramientas?│
     └──┬───────┬───┘
    SÍ  │       │ NO
        ▼       ▼
  ┌──────────┐  ┌──────────────┐
  │ Ejecutar │  │ Devolver resp│
  │ herram.  │  │ final        │
  └────┬─────┘  └──────────────┘
       │
       └──────► volver a "Llamar Modelo"

  Seguridad: máx. 10 iteraciones
```

Note:
El algoritmo central. Componer prompt, llamar al modelo, verificar si hay llamadas a herramientas. Si se solicitan herramientas, ejecutarlas y volver al bucle. Si no hay herramientas, tenemos la respuesta final. Máximo 10 iteraciones como válvula de seguridad.

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
El orquestador es la API pública. Crea el contexto y delega al runtime. Separación limpia — el orquestador no sabe nada sobre herramientas, modelos o prompts.

---

<!-- .slide: class="code-slide" -->

## DefaultAgentRuntime — El Bucle

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
El corazón del agente. Bucle while con máximo de iteraciones. Si el modelo devuelve llamadas a herramientas, ejecuta cada una a través del ejecutor, agrega los resultados como mensajes Tool, y vuelve al bucle. Si el modelo devuelve solo texto, esa es la respuesta final.

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

Las definiciones de herramientas se pasan por separado mediante la API del modelo <!-- .element: class="fragment" -->

Note:
Cuatro capas: prompt del sistema con skills, resumen de sesión, historial de conversación, mensaje actual. Importante: las definiciones de herramientas NO están en el prompt del sistema — son objetos estructurados que se pasan mediante la API del modelo.

---

<!-- .slide: class="demo-transition" -->

## 🖥️ Demo en Vivo

### El Agente Usa Herramientas

1. 📁 "Lista los archivos del directorio actual" → FileSystem
2. 🌐 "¿Qué hay en Hacker News?" → Web fetch + resumir
3. 🚫 "Ejecutar `rm -rf /`" → ¡Bloqueado!

Note:
Tres demos mostrando el bucle del agente en acción. Listado de archivos, obtención web y un comando peligroso bloqueado. Observa las llamadas a herramientas fluir a través del ejecutor.

---

<!-- .slide: class="demo-transition" -->

## 🤖 Momento Copilot #2

### Agregar estadísticas de ejecución al ToolExecutor

> "Agrega un método `GetExecutionStats()` que devuelva nombre de herramienta → duración promedio. Seguimiento en un `ConcurrentDictionary`."

El **patrón de punto de control** hace esto trivial <!-- .element: class="fragment" -->

Note:
Segundo momento Copilot. Debido a que todas las herramientas pasan por el ejecutor, agregamos métricas en un solo lugar y obtenemos estadísticas para todo. Demuestra el beneficio arquitectónico.

---

<!-- .slide: class="content-slide" -->

## ✅ Qué Construimos

- ✅ Capa de abstracción de herramientas (ITool, IToolExecutor, IToolRegistry) <!-- .element: class="fragment" -->
- ✅ Puerta de política de aprobación (IToolApprovalPolicy) <!-- .element: class="fragment" -->
- ✅ FileSystemTool con prevención de path traversal <!-- .element: class="fragment" -->
- ✅ ShellTool con lista negra de comandos + tiempo límite <!-- .element: class="fragment" -->
- ✅ WebTool con protección SSRF <!-- .element: class="fragment" -->
- ✅ SchedulerTool con CRUD de trabajos <!-- .element: class="fragment" -->
- ✅ Bucle de razonamiento del agente (prompt → modelo → herramienta → bucle) <!-- .element: class="fragment" -->
- ✅ Composición de prompts con inyección de herramientas <!-- .element: class="fragment" -->

Note:
Ocho marcas de verificación. Un framework completo de herramientas, cuatro herramientas con seguridad, y el bucle del agente que lo conecta todo.

---

<!-- .slide: class="content-slide" -->

## 🛡️ Resumen de Seguridad

| Amenaza | Herramienta | Defensa |
|---------|-------------|---------|
| Path Traversal | FileSystemTool | `Path.GetFullPath` + límite de workspace |
| Inyección de Comandos | ShellTool | HashSet de comandos bloqueados + tiempo límite |
| SSRF | WebTool | Lista negra de IPs privadas + verificación de esquema |

**Patrón:** Validar entradas *antes* de la ejecución <!-- .element: class="fragment" -->

**Principio:** Fallar rápido. Fallar de forma segura. <!-- .element: class="fragment" -->

Note:
Tres amenazas, tres defensas. El patrón es siempre el mismo: validar antes de ejecutar. Esto aplica a cualquier herramienta que construyas, no solo a estas cuatro.

---

<!-- .slide: class="content-slide" -->

## 🔮 Vista Previa de la Próxima Sesión

### Sesión 3: Skills + Memoria

> "El agente ya tiene manos. Lo siguiente: darle personalidad y memoria."

- **Skills** — archivos YAML de personalidad que personalizan el comportamiento <!-- .element: class="fragment" -->
- **Memoria** — Resumen de conversaciones para contexto a largo plazo <!-- .element: class="fragment" -->
- **Carga de Skills** — Descubrimiento dinámico + inyección en prompt del sistema <!-- .element: class="fragment" -->

Note:
Vista previa de la Sesión 3. El agente ya puede actuar en el mundo. A continuación le damos personalidad a través de skills y memoria mediante el resumen de conversaciones.

---

<!-- .slide: class="content-slide" -->

## Recursos

| Recurso | Enlace |
|----------|------|
| 📦 Repositorio GitHub | [github.com/elbruno/openclawnet](https://github.com/elbruno/openclawnet) |
| 🦀 OpenClaw | [openclaw.ai](https://openclaw.ai) |
| 🧠 NVIDIA NemoClaw | [nvidia.com/en-us/ai/nemoclaw](https://www.nvidia.com/en-us/ai/nemoclaw/) |
| 🔗 NemoClaw GitHub | [github.com/NVIDIA/NemoClaw](https://github.com/NVIDIA/NemoClaw) |
| 🚀 Aspire | [aspire.dev](https://aspire.dev) |
| 🤖 GitHub Copilot | [github.com/features/copilot](https://github.com/features/copilot) |

Note: Todos los enlaces están en el README de la sesión.

---

<!-- .slide: class="closing-slide" -->

# ¡Gracias! 💜

## OpenClawNet — Sesión 2 Completada

**Bruno Capuano** — 🐦 [@elbruno](https://x.com/elbruno) · 💼 [LinkedIn](https://www.linkedin.com/in/elbruno/)

**Pablo Piovano** — 💼 [LinkedIn](https://www.linkedin.com/in/ppiova/)

📦 [github.com/elbruno/openclawnet](https://github.com/elbruno/openclawnet)

⭐ Dale una estrella · 🍴 Haz un fork · 🔨 Rómpelo · 🔧 Arréglalo

¡Nos vemos en la **Sesión 3**!

Note:
¡Gracias! Todo el código está en el repositorio. Nos vemos la próxima vez para Skills y Memoria.
