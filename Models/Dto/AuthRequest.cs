namespace McpVersionVer2.Models.Dto;

/// <summary>
/// Login request model for external API
/// </summary>
public class LoginRequest
{
    public string phone { get; set; } = string.Empty;
    public string password { get; set; } = string.Empty;
}