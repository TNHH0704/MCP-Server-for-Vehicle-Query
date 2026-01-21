using System.Collections.Concurrent;
using McpVersionVer2.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpVersionVer2.Services;

public interface IConversationContextService
{
    void AddMessage(string sessionId, ConversationEntry entry);
    List<ConversationEntry> GetRecentMessages(string sessionId, int? limit = null);
    void ClearSession(string sessionId);
    string GetFormattedContext(string sessionId);
    int GetMessageCount(string sessionId);
}

public class InMemoryConversationContextService : IConversationContextService
{
    private readonly ConversationConfig _config;
    private readonly ConcurrentDictionary<string, ConcurrentQueue<ConversationEntry>> _sessions;
    private readonly ILogger<InMemoryConversationContextService> _logger;

    public InMemoryConversationContextService(
        IOptions<ConversationConfig> config,
        ILogger<InMemoryConversationContextService> logger)
    {
        _config = config.Value;
        _sessions = new ConcurrentDictionary<string, ConcurrentQueue<ConversationEntry>>();
        _logger = logger;
    }

    public void AddMessage(string sessionId, ConversationEntry entry)
    {
        entry.SessionId = sessionId;

        var queue = _sessions.GetOrAdd(sessionId, _ => new ConcurrentQueue<ConversationEntry>());
        queue.Enqueue(entry);

        while (queue.Count > _config.WindowSize)
        {
            queue.TryDequeue(out _);
        }

        if (_config.MaxAge > TimeSpan.Zero)
        {
            PruneOldEntries(queue);
        }

        _logger.LogDebug("Added message to session {Session}, queue size: {Size}", 
            sessionId, queue.Count);
    }

    public List<ConversationEntry> GetRecentMessages(string sessionId, int? limit = null)
    {
        if (!_sessions.TryGetValue(sessionId, out var queue))
        {
            return new List<ConversationEntry>();
        }

        var count = Math.Min(limit ?? _config.WindowSize, queue.Count);
        return queue.TakeLast(count).Reverse().ToList();
    }

    public void ClearSession(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var queue))
        {
            _logger.LogInformation("Cleared conversation context for session {Session}", sessionId);
        }
    }

    public string GetFormattedContext(string sessionId)
    {
        var messages = GetRecentMessages(sessionId);
        
        if (!messages.Any())
        {
            return string.Empty;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## Recent Conversation Context");
        sb.AppendLine($"Total messages: {messages.Count}");
        sb.AppendLine();

        foreach (var msg in messages)
        {
            var timestamp = msg.Timestamp.ToString("HH:mm:ss");
            sb.AppendLine($"**[{timestamp}] {msg.Role}**: {msg.Message}");
            
            if (msg.Metadata != null && msg.Metadata.Any())
            {
                sb.AppendLine($"   Context: {System.Text.Json.JsonSerializer.Serialize(msg.Metadata)}");
            }
        }

        return sb.ToString();
    }

    public int GetMessageCount(string sessionId)
    {
        return _sessions.TryGetValue(sessionId, out var queue) ? queue.Count : 0;
    }

    private void PruneOldEntries(ConcurrentQueue<ConversationEntry> queue)
    {
        var cutoffTime = DateTime.UtcNow - _config.MaxAge;
        
        var tempQueue = new Queue<ConversationEntry>();
        while (queue.TryDequeue(out var entry))
        {
            if (entry.Timestamp >= cutoffTime)
            {
                tempQueue.Enqueue(entry);
            }
        }
        
        while (tempQueue.Count > 0)
        {
            queue.Enqueue(tempQueue.Dequeue());
        }
    }
}
