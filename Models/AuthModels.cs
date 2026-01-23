using McpVersionVer2.Services;

namespace McpVersionVer2.Models;

/// <summary>
/// Represents a cached JWT token pair with expiration information
/// </summary>
public class CachedTokenPair
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime RefreshExpiresAt { get; set; }
    public DateTime LoginTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Check if the access token is expired or will expire within the buffer period
    /// </summary>
    /// <param name="bufferMinutes">Buffer time in minutes before actual expiry</param>
    /// <returns>True if token needs refresh</returns>
    public bool NeedsRefresh(int bufferMinutes = 5)
    {
        return DateTime.UtcNow.AddMinutes(bufferMinutes) >= ExpiresAt;
    }

    /// <summary>
    /// Check if the refresh token is expired
    /// </summary>
    /// <returns>True if refresh token is expired</returns>
    public bool IsRefreshExpired()
    {
        return DateTime.UtcNow >= RefreshExpiresAt;
    }
}

/// <summary>
/// Response model for token operations
/// </summary>
public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; } 
    public string TokenType { get; set; } = "Bearer";
}

/// <summary>
/// Login request model for external API
/// </summary>
public class LoginRequest
{
    public string phone { get; set; } = string.Empty;
    public string password { get; set; } = string.Empty;
}

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