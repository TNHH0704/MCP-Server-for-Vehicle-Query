using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace McpVersionVer2.Services;

/// <summary>
/// Service for handling authentication operations including token refresh
/// </summary>
public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthService> _logger;
    private readonly string _authApiUrl;
    private readonly string _refreshTokenEndpoint;

    public AuthService(
        HttpClient httpClient, 
        ILogger<AuthService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _authApiUrl = configuration.GetValue<string>("ApiSettings:AuthApiUrl")
            ?? throw new InvalidOperationException("AuthApiUrl not configured in appsettings.json");
        _refreshTokenEndpoint = configuration.GetValue<string>("ApiSettings:RefreshTokenEndpoint") ?? "/refresh";
    }

    /// <summary>
    /// Refresh an expired access token using a refresh token
    /// </summary>
    public async Task<TokenResponse?> RefreshAccessTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            _logger.LogWarning("Attempted to refresh with empty refresh token");
            return null;
        }

        try
        {
            var endpoint = $"{_authApiUrl}{_refreshTokenEndpoint}";
            _logger.LogDebug("Attempting token refresh at: {Endpoint}", endpoint);

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { refreshToken }),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token refresh failed with status code: {StatusCode}", 
                    response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            _logger.LogInformation("Token refresh successful");
            return tokenResponse;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred during token refresh");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize token response");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token refresh");
            return null;
        }
    }
}

/// <summary>
/// Response model for token refresh operations
/// </summary>
public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; } // Seconds until expiration
    public string TokenType { get; set; } = "Bearer";
}
