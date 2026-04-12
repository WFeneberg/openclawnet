# Azure OpenAI Setup

## Prerequisites

1. An Azure subscription
2. An Azure OpenAI resource with a deployed model
3. The endpoint URL and API key

## Create Azure OpenAI Resource

1. Go to [Azure Portal](https://portal.azure.com)
2. Create a new **Azure OpenAI** resource
3. Deploy a model (e.g., `gpt-4o`)
4. Copy the **Endpoint** and **API Key** from the resource

## Configuration

Add to `src/OpenClawNet.Gateway/appsettings.json`:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com",
    "ApiKey": "your-api-key",
    "DeploymentName": "gpt-4o",
    "Temperature": 0.7,
    "MaxTokens": 4096
  }
}
```

## Enable in Code

In `Program.cs`, replace the Ollama registration:

```csharp
// Comment out Ollama
// builder.Services.AddModelClient<OllamaModelClient>();

// Add Azure OpenAI
builder.Services.AddAzureOpenAI(options =>
{
    builder.Configuration.GetSection("AzureOpenAI").Bind(options);
});
```

## Security Note

Never commit API keys to source control. Use environment variables or user secrets:

```bash
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-key"
```
