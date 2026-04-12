using OpenClawNet.Storage.Entities;

namespace OpenClawNet.Storage;

public interface IConversationStore
{
    Task<ChatSession> CreateSessionAsync(string? title = null, CancellationToken cancellationToken = default);
    Task<ChatSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatSession>> ListSessionsAsync(CancellationToken cancellationToken = default);
    Task DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<ChatSession> UpdateSessionTitleAsync(Guid sessionId, string title, CancellationToken cancellationToken = default);
    
    Task<ChatMessageEntity> AddMessageAsync(Guid sessionId, string role, string content, string? name = null, string? toolCallId = null, string? toolCallsJson = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatMessageEntity>> GetMessagesAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
