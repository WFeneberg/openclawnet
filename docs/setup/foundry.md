# Microsoft Foundry Setup

## Overview

Microsoft Foundry provides hosted model endpoints with an OpenAI-compatible API. OpenClawNet can connect to any Foundry-hosted model that exposes the chat completions endpoint.

## Prerequisites

1. A Microsoft Foundry workspace
2. A deployed model endpoint
3. The endpoint URL and API key

## Configuration

Add to `src/OpenClawNet.Gateway/appsettings.json`:

```json
{
  "Foundry": {
    "Endpoint": "https://your-model.inference.ai.azure.com",
    "ApiKey": "your-api-key",
    "Model": "your-model-name",
    "Temperature": 0.7,
    "MaxTokens": 4096
  }
}
```

## Enable in Code

In `Program.cs`:

```csharp
builder.Services.AddFoundry(options =>
{
    builder.Configuration.GetSection("Foundry").Bind(options);
});
```

## Supported Models

Any model hosted on Microsoft Foundry that exposes the OpenAI-compatible `/chat/completions` endpoint should work, including:
- Meta Llama models
- Mistral models
- Phi models
- Cohere models
