namespace OpenClawNet.Models.Abstractions;

public sealed record ChatRequest
{
    public required string Model { get; init; }
    public required IReadOnlyList<ChatMessage> Messages { get; init; }
    public IReadOnlyList<ToolDefinition>? Tools { get; init; }
    public double? Temperature { get; init; }
    public int? MaxTokens { get; init; }
}
