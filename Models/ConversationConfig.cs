namespace McpVersionVer2.Models;

public class ConversationConfig
{
    public int WindowSize { get; set; } = 10;
    public int MaxTokens { get; set; } = 8000;
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromHours(1);
    public bool AutoProvideContext { get; set; } = true;
}
