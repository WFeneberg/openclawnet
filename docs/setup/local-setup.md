# Local Setup Guide

## Prerequisites

1. **.NET 10 SDK** — [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
2. **Ollama** — [Download](https://ollama.ai)
3. **Git** — For cloning the repository

## Quick Start

```bash
# 1. Clone the repository
git clone https://github.com/elbruno/tuis.git
cd tuis

# 2. Install and start Ollama
ollama pull llama3.2

# 3. Run the solution
dotnet run --project src/OpenClawNet.AppHost

# 4. Open browser
# Gateway: https://localhost:7100
# Web UI: https://localhost:7200
# Aspire Dashboard: https://localhost:15100
```

## Configuration

Edit `src/OpenClawNet.Gateway/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=openclawnet.db"
  },
  "Model": {
    "Provider": "ollama",
    "Model": "llama3.2",
    "Endpoint": "http://localhost:11434",
    "Temperature": 0.7,
    "MaxTokens": 4096
  }
}
```

## Troubleshooting

- **Ollama not connecting**: Ensure Ollama is running (`ollama serve`)
- **Model not found**: Pull the model first (`ollama pull llama3.2`)
- **Port conflicts**: Check `Properties/launchSettings.json` for port configuration
- **Database issues**: Delete `openclawnet.db` to reset (it auto-recreates)
