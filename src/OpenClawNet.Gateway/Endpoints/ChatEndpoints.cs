using OpenClawNet.Agent;
using OpenClawNet.Gateway.Services;
using OpenClawNet.Models.Abstractions;
using OpenClawNet.Storage;

namespace OpenClawNet.Gateway.Endpoints;

public static class ChatEndpoints
{
    public static void MapChatEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/chat").WithTags("Chat");
        
        group.MapPost("/", async (
            ChatMessageRequest request,
            IAgentOrchestrator orchestrator,
            IAgentProfileStore profileStore,
            ILogger<Program> logger,
            HttpContext httpContext) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Results.BadRequest(new { error = "Message is required and cannot be empty." });
            }

            try
            {
                // Resolve agent profile: explicit name → default profile
                AgentProfile? profile = null;
                if (!string.IsNullOrEmpty(request.AgentProfileName))
                {
                    profile = await profileStore.GetAsync(request.AgentProfileName);
                    // Guard: chat must use Standard profiles only.
                    if (profile is not null && profile.Kind != ProfileKind.Standard)
                    {
                        profile = null;
                    }
                }
                profile ??= await profileStore.GetDefaultAsync();

                // Resolve provider: profile.Provider or request.Provider → definition → global fallback
                var resolver = httpContext.RequestServices.GetService<ProviderResolver>();
                var resolvedProvider = resolver is not null
                    ? await resolver.ResolveAsync(request.Provider ?? profile.Provider)
                    : null;

                // Sync RuntimeModelSettings so the runtime uses the resolved endpoint/apiKey
                // instead of the global default. Acceptable for this single-user demo app.
                if (resolvedProvider?.DefinitionName is not null)
                {
                    var runtimeSettings = httpContext.RequestServices.GetService<RuntimeModelSettings>();
                    if (runtimeSettings is not null)
                    {
                        runtimeSettings.Update(new ModelProviderConfig
                        {
                            Provider = resolvedProvider.ProviderType,
                            Model = string.IsNullOrWhiteSpace(resolvedProvider.Model) ? null : resolvedProvider.Model,
                            Endpoint = resolvedProvider.Endpoint,
                            ApiKey = resolvedProvider.ApiKey,
                            DeploymentName = string.IsNullOrWhiteSpace(resolvedProvider.DeploymentName) ? null : resolvedProvider.DeploymentName,
                            AuthMode = resolvedProvider.AuthMode,
                        });
                    }
                }

                var agentRequest = new AgentRequest
                {
                    SessionId = request.SessionId,
                    UserMessage = request.Message,
                    Model = request.Model ?? resolvedProvider?.Model,
                    Provider = resolvedProvider?.ProviderType ?? request.Provider ?? profile.Provider,
                    AgentProfileName = profile.Name,
                    AgentProfileInstructions = profile.Instructions,
                    ResolvedProvider = resolvedProvider
                };
                
                var response = await orchestrator.ProcessAsync(agentRequest);
                
                return Results.Ok(new ChatMessageResponse
                {
                    Content = response.Content,
                    ToolCallCount = response.ToolCallCount,
                    TotalTokens = response.TotalTokens
                });
            }
            catch (ModelProviderUnavailableException ex)
            {
                logger.LogError(ex, "Model provider '{Provider}' is unavailable for chat request", ex.ProviderName);
                return Results.Json(
                    new { error = ex.Message },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "HTTP error communicating with model provider for chat request");
                return Results.Json(
                    new { error = "Model provider is unavailable. Please check that the provider is running." },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("SendChatMessage")
        .WithDescription("Send a message and get a response");
        
        group.MapPost("/{id}/auto-rename", async (
            Guid id,
            IConversationStore conversationStore,
            ChatNamingService namingService,
            ILogger<Program> logger) =>
        {
            try
            {
                var session = await conversationStore.GetSessionAsync(id);
                if (session is null)
                {
                    return Results.NotFound(new { error = $"Chat session {id} not found" });
                }

                var messages = await conversationStore.GetMessagesAsync(id);
                if (messages.Count == 0)
                {
                    return Results.BadRequest(new { error = "Cannot auto-rename empty chat" });
                }

                var newName = await namingService.GenerateChatNameAsync(id, messages);
                var updatedSession = await conversationStore.UpdateSessionTitleAsync(id, newName);

                return Results.Ok(new AutoRenameResponse
                {
                    Success = true,
                    NewName = updatedSession.Title
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error auto-renaming chat {ChatId}", id);
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("AutoRenameChat")
        .WithDescription("Auto-generate and set a chat name based on conversation content");
    }
}

public sealed record ChatMessageRequest
{
    public Guid SessionId { get; init; }
    public required string Message { get; init; }
    public string? Model { get; init; }
    public string? Provider { get; init; }
    public string? AgentProfileName { get; init; }
}

public sealed record ChatMessageResponse
{
    public required string Content { get; init; }
    public int ToolCallCount { get; init; }
    public int TotalTokens { get; init; }
}

public sealed record AutoRenameResponse
{
    public required bool Success { get; init; }
    public required string NewName { get; init; }
}
