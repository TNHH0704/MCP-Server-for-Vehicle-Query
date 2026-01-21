namespace McpVersionVer2.Models;

public class ConversationEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SessionId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Role { get; set; } = "user";
    public string ToolName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
    public int TokenCount { get; set; }
}
