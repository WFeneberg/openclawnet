namespace OpenClawNet.Models.Ollama;

public sealed class OllamaOptions
{
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.2";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 4096;
}
