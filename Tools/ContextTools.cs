using System.ComponentModel;
using McpVersionVer2.Models;
using McpVersionVer2.Services;
using McpVersionVer2.Security;
using ModelContextProtocol.Server;
using static McpVersionVer2.Services.AppJsonSerializerOptions;
using Microsoft.Extensions.Logging;

namespace McpVersionVer2.Tools;

[McpServerToolType]
public class ContextTools
{
    private readonly IConversationContextService _contextService;
    private readonly RequestContextService _requestContext;
    private readonly ILogger<ContextTools> _logger;

    public ContextTools(
        IConversationContextService contextService,
        RequestContextService requestContext,
        ILogger<ContextTools> logger)
    {
        _contextService = contextService;
        _requestContext = requestContext;
        _logger = logger;
    }

    private string GetSessionId()
    {
        return _requestContext.SessionId;
    }

    [McpServerTool, Description("Get recent conversation context for AI to maintain continuity. Returns last N messages from rolling window.")]
    public async Task<string> GetConversationContext(
        [Description("Bearer token")] string bearerToken)
    {
        try
        {
            RequestContextService.SetToken(bearerToken);
            
            var sessionId = GetSessionId();
            var messages = _contextService.GetRecentMessages(sessionId);
            var formattedContext = _contextService.GetFormattedContext(sessionId);

            var contextJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                sessionId = sessionId,
                messageCount = messages.Count,
                context = formattedContext,
                timestamp = DateTime.UtcNow
            }, Default);

            _logger.LogInformation("Retrieved context for session {Session}: {Count} messages", 
                sessionId, messages.Count);

            return contextJson;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversation context");
            return System.Text.Json.JsonSerializer.Serialize(new 
            { 
                error = "Failed to retrieve conversation context",
                details = ex.Message 
            }, Default);
        }
        finally
        {
            RequestContextService.Clear();
        }
    }

    [McpServerTool, Description("Clear conversation history for current session. Removes all messages from rolling window.")]
    public async Task<string> ClearConversation(
        [Description("Bearer token")] string bearerToken)
    {
        try
        {
            RequestContextService.SetToken(bearerToken);
            
            var sessionId = GetSessionId();
            _contextService.ClearSession(sessionId);

            _logger.LogInformation("Cleared conversation context for session {Session}", sessionId);

            return System.Text.Json.JsonSerializer.Serialize(new
            {
                sessionId = sessionId,
                message = "Conversation context cleared successfully",
                timestamp = DateTime.UtcNow
            }, Default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing conversation context");
            return System.Text.Json.JsonSerializer.Serialize(new 
            { 
                error = "Failed to clear conversation context",
                details = ex.Message 
            }, Default);
        }
        finally
        {
            RequestContextService.Clear();
        }
    }
}
