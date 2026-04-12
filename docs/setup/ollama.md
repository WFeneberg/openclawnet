# Ollama Setup

## Installation

### Windows
Download from [ollama.ai](https://ollama.ai) and run the installer.

### macOS
```bash
brew install ollama
```

### Linux
```bash
curl -fsSL https://ollama.ai/install.sh | sh
```

## Pull a Model

```bash
# Recommended: llama3.2 (default for OpenClawNet)
ollama pull llama3.2

# Alternatives
ollama pull mistral
ollama pull codellama
ollama pull phi3
```

## Verify

```bash
ollama list          # Shows installed models
ollama serve         # Starts the server (usually auto-started)
curl localhost:11434 # Should return "Ollama is running"
```

## OpenClawNet Configuration

The default configuration uses `llama3.2` at `http://localhost:11434`. To change the model, update `appsettings.json`:

```json
{
  "Model": {
    "Model": "mistral",
    "Endpoint": "http://localhost:11434"
  }
}
```
