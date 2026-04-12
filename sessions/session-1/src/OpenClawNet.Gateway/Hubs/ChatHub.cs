using Microsoft.AspNetCore.SignalR;
using OpenClawNet.Models.Abstractions;
using OpenClawNet.Storage;

namespace OpenClawNet.Gateway.Hubs;

public sealed class ChatHub : Hub
{
    private readonly IModelClient _modelClient;
    private readonly IConversationStore _store;

    public ChatHub(IModelClient modelClient, IConversationStore store)
    {
        _modelClient = modelClient;
        _store = store;
    }

    public async IAsyncEnumerable<ChatHubMessage> StreamChat(Guid sessionId, string message, string? model = null)
    {
        // Save user message
        await _store.AddMessageAsync(sessionId, "user", message);

        var messages = (await _store.GetMessagesAsync(sessionId))
            .Select(m => new ChatMessage { Role = Enum.Parse<ChatMessageRole>(m.Role, true), Content = m.Content })
            .ToList();

        var request = new ChatRequest { Messages = messages, Model = model };

        var fullContent = "";
        await foreach (var chunk in _modelClient.StreamAsync(request))
        {
            fullContent += chunk.Content;
            yield return new ChatHubMessage("content", chunk.Content ?? "");
        }

        // Save assistant response
        await _store.AddMessageAsync(sessionId, "assistant", fullContent);
        yield return new ChatHubMessage("complete", fullContent);
    }
}

public sealed record ChatHubMessage(string Type, string Content);
