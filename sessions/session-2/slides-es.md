---
marp: true
title: "OpenClawNet — Sesión 2: Tools y Workflows de Agentes"
description: "De chatbot a agente: ITool + MCP en proceso, una puerta de aprobación, un solo runtime"
theme: openclaw
paginate: true
size: 16:9
footer: "OpenClawNet · Sesión 2 · Tools y Workflows de Agentes"
---

<!-- _class: lead -->

# OpenClawNet
## Sesión 2 — Tools y Workflows de Agentes

**Serie Microsoft Reactor · ~75 min · .NET Intermedio**

> *Los tools convierten un chatbot en un compañero de trabajo.*

<br>

<div class="speakers">

**Bruno Capuano** — Principal Cloud Advocate, Microsoft
[github.com/elbruno](https://github.com/elbruno) · [@elbruno](https://twitter.com/elbruno)

**Pablo Piovano** — Microsoft MVP
[linkedin.com/in/ppiova](https://www.linkedin.com/in/ppiova/)

</div>

<!--
SPEAKER NOTES — title slide.
Hola a todos, bienvenidos de vuelta. En la Sesión 1 creamos una app real en Blazor + Aspire hablando con LLMs locales y en la nube. Hoy cerramos la brecha entre "chatbot" y "agente": le damos al modelo la capacidad de HACER cosas — leer archivos, ejecutar shells, llamar APIs, incluso lanzar otros agentes. Lo haremos dos veces: con nuestro propio framework ITool Y con servidores MCP. Mismo agente. Misma puerta de aprobación. Un solo modelo mental.
Promesa: al final habrás escrito un tool personalizado, intercambiado una política de aprobación, conectado un servidor MCP, y visto un loop de agente real elegir el tool correcto para el trabajo.
-->

---

## Dónde quedó la Sesión 1

- Una app real en **Aspire**, 27 proyectos, 4 capas
- `IAgentProvider` abstrae 5 proveedores de modelos
- Streaming HTTP **NDJSON** hacia una UI Blazor
- Persistencia EF Core / SQLite + perfiles de agente
- `aspire start` → chat con `llama3.2` en 30 segundos

> Hoy agregamos el ingrediente faltante: **acción**.

<!--
SPEAKER NOTES — recap.
Recap rápido de la Sesión 1 en 30 segundos. Tenemos un stack Aspire funcionando, cinco proveedores detrás de una interfaz IAgentProvider, una UI Blazor con streaming NDJSON, y conversaciones respaldadas por SQLite. Lo que NO tenemos todavía es acción — un chatbot que solo habla no es un agente. Hoy arreglamos eso.
Si alguien llegó tarde y se perdió la Sesión 1, la grabación y el código están en github.com/elbruno/openclawnet.
-->

---

## Chatbot vs. agente

|              | Chatbot                       | **Agente**                                    |
|--------------|-------------------------------|----------------------------------------------|
| Entradas     | texto                         | texto **+ resultados de tools**              |
| Salidas      | texto                         | texto **+ llamadas a tools**                 |
| Loop         | un turno                      | **multi-turno hasta terminar**               |
| Efectos secundarios | ninguno              | **filesystem, red, shells, schedules**       |
| Perfil de riesgo | bajo                   | **necesita una puerta de aprobación**        |

<!--
SPEAKER NOTES — chatbot vs agent.
Esta es la única slide teórica del día, así que aprovechémosla. Un chatbot es algo de petición/respuesta — envías texto, obtienes texto. Un agente hace eso ADEMÁS puede decidir "necesito más información" o "necesito hacer un cambio", emite una llamada a tool, obtiene un resultado de vuelta, y usa ese resultado para decidir qué hacer después. Ese es el loop. La gran consecuencia arquitectónica es que entran los efectos secundarios — y en el momento en que existen efectos secundarios, necesitas una historia de "¿quién aprobó esto?" Por eso las políticas de aprobación son un ciudadano de primera clase en OpenClawNet.
-->

---

## Dos superficies de tools, un agente

<div class="cols">
<div>

### `ITool` (en proceso)
- 100% C#, en tu proceso
- Overhead de microsegundos
- Control total del schema
- Ideal para: hot path, secretos, lógica personalizada

</div>
<div>

### MCP (Model Context Protocol)
- Protocolo abierto, agnóstico del lenguaje
- Transporte stdio o en proceso
- Reutiliza cualquier servidor de la comunidad
- Ideal para: integraciones de terceros, equipos políglotas

</div>
</div>

> Mismo loop de agente. Misma `IToolApprovalPolicy`. Mismos eventos NDJSON.

<!--
SPEAKER NOTES — two surfaces.
Esta es la slide que configura toda la sesión. OpenClawNet incluye DOS formas de darle tools a un agente, y la gente a menudo piensa que tiene que elegir una religión. No es así. Usa ITool cuando la velocidad y el control importan — tu propio código, tu propio proceso, sin marshalling, sin overhead de protocolo. Usa MCP cuando quieras conectar algo que la comunidad ya escribió — servidor MCP de GitHub, Notion, un conector de base de datos de un proveedor. La decisión de diseño crucial que tomamos es que el agente en sí, el runtime, y la puerta de aprobación no se preocupan por de qué superficie vino un tool. Desde el punto de vista del modelo, todos son simplemente tools en el manifiesto.
-->

---

## El build de hoy, en números

- **`OpenClawNet.Tools.Abstractions`** — `ITool`, `ToolMetadata`, `ToolResult`
- **`OpenClawNet.Tools.Core`** — registry, executor, políticas de aprobación
- **5** tools en proceso — FileSystem, Shell, Web, Image, Scheduler
- **5** servidores MCP incluidos — FileSystem, Web, Browser, Shell, Abstractions
- **1** `IToolApprovalPolicy` unificada cubriendo ambos
- **5** demos ejecutables en `docs/sessions/session-2/code/`

<!--
SPEAKER NOTES — by the numbers.
Ubicación rápida antes de entrar en código. Hay dos pares de proyectos que debes conocer: Tools.Abstractions (interfaces) y Tools.Core (registry, executor, políticas). Encima de esos, cinco tools integrados en proceso y cinco servidores MCP incluidos. Toda la superficie de tools está gobernada por una interfaz de política de aprobación, y tenemos cinco demos de consola en el repo que puedes ejecutar en el tren a casa.
-->

---

# 🔧  Etapa 1 — Tools en Proceso (`ITool`)

<!--
SPEAKER NOTES — Stage 1 divider.
Primera media hora: tools en proceso. Veremos el contrato, la separación registry/executor, la puerta de aprobación, y un tool personalizado desde cero.
-->

---

## `ITool` — el contrato

```csharp
public interface ITool
{
    string Name { get; }
    string Description { get; }
    ToolMetadata Metadata { get; }

    Task<ToolResult> ExecuteAsync(
        ToolInput input,
        CancellationToken ct = default);
}
```

- Un método, un tipo de retorno — fácil de mockear, fácil de probar
- `ToolInput` lleva argumentos + identidad del llamador
- `ToolResult` es `Ok(...)` o `Fail(...)` (información de error rica)

<!--
SPEAKER NOTES — ITool contract.
Cinco miembros en total. Name es el identificador que el modelo usa al emitir una llamada a tool. Description es lo que el modelo LEE para decidir si usar este tool — escríbelo como un docstring, no como un comentario de código. Metadata es el lado estructurado: schema de parámetros, requisito de aprobación, tags, categoría. ExecuteAsync obtiene los argumentos parseados y un token de cancelación. ToolResult es un record discriminado — Ok o Fail — así que los llamadores nunca tienen que elegir entre "lanzar una excepción" y "retornar null". Ambas ramas llevan duración para que la observabilidad sea gratuita.
-->

---

## `ToolMetadata` — lo que ve el LLM

```csharp
public sealed record ToolMetadata(
    JsonDocument ParameterSchema, // JSON Schema draft 2020-12
    bool RequiresApproval,        // ¿necesita un humano?
    string Category,              // "fs" | "shell" | "web" | ...
    string[] Tags);
```

- **Parameter schema** = el contrato con el modelo
- **`RequiresApproval`** = el contrato con el humano
- **Category + Tags** impulsan los filtros del "tool picker" de la UI

<!--
SPEAKER NOTES — ToolMetadata.
El record de metadata es pequeño pero cada campo tiene peso. Parameter schema es JSON Schema 2020-12 — eso es lo que se convierte en una definición de función cuando le entregamos el manifiesto al modelo. Haz bien el schema y el modelo llamará al tool con los argumentos correctos la primera vez; hazlo mal y desperdiciarás tokens en loops de reintento. RequiresApproval es el guardrail más seguro en todo el sistema: cuando es true, el executor se pausará y emitirá un evento ToolApprovalRequest antes de hacer algo destructivo. Category y Tags no afectan el comportamiento en runtime — son para la página de Tools en la UI, para que los usuarios puedan filtrar por superficie o por capacidad.
-->

---

## Registry vs. Executor

<div class="cols">
<div>

### `IToolRegistry`
- **Descubrimiento** (global, singleton)
- "¿Qué tools existen?"
- "Dame uno por nombre"
- Fusiona tools en proceso **y** MCP

</div>
<div>

### `IToolExecutor`
- **Ejecución** (scoped, por solicitud)
- Puerta de aprobación
- Stopwatch + try/catch
- Emite eventos NDJSON

</div>
</div>

> Separación = puedes intercambiar uno sin romper el otro.

<!--
SPEAKER NOTES — registry vs executor.
Esta separación parece burocrática pero es estructural. El registry es global, con scope de singleton — es simplemente "el catálogo de tools actualmente cargados". El executor tiene scope por solicitud, porque la política de aprobación puede necesitar saber quién está llamando, en qué conversación, con qué presupuesto restante. Al mantener el descubrimiento y la ejecución separados podemos hacer cosas como: cargar un servidor MCP en runtime y que aparezca en el registry sin reiniciar el executor; o envolver el executor con telemetría sin tocar el registry. Y más tarde en la sesión verás que esto rinde cuando los tools MCP simplemente encajan en el mismo registry.
-->

---

## La puerta de aprobación, en 6 líneas

```csharp
if (await _policy.RequiresApprovalAsync(tool, input, ct)
 && !await _policy.IsApprovedAsync(tool, input, ct))
{
    return ToolResult.Fail(
        tool.Name,
        "approval required",
        TimeSpan.Zero);
}
```

- Política por defecto: **`AlwaysApprovePolicy`** (genial para demos, terrible para prod)
- Producción: reemplaza con una que pregunte al usuario (prompt de UI, tarjeta de Teams, ...)
- Misma puerta cubre `ITool` **y** tools MCP

<!--
SPEAKER NOTES — approval gate.
Seis líneas. Esa es toda la puerta de aprobación. La interfaz tiene dos métodos: RequiresApprovalAsync (¿necesita esta combinación de tool + argumentos un humano?) e IsApprovedAsync (¿ya dijo que sí un humano?). La implementación por defecto dice "sí, siempre aprobado" que está bien para una demo y es un desastre para producción. El patrón que recomendamos en producción es: RequiresApprovalAsync mira los metadatos del tool Y los argumentos — por ejemplo "cualquier comando shell que contenga rm -rf necesita aprobación, todo lo demás está bien" — e IsApprovedAsync verifica un mapa en memoria poblado por un callback de UI o una tarjeta adaptativa de Teams. La propiedad crucial de este diseño: cuando agregamos soporte MCP más tarde, las mismas seis líneas se ejecutan para tools MCP también. Una puerta, dos superficies.
-->

---

## Conectándolo — un método de extensión

```csharp
services.AddToolFramework();   // registry + executor + AlwaysApprovePolicy
services.AddTool<MyAwesomeTool>();
services.AddTool<AnotherTool>();
```

Para intercambiar la política de aprobación:

```csharp
services.RemoveAll<IToolApprovalPolicy>();
services.AddSingleton<IToolApprovalPolicy, MyHumanInTheLoopPolicy>();
```

<!--
SPEAKER NOTES — wiring.
El objetivo es wiring de dos líneas. AddToolFramework registra el registry, el executor, y una política de aprobación por defecto. AddTool<T> agrega tu tool como un ITool singleton — el registry lo recoge automáticamente porque se inyecta con IEnumerable&lt;ITool&gt;. Para intercambiar la política en producción, eliminas la predeterminada y agregas la tuya. Veremos este patrón exacto en el Demo 2.
-->

---

## 🤖 Momento Copilot — scaffoldear un tool

> "Genera una implementación de `ITool` llamada `WeatherTool` que tome un parámetro de string `city` y devuelva un pronóstico falso. Incluye `ToolMetadata` con un JSON Schema. Usa los mismos patrones que `FileSystemTool` en este repo."

- Copilot lee tools hermanos como ejemplos
- Genera schema, metadata, y un path de `Fail`
- Te ahorra ~20 minutos por tool

<!--
SPEAKER NOTES — Copilot moment.
Demo rápida si el tiempo lo permite, de lo contrario solo describe. Copilot en el IDE es increíble para este patrón porque los tools son TAN formulaicos — misma forma, diferente cuerpo. Abre el repo, escribe ese prompt, míralo generar un ITool con forma completa con el JSON Schema correctamente poblado. El truco es el "usa los mismos patrones que FileSystemTool" — le dice a Copilot qué archivo mirar como referencia. Sin esa pista obtienes una respuesta genérica; con ella obtienes algo que compila en tu código base.
-->

---

# 🌐  Etapa 2 — Tools MCP

<!--
SPEAKER NOTES — Stage 2 divider.
Ahora lo nuevo. MCP — Model Context Protocol — es el estándar abierto para dejar que los agentes hablen con servidores de tools externos. Lo integramos en OpenClawNet para que el agente pueda usar ambas superficies sin conocer la diferencia.
-->

---

## Qué es MCP, en 30 segundos

- **Model Context Protocol** — especificación abierta, JSON-RPC sobre stdio o HTTP
- Un **servidor** expone: tools, prompts, recursos
- Un **host** (nosotros) los consume y los expone al modelo
- Ecosistema: GitHub, Filesystem, Notion, Slack, Postgres, Browser, …

> Cualquier cosa que puedas empaquetar como un servidor MCP, OpenClawNet puede usarla.

<!--
SPEAKER NOTES — MCP intro.
Para quienes aún no han visto MCP: es un protocolo abierto de Anthropic, ahora adoptado en la industria, que estandariza cómo los agentes hablan con proveedores de tools externos. Un servidor habla JSON-RPC sobre stdio o HTTP, expone una lista de tools (y opcionalmente prompts y recursos), y el host del agente conecta esos tools en el manifiesto del modelo. La gran ganancia es un ecosistema en rápido crecimiento: hay servidores MCP para GitHub, el filesystem, Notion, Slack, Postgres, navegadores Playwright, y docenas más. En el momento en que tu host de agente habla MCP, los obtienes todos gratis.
-->

---

## Por qué OpenClawNet tiene ambos

```
┌─────────────────────────────────────────────────┐
│           DefaultAgentRuntime                   │
│   (el loop del agente, agnóstico del proveedor) │
├─────────────────────────────────────────────────┤
│        IToolRegistry  +  IToolExecutor          │
│      (una puerta de aprobación, un stream de eventos) │
├─────────────────────┬───────────────────────────┤
│   in-process ITool  │     MCP tool wrapper      │
│  (FileSystem, Web,  │   (StdioMcpHost o         │
│   Shell, Image, ...) │    InProcessMcpHost)      │
└─────────────────────┴───────────────────────────┘
```

- El modelo ve **un manifiesto de tools fusionado**
- El runtime emite los **mismos eventos NDJSON** para ambos
- Telemetría etiqueta `tool.source` = `"in-process"` o `"mcp:<server>"`

<!--
SPEAKER NOTES — unified architecture.
Este es el diagrama que explica todo el diseño. Mira la fila inferior: ITools en proceso a la izquierda, tools MCP a la derecha. Ambos se alimentan HACIA ARRIBA en un IToolRegistry y un IToolExecutor — un manifiesto fusionado, una puerta de aprobación, un stream de eventos. El runtime arriba no sabe ni le importa de qué superficie vino un tool. El único lugar donde aparece la distinción es en OpenTelemetry, donde etiquetamos cada span con tool.source para que puedas filtrar tus trazas por superficie.
-->

---

## Los 5 servidores MCP incluidos

| Servidor | Tipo | Qué hace |
|--------|------|--------------|
| `OpenClawNet.Mcp.FileSystem` | en proceso | operaciones de archivo sandboxeadas |
| `OpenClawNet.Mcp.Web` | en proceso | fetch HTTP + búsqueda |
| `OpenClawNet.Mcp.Shell` | en proceso | ejecución de shell protegida |
| `OpenClawNet.Mcp.Browser` | en proceso | navegador impulsado por Playwright |
| `OpenClawNet.Mcp.Abstractions` | (lib) | contratos compartidos |

> Incluido = viene por defecto, registrado vía `IBundledMcpServerRegistration`.

<!--
SPEAKER NOTES — bundled servers.
Incluimos cinco servidores MCP en la caja. FileSystem, Web y Shell reflejan los tools en proceso — misma capacidad, expuesta sobre el protocolo MCP para que los agentes externos también puedan usarlos. Browser está impulsado por Playwright y es el único que necesita una descarga de Chromium en la primera ejecución. Abstractions es solo la biblioteca de contratos compartidos. Los cinco son "incluidos" — se auto-registran en el inicio vía IBundledMcpServerRegistration, no se necesita configuración JSON. También puedes agregar servidores MCP externos vía la página /mcp-settings de la UI, que veremos en un minuto.
-->

---

## Dos transportes disponibles de fábrica

<div class="cols">
<div>

### `StdioMcpHost`
- Subproceso, JSON-RPC sobre stdin/stdout
- Perfecto para servidores `npx` / `uvx`
- Ciclo de vida: iniciar, health-check, reiniciar
- Aislamiento de crashes

</div>
<div>

### `InProcessMcpHost`
- Pipe en memoria, costo de serialización cero
- Para nuestros 5 servidores incluidos
- Mismo protocolo MCP en el wire
- Más fácil de depurar

</div>
</div>

<!--
SPEAKER NOTES — transports.
Dos opciones de transporte. StdioMcpHost es lo que usas para servidores de la comunidad — la mayoría están distribuidos como paquetes npx o uvx, y stdio es la forma de menor denominador común para hablar con ellos. Generamos un subproceso, canalizamos JSON-RPC sobre su stdin/stdout, y agregamos gestión de ciclo de vida — health checks, reinicio automático en crash, logs estructurados desde stderr. InProcessMcpHost es una optimización para nuestros servidores incluidos: mismo protocolo en el wire, pero el wire es un pipe en memoria en lugar de un pipe del SO. Overhead de serialización cero, más fácil establecer breakpoints. La abstracción del host significa que el resto del sistema no se preocupa cuál usa un servidor.
-->

---

## Ciclo de vida: `McpServerLifecycleService`

```
start  ──►  initialize handshake  ──►  list tools
   │                │                       │
   │                ▼                       ▼
   │        capabilities cached     register in IToolRegistry
   │
   └──►  health pings  ──►  restart on failure
```

- `IHostedService` en segundo plano (amigable con Aspire)
- Estado por servidor: `Stopped | Starting | Running | Failed`
- Expuesto en página `/mcp-settings`, con **Restart** de un clic

<!--
SPEAKER NOTES — lifecycle.
McpServerLifecycleService es un hosted service que corre en segundo plano. En el inicio de la app itera definiciones de servidor MCP registradas, genera cada una a través del host elegido, realiza el handshake de initialize de MCP, lista sus tools, cachea capacidades, y registra cada tool en el IToolRegistry unificado. Después de eso hace ping-polls a cada servidor por salud y reinicia los crashed con backoff exponencial. El estado de cada servidor se expone en la página de UI /mcp-settings con un botón de reinicio de un clic — realmente útil cuando estás iterando en un servidor local durante el desarrollo.
-->

---

## `McpToolOverride` — tu última línea de defensa

```csharp
public sealed record McpToolOverride(
    string ServerId,
    string ToolName,
    string? RenamedTo = null,
    string? RewrittenDescription = null,
    bool ForceApproval = false,
    bool Disabled = false);
```

- Renombra un tool (evita colisiones entre servidores)
- Reescribe la descripción (mejor grounding para tu modelo)
- **Fuerza aprobación** incluso si el servidor dice que es seguro
- Deshabilita un tool por completo sin tocar el servidor

<!--
SPEAKER NOTES — overrides.
Slide crítica para producción. Cuando dependes de servidores MCP de terceros no controlas sus definiciones de tools o su idea de lo que es seguro. McpToolOverride es el parche de política que aplicas localmente. Puedes renombrar tools para evitar colisiones cuando dos servidores exponen un tool "search". Puedes reescribir la descripción para enseñarle a tu modelo cuándo usar el tool — "usa esto solo para documentos en /var/data, NO para archivos del sistema". Puedes voltear ForceApproval a true incluso si el servidor dice que no se necesita aprobación — por ejemplo, un servidor MCP de base de datos puede marcar "select" como seguro pero quieres un humano en el loop para cualquier consulta de producción. Y puedes deshabilitar completamente un tool en el que no confías sin desinstalar el servidor. Defensa en profundidad.
-->

---

## Secretos: `DpapiSecretStore`

- Credenciales por servidor nunca en texto plano
- DPAPI en Windows, shims de `libsecret` / Keychain en Linux/macOS
- Almacenado junto al `McpServerDefinition` en SQLite (blob encriptado)
- Desencriptado **solo** cuando el servicio de ciclo de vida genera el subproceso

```csharp
var token = await _secrets.GetAsync(serverId, "GITHUB_TOKEN", ct);
process.StartInfo.EnvironmentVariables["GITHUB_TOKEN"] = token;
```

<!--
SPEAKER NOTES — secrets.
Muchos servidores MCP necesitan credenciales — un PAT de GitHub, un string de conexión de Azure, una clave API de Notion. Los almacenamos vía DpapiSecretStore: DPAPI en Windows porque es la API nativa del SO para "encripta esto para que solo este usuario en esta máquina pueda desencriptar", con shims a libsecret y Keychain en las otras plataformas. El blob encriptado vive en SQLite junto a la definición del servidor, pero solo se desencripta en el momento en que generamos el subproceso e inyectamos el valor en sus variables de entorno. Nada loggeado, nada renderizado a la UI, nada en memoria del proceso más tiempo del necesario.
-->

---

## Descubribilidad — `/mcp-settings`

Tres páginas, en orden creciente de utilidad:

1. **Index** — listar / iniciar / parar / reiniciar tus servidores
2. **Edit** — definición, transporte, vars de env, secretos, overrides
3. **Suggestions** — catálogo curado, **instalación de un clic**

> Respaldado por `McpSuggestionsProvider` + `McpRegistryClient` (consulta el registro MCP público).

<!--
SPEAKER NOTES — UI surface.
Tres páginas de UI manejan la superficie MCP. La página Index te muestra cada servidor, su estado actual, y te da controles de iniciar/parar/reiniciar. La página Edit es donde configuras un servidor individual: su definición (comando + args), el transporte, variables de entorno, secretos, y cualquier override de tool. La tercera página es la mágica — Suggestions, respaldada por McpSuggestionsProvider y McpRegistryClient, que consulta el registro MCP público y te da un catálogo curado, instalable con un clic. ¿Servidor comunitario nuevo publicado ayer? Aparece aquí hoy.
-->

---

## 🤖 Momento Copilot — convertir comando de instalación a definición

> "Aquí está el comando de instalación del README del servidor MCP de GitHub:
> `npx -y @modelcontextprotocol/server-github`.
> Convierte esto en un JSON de `McpServerDefinition` para OpenClawNet, incluyendo las variables de entorno requeridas."

- Copilot lee `McpServerDefinition.cs` para el schema
- Genera JSON listo para pegar en `/mcp-settings/edit`
- Resalta cualquier secreto faltante

<!--
SPEAKER NOTES — Copilot moment 2.
El punto de fricción con MCP siempre es: el README dice "ejecuta este comando", tienes que traducir eso al formato de configuración de tu host. Copilot hace esta conversión realmente bien si le das el schema. Abre McpServerDefinition.cs en tu editor, pega el comando de instalación de cualquier README de MCP, pídele a Copilot que convierta. Produce JSON listo para pegar en la página Edit. Bonus: usualmente señalará las variables de env que necesitan secretos, para que no pegues un token en un archivo de configuración por accidente.
-->

---

# 🛡️  Etapa 3 — Seguridad en Ambas Superficies

<!--
SPEAKER NOTES — Stage 3 divider.
Ambas superficies de tools comparten las mismas primitivas de seguridad. Esta etapa de 10 minutos cubre los tres ataques contra los que DEBES defenderte, y cómo los mismos patrones aplican a ITool y MCP.
-->

---

## 3 ataques que todo framework de tools debe bloquear

1. **Path traversal** — `..\..\windows\system32\config\sam`
2. **Command injection** — `ls; rm -rf /`
3. **SSRF** — `http://169.254.169.254/latest/meta-data/`

> El modelo **GENERARÁ** estos si un usuario pregunta. No confíes en ninguna entrada.

<!--
SPEAKER NOTES — attacks.
Hay tres categorías de ataque que debes planear desde el día uno. Path traversal — el modelo eventualmente intentará leer o escribir fuera del sandbox si un usuario elabora el prompt correcto. Command injection — misma historia para comandos shell, separados por punto y coma o backticks. Y SSRF, server-side request forgery — fetching de endpoints de metadata de nube, servicios internos, puertos localhost. El modelo no es malicioso; es servicial, lo que significa que con gusto intentará CUALQUIER URL o path que el usuario pregunte. Tus tools tienen que asumir que toda entrada es hostil.
-->

---

## FileSystem — mata el traversal en tiempo de resolución

```csharp
var fullPath = Path.GetFullPath(Path.Combine(_root, requestedPath));
if (!fullPath.StartsWith(_root, StringComparison.OrdinalIgnoreCase))
    return ToolResult.Fail(Name, "path escape", elapsed);
```

- Resuelve **antes** de abrir el archivo
- Compara contra la raíz canónica del sandbox
- Mismo patrón en `FileSystemTool` **y** `FileSystemMcpTools`

<!--
SPEAKER NOTES — filesystem.
El patrón de defensa para path traversal es canonicalizar-luego-comparar. Path.GetFullPath resuelve todos los .., los enlaces simbólicos, las barras redundantes — y te da el path absoluto que el SO realmente abriría. LUEGO comparas contra la raíz canónica del sandbox. Si el path resuelto no comienza con tu raíz, niega. La razón por la que esto funciona donde checks de strings más simples fallan: un atacante astuto puede escribir \\?\C:\Windows o usar trucos de normalización Unicode, y Path.GetFullPath colapsará todo eso al path real antes de que hagas la comparación. Tanto nuestro FileSystemTool en proceso como el servidor MCP FileSystem usan este patrón exacto.
-->

---

## Shell — blocklist + timeout + aprobación

```csharp
private static readonly string[] Blocked = ["rm", "del", "format", "shutdown", "reg"];
if (Blocked.Any(b => command.StartsWith(b, ...)))
    return ToolResult.Fail(...);

using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
cts.CancelAfter(TimeSpan.FromSeconds(30));
```

- La blocklist es un **piso**, no un techo
- El timeout protege contra procesos desbocados
- `RequiresApproval = true` significa que el usuario **siempre** ve el comando primero

<!--
SPEAKER NOTES — shell.
El tool shell obtiene TRES capas porque es la superficie de mayor riesgo. Primero, una blocklist — rm, del, format, shutdown, reg, taskkill — estos son los verbos que no se pueden deshacer. La blocklist es un PISO no un techo: cubre los peligros obvios, aún deberías aplicar políticas más restrictivas en producción. Segundo, un timeout duro: treinta segundos, sin excepciones, el token de cancelación mata el proceso. Tercero, RequiresApproval está hardcodeado a true — el usuario ve el comando exacto que está a punto de ejecutarse y tiene que hacer clic en aprobar. Si tu prompt dice "ejecuta npm install" y el modelo decide ejecutar algo más, lo verás antes de que se ejecute.
-->

---

## Web — bloquea SSRF antes de que el socket se abra

```csharp
var uri = new Uri(url);
if (IPAddress.TryParse(uri.Host, out var ip) && IsPrivate(ip))
    return ToolResult.Fail(Name, "private IP blocked", elapsed);
if (uri.Host is "localhost" or "metadata.google.internal" or "169.254.169.254")
    return ToolResult.Fail(Name, "blocked host", elapsed);
```

- Resuelve y verifica **antes** de que el cliente HTTP abra un socket
- Defiende contra robo de datos IMDS de EC2 / GCE / Azure

<!--
SPEAKER NOTES — web.
La defensa SSRF se ejecuta ANTES de que el socket se abra. Parseamos el URI, y si el host es una IP de rango privado — 10.x, 172.16-31.x, 192.168.x, 127.x, link-local — negamos. Igual para los hosts de metadata de nube bien conocidos: localhost, metadata.google.internal, 169.254.169.254 — esos filtrarían credenciales de instancia en segundos si fueran alcanzables. La palabra crucial es "antes". Si difieren este check hasta después de que la conexión se complete, ya filtraste un paquete a un servicio interno que puede loggearlo. Hacemos el check en tiempo de parseo de URL.
-->

---

## Tipos de eventos NDJSON — iguales para ambas superficies

```jsonl
{"type":"ToolApprovalRequest","tool":"shell","args":{"cmd":"npm install"}}
{"type":"ToolCallStart","tool":"shell","callId":"abc123"}
{"type":"ToolCallComplete","tool":"shell","callId":"abc123","durationMs":4210}
{"type":"ContentDelta","text":"Installed 247 packages..."}
```

- La UI no se preocupa si el tool fue `ITool` o MCP
- El usuario obtiene una línea de tiempo consistente
- Los spans de OpenTelemetry etiquetan `tool.source` para filtrado

<!--
SPEAKER NOTES — events.
Sea cual sea la superficie, el usuario ve los mismos eventos. ToolApprovalRequest es lo que la UI escucha para renderizar un diálogo de aprobación. ToolCallStart marca el momento en que comienza la ejecución para que la UI pueda mostrar un spinner. ToolCallComplete lleva duración y el resultado para que podamos loggear timing y renderizar output. ContentDelta es texto en stream normal. Cada uno de estos eventos es idéntico para tools en proceso y MCP — el usuario no puede notar la diferencia, que es exactamente lo que queremos. En tus trazas, OpenTelemetry etiqueta cada span de tool con tool.source para que puedas filtrar por superficie al depurar.
-->

---

# 🔄  Etapa 4 — El Loop del Agente

<!--
SPEAKER NOTES — Stage 4 divider.
Ahora juntamos todo. El loop del agente es el latido que convierte una sola llamada al LLM en comportamiento multi-turno, que usa tools.
-->

---

## Qué hace realmente un loop de agente

```
1. Componer prompt + historial de chat + manifiesto de tools
2. Llamar al modelo → obtener mensaje del asistente (texto y/o llamadas a tools)
3. Si no hay llamadas a tools → listo, devolver respuesta
4. Para cada llamada a tool:
       a. Puerta de aprobación
       b. Ejecutar vía IToolExecutor
       c. Agregar resultado del tool a mensajes
5. Goto 2 (cap a N iteraciones)
```

> Todos los frameworks de agentes modernos son una variante de este loop.

<!--
SPEAKER NOTES — the loop.
Este es el loop, cada framework de agente que hayas escuchado es una variante de esto. Componer un prompt — instrucciones del sistema más historial de chat más el manifiesto de tools. Llamar al modelo, obtener de vuelta un mensaje que puede contener texto, llamadas a tools, o ambos. Si no hay llamadas a tools, terminaste — devuelve el texto. De lo contrario, para cada llamada a tool, ejecuta la puerta de aprobación, ejecuta a través del executor, agrega el resultado de vuelta en la lista de mensajes, y llama al modelo otra vez. El cap en iteraciones existe para que un modelo que se comporta mal no pueda ponerte en un loop infinito hacia una factura de tokens del infierno. Cinco líneas de pseudocódigo, pero todo el campo de "AI agéntica" es solo ingeniería alrededor de este esqueleto.
-->

---

## `DefaultAgentRuntime` — el motor

```csharp
public async IAsyncEnumerable<AgentStreamEvent> ExecuteStreamAsync(
    AgentContext ctx,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    for (var i = 0; i < _maxIterations; i++)
    {
        var response = await _client.GetResponseAsync(messages, opts, ct);

        foreach (var ev in StreamContent(response)) yield return ev;
        if (response.ToolCalls.Count == 0) break;

        foreach (var call in response.ToolCalls)
        {
            yield return new ToolCallStartEvent(call);
            var result = await _executor.ExecuteAsync(call, ct);
            yield return new ToolCallCompleteEvent(call, result);
            messages.Add(ToToolMessage(call, result));
        }
    }
}
```

<!--
SPEAKER NOTES — DefaultAgentRuntime.
Esta es una versión simplificada del método real — el código de producción agrega telemetría, sumarización, seguimiento de presupuesto — pero los huesos están aquí. El for-loop es el cap de iteraciones. Llamamos al modelo, hacemos stream de cualquier contenido, verificamos llamadas a tools. Si no hay ninguna, hacemos break y terminamos. Si hay llamadas a tools, hacemos yield de un ToolCallStartEvent para que la UI pueda reaccionar, ejecutamos el executor — que maneja la puerta de aprobación y timing — hacemos yield de un ToolCallCompleteEvent, y agregamos el resultado a la lista de mensajes para que el modelo lo vea en la siguiente iteración. Nota messages.Add en el fondo — eso es lo que cierra el loop. El modelo ve su propia llamada a tool y el resultado, y decide qué hacer con esa información.
-->

---

## Cómo los tools entran en el prompt

```
IToolRegistry  ─►  GetAllTools()
                       │
                       ▼
   ┌──────── in-process ITool ─────────┐
   │  GreeterTool, FileSystemTool, ... │
   └───────────────────────────────────┘
   ┌────────── MCP tool wrapper ───────┐
   │  fs.read_file, web.fetch, ...     │
   └───────────────────────────────────┘
                       │
                       ▼
        Convert to AIFunction[]
                       │
                       ▼
   new ChatOptions { Tools = [...] }  ──► model
```

<!--
SPEAKER NOTES — tool manifest.
Donde las dos superficies realmente se fusionan. IToolRegistry.GetAllTools devuelve un IEnumerable&lt;ITool&gt; que contiene ambas superficies — las implementaciones en proceso directamente, y los tools MCP envueltos en un adaptador que satisface ITool pero delega al host MCP por debajo. Luego convertimos cada uno a un AIFunction de Microsoft.Extensions.AI, construimos un objeto ChatOptions con Tools = ese array, y lo pasamos al modelo. Desde aquí, todo es M.E.AI estándar — el modelo elige tools, los llama por nombre, el executor los ejecuta. El modelo nunca ve la distinción de superficie; nunca tenemos que escribir dos paths de código.
-->

---

# 🧪  Etapa 5 — Demos

<!--
SPEAKER NOTES — Stage 5 divider.
Cinco demos ejecutables. Los primeros tres son demos sin LLM que muestran primitivas del framework. Los últimos dos necesitan Ollama y ejercitan el loop del agente end-to-end con ambas superficies de tools.
-->

---

## Los 5 demos de un vistazo

| # | Demo | Qué muestra | LLM? |
|---|------|--------------|------|
| 1 | `demo1-tool` | `ITool` personalizado, metadata, schema | ❌ |
| 2 | `demo2-approval` | Intercambio de `IToolApprovalPolicy` | ❌ |
| 3 | `demo3-agent-loop` | Ollama + tools `AIFunction` | ✅ |
| **4** | **`demo4-mcp-stdio`** | **Conectar un servidor MCP, llamar su tool** | ❌ |
| **5** | **`demo5-hybrid`** | **Un agente, un ITool + un tool MCP** | ✅ |

```pwsh
$env:NUGET_PACKAGES = "$env:USERPROFILE\.nuget\packages2"
dotnet run --project docs\sessions\session-2\code\demo5-hybrid
```

<!--
SPEAKER NOTES — demos overview.
Cinco demos en el repo. Del uno al tres los viste la última vez — bloques de construcción. El Demo 4 es nuevo — muestra cómo hacer spin up de un servidor MCP en proceso e invocar un tool a través de él SIN un LLM, para que puedas aislar "¿funciona el wiring de MCP?" de "¿elige el modelo el tool correcto?". El Demo 5 es el showstopper: un agente, un ITool y un tool MCP, y el modelo decide cuál usar. Ese es todo el pitch de esta sesión en un solo archivo de 200 líneas.
-->

---

## Demo 4 — Conectar un servidor MCP (stdio)

```csharp
var def = new McpServerDefinition(
    Id: "fs-demo",
    Name: "FileSystem (npx)",
    Transport: McpTransport.Stdio,
    Command: "npx",
    Args: ["-y", "@modelcontextprotocol/server-filesystem", "./sandbox"]);

await using var host = new StdioMcpHost(def, logger);
await host.StartAsync(ct);

var tools = await host.ListToolsAsync(ct);
foreach (var t in tools) Console.WriteLine($"  • {t.Name} — {t.Description}");

var result = await host.CallToolAsync("read_file",
    new { path = "README.md" }, ct);
Console.WriteLine(result.Content);
```

<!--
SPEAKER NOTES — demo 4.
El Demo 4 es la demo MCP más limpia posible. Definimos un McpServerDefinition apuntando al servidor MCP oficial de filesystem distribuido como un paquete npm — uno de docenas de servidores de la comunidad que "simplemente funcionan". StdioMcpHost genera el subproceso, realiza el handshake de initialize de MCP. Luego listamos los tools que el servidor expone, y llamamos uno de ellos por nombre con un objeto de argumentos JSON. Sin modelo en el loop todavía — esto es simplemente "¿está funcionando el wiring?". Ejecuta esto para convencerte de que el transporte stdio funciona, luego ve al demo 5 para agregar el modelo.
-->

---

## Demo 5 — Agente híbrido (`ITool` + MCP)

```csharp
services.AddToolFramework();
services.AddTool<CalculatorTool>();           // in-process

services.AddOpenClawMcp();
services.AddMcpServerDefinition(new McpServerDefinition(
    Id: "fs-demo", Transport: McpTransport.Stdio,
    Command: "npx", Args: ["-y", "@modelcontextprotocol/server-filesystem", "./sandbox"]));

services.AddSingleton<IAgentProvider, OllamaAgentProvider>();
```

> Luego pregunta: *"Lee sandbox/numbers.txt, suma los valores, devuelve el promedio."*

El modelo elige `fs-demo.read_file` luego `calculator` luego responde. **Un loop, dos superficies.**

<!--
SPEAKER NOTES — demo 5.
El Demo 5 es la recompensa. Registramos un tool en proceso (Calculator), agregamos el framework MCP, conectamos el servidor MCP de filesystem. Le damos al agente un prompt que REQUIERE ambas superficies: leer un archivo usando el tool MCP, luego hacer matemáticas usando el ITool, luego devolver el resultado. El modelo no tiene idea de que uno vino de código C# y uno de un subproceso Node.js. Solo ve "fs-demo.read_file" y "calculator" en su manifiesto y los elige en el orden correcto. Cuando veas los logs verás la puerta de aprobación dispararse para cualquier tool que señalaste, los eventos en forma NDJSON, y la respuesta final. Esa es la arquitectura — colapsada en un archivo ejecutable.
-->

---

## Yendo más allá — templates integrados

`/jobs/templates` viene con recetas de un clic:

- 📂 **Watched folder summarizer** — cada 5 min, resume docs nuevos
- 🌐 **Daily site digest** — fetch, extrae, resume, email
- 📰 **Inbox triage** — IMAP + clasificar + mover
- ⏰ **Cron meets agent** — ejecuta un agente en un schedule

> Todo construido sobre el mismo `IToolExecutor` que acabas de aprender.

<!--
SPEAKER NOTES — templates.
Incluimos una página de Templates en la UI con recetas de un clic para los patrones de agente más comunes. Watched folder summarizer es el que recorremos en docs/demos/tools — cada 5 minutos escanea una carpeta, convierte docs a markdown, resume. Daily site digest, inbox triage, agentes impulsados por cron — todos están construidos sobre el mismo IToolExecutor que acabas de ver. El punto de la página de templates es que la gente no comience desde un canvas en blanco; hacen fork de una receta que funciona.
-->

---

# 🎯  Hacia dónde vamos después

- **Sesión 3** — Memoria, sumarización, y presupuestos de conversación
- **Sesión 4** — Multi-agente: orquestador + trabajadores + handoff
- **Bonus** — Hardening de producción: secretos, telemetría, rate limits

> Hoy: tools en los que confías. Después: un agente que **recuerda**.

<!--
SPEAKER NOTES — what's next.
La Sesión 3 trata de memoria — hemos estado guardando cada mensaje en una lista todo este tiempo, que está bien hasta que tu conversación golpea la ventana de contexto del modelo. Sumarización, presupuestos de conversación, memoria vectorial — todo en la próxima sesión. La Sesión 4 va multi-agente: un orquestador que entrega trabajo a trabajadores especializados. La sesión bonus es hardening de producción: gestión de secretos, telemetría, rate limits, las cosas aburridas que convierten una demo en un deployment.
-->

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

`elbruno/openclawnet` · Licencia MIT · contribuciones bienvenidas
`docs/sessions/session-2/` para todo lo de hoy

<!--
SPEAKER NOTES — closing.
Gracias a todos. El repo es github.com/elbruno/openclawnet, licencia MIT, contribuciones muy bienvenidas. Todo lo de hoy — slides, demos, walkthrough — vive bajo docs/sessions/session-2/. Si quieres seguir, el demo 5 es el más divertido de extender: intenta reemplazar el tool Calculator con un tool Weather que llame una API real, o intercambia el servidor MCP de filesystem por el de GitHub y pregúntale al agente sobre tus repos. ¿Preguntas?
-->
