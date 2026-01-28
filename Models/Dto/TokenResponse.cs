namespace McpVersionVer2.Models.Dto;

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