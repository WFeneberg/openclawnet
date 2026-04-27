using Microsoft.Extensions.Logging;
using OpenClawNet.Models.Abstractions;
using OpenClawNet.Storage.Entities;

namespace OpenClawNet.Gateway.Services;

/// <summary>
/// Service for auto-generating chat names using LLM based on conversation context.
/// </summary>
public sealed class ChatNamingService
{
    private readonly IModelClient _modelClient;
    private readonly ILogger<ChatNamingService> _logger;

    public ChatNamingService(IModelClient modelClient, ILogger<ChatNamingService> logger)
    {
        _modelClient = modelClient;
        _logger = logger;
    }

    /// <summary>
    /// Generates a descriptive name for a chat session based on the last messages.
    /// </summary>
    /// <param name="chatId">The chat session ID (used for fallback naming).</param>
    /// <param name="messages">The list of chat messages to analyze (will use last 5-10).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A generated chat name, or a fallback name if LLM fails.</returns>
    public async Task<string> GenerateChatNameAsync(
        Guid chatId,
        IReadOnlyList<ChatMessageEntity> messages,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use last 5-10 messages for context
            var contextMessages = messages
                .OrderByDescending(m => m.OrderIndex)
                .Take(10)
                .OrderBy(m => m.OrderIndex)
                .ToList();

            if (contextMessages.Count == 0)
            {
                _logger.LogWarning("No messages available for chat {ChatId}, using fallback name", chatId);
                return GenerateFallbackName(chatId);
            }

            // Format messages as conversation context
            var conversationText = string.Join("\n", contextMessages.Select(m =>
                $"{m.Role}: {(m.Content?.Length > 200 ? m.Content[..200] + "..." : m.Content)}"
            ));

            // Call LLM with focused prompt
            var request = new ChatRequest
            {
                Messages = new List<ChatMessage>
                {
                    new()
                    {
                        Role = ChatMessageRole.System,
                        Content = "You are a helpful assistant that generates concise, descriptive names for conversations. " +
                                 "Generate a 5-8 word descriptive name for the conversation below. " +
                                 "Return ONLY the name, no quotes, no explanation, no newlines."
                    },
                    new()
                    {
                        Role = ChatMessageRole.User,
                        Content = $"Here is the conversation to name:\n\n{conversationText}"
                    }
                }
            };

            var response = await _modelClient.CompleteAsync(request, cancellationToken);
            var generatedName = response.Content?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(generatedName))
            {
                _logger.LogWarning("LLM returned empty response for chat {ChatId}, using fallback name", chatId);
                return GenerateFallbackName(chatId);
            }

            // Ensure name doesn't exceed reasonable length
            if (generatedName.Length > 100)
            {
                generatedName = generatedName[..100];
            }

            _logger.LogInformation("Generated chat name for {ChatId}: {ChatName}", chatId, generatedName);
            return generatedName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate chat name for {ChatId}, using fallback", chatId);
            return GenerateFallbackName(chatId);
        }
    }

    private static string GenerateFallbackName(Guid chatId)
    {
        return $"Chat {DateTime.UtcNow:yyyy-MM-dd HH:mm}";
    }
}
