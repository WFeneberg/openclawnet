# Provider Model

## Interface

All model providers implement `IModelClient`:

```csharp
public interface IModelClient
{
    string ProviderName { get; }
    Task<ChatResponse> CompleteAsync(ChatRequest request, CancellationToken ct);
    IAsyncEnumerable<ChatResponseChunk> StreamAsync(ChatRequest request, CancellationToken ct);
    Task<bool> IsAvailableAsync(CancellationToken ct);
}
```

## Provider Configuration

### Ollama (Default)
```json
{
  "Model": {
    "Provider": "ollama",
    "Model": "llama3.2",
    "Endpoint": "http://localhost:11434"
  }
}
```

### Azure OpenAI
```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com",
    "ApiKey": "your-api-key",
    "DeploymentName": "gpt-4o"
  }
}
```

### Foundry
```json
{
  "Foundry": {
    "Endpoint": "https://your-foundry-endpoint.inference.ai.azure.com",
    "ApiKey": "your-api-key",
    "Model": "your-model-name"
  }
}
```

## Switching Providers

Provider selection is configured at startup via DI. The Gateway's `Program.cs` registers the desired provider:

```csharp
// Local (default)
builder.Services.AddOllama();

// Azure OpenAI
builder.Services.AddAzureOpenAI(options => { ... });

// Foundry
builder.Services.AddFoundry(options => { ... });
```

Future: Runtime provider switching via settings API.
