using OpenClawNet.Models.Abstractions;
using OpenClawNet.Storage;

namespace OpenClawNet.Gateway.Endpoints;

public static class ChatEndpoints
{
    public static void MapChatEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/chat").WithTags("Chat");

        group.MapPost("/", async (ChatMessageRequest request, IModelClient modelClient, IConversationStore store) =>
        {
            // Save user message
            await store.AddMessageAsync(request.SessionId, "user", request.Message);

            // Simple model call — no agent loop, no tools
            var chatRequest = new ChatRequest
            {
                Messages = (await store.GetMessagesAsync(request.SessionId))
                    .Select(m => new ChatMessage { Role = Enum.Parse<ChatMessageRole>(m.Role, true), Content = m.Content })
                    .ToList(),
                Model = request.Model
            };

            var response = await modelClient.CompleteAsync(chatRequest);

            // Save assistant response
            await store.AddMessageAsync(request.SessionId, "assistant", response.Content);

            return Results.Ok(new ChatMessageResponse
            {
                Content = response.Content,
                ToolCallCount = 0,
                TotalTokens = response.Usage?.TotalTokens ?? 0
            });
        })
        .WithName("SendChatMessage")
        .WithDescription("Send a message and get a response");
    }
}

public sealed record ChatMessageRequest
{
    public Guid SessionId { get; init; }
    public required string Message { get; init; }
    public string? Model { get; init; }
    public string? Provider { get; init; }
}

public sealed record ChatMessageResponse
{
    public required string Content { get; init; }
    public int ToolCallCount { get; init; }
    public int TotalTokens { get; init; }
}
