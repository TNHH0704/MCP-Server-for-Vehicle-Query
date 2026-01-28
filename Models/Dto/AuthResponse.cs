namespace McpVersionVer2.Models.Dto;

/// <summary>
/// API response wrapper from login endpoint
/// </summary>
public class LoginApiResponse
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public TokenResponse? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public object? Metadata { get; set; }
}

/// <summary>
/// Login response model extending TokenResponse
/// </summary>
public class LoginResponse : TokenResponse
{
    public DateTime LoginTime { get; set; } = DateTime.UtcNow;
    public string SessionId { get; set; } = string.Empty;
}