using Microsoft.EntityFrameworkCore;
using OpenClawNet.Storage.Entities;

namespace OpenClawNet.Storage;

public sealed class ConversationStore : IConversationStore
{
    private readonly IDbContextFactory<OpenClawDbContext> _contextFactory;
    
    public ConversationStore(IDbContextFactory<OpenClawDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }
    
    public async Task<ChatSession> CreateSessionAsync(string? title = null, CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var session = new ChatSession { Title = title ?? "New Chat" };
        db.Sessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return session;
    }
    
    public async Task<ChatSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Sessions
            .Include(s => s.Messages.OrderBy(m => m.OrderIndex))
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
    }
    
    public async Task<IReadOnlyList<ChatSession>> ListSessionsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Sessions
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var session = await db.Sessions.FindAsync([sessionId], cancellationToken);
        if (session is not null)
        {
            db.Sessions.Remove(session);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
    
    public async Task<ChatSession> UpdateSessionTitleAsync(Guid sessionId, string title, CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var session = await db.Sessions.FindAsync([sessionId], cancellationToken)
            ?? throw new InvalidOperationException($"Session {sessionId} not found");
        session.Title = title;
        session.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return session;
    }
    
    public async Task<ChatMessageEntity> AddMessageAsync(Guid sessionId, string role, string content, string? name = null, string? toolCallId = null, string? toolCallsJson = null, CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        
        var maxOrder = await db.Messages
            .Where(m => m.SessionId == sessionId)
            .MaxAsync(m => (int?)m.OrderIndex, cancellationToken) ?? -1;
        
        var message = new ChatMessageEntity
        {
            SessionId = sessionId,
            Role = role,
            Content = content,
            Name = name,
            ToolCallId = toolCallId,
            ToolCallsJson = toolCallsJson,
            OrderIndex = maxOrder + 1
        };
        
        db.Messages.Add(message);
        
        // Update session timestamp
        var session = await db.Sessions.FindAsync([sessionId], cancellationToken);
        if (session is not null)
        {
            session.UpdatedAt = DateTime.UtcNow;
        }
        
        await db.SaveChangesAsync(cancellationToken);
        return message;
    }
    
    public async Task<IReadOnlyList<ChatMessageEntity>> GetMessagesAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Messages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.OrderIndex)
            .ToListAsync(cancellationToken);
    }
}
