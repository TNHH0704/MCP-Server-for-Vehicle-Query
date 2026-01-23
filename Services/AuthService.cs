using System.Text.Json;
using McpVersionVer2.Models;

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
    private readonly string _loginEndpoint;
    private readonly int _tokenRefreshBuffer;
    private readonly int _maxRetryAttempts;
    private readonly ISessionStorageService _sessionStorage;

    public AuthService(
        HttpClient httpClient, 
        ILogger<AuthService> logger,
        IConfiguration configuration,
        ISessionStorageService sessionStorage)
    {
        _httpClient = httpClient;
        _logger = logger;
        _sessionStorage = sessionStorage;
        _authApiUrl = configuration.GetValue<string>("ApiSettings:AuthApiUrl")
            ?? throw new InvalidOperationException("AuthApiUrl not configured in appsettings.json");
        _refreshTokenEndpoint = configuration.GetValue<string>("ApiSettings:RefreshTokenEndpoint") ?? "/refresh";
        _loginEndpoint = configuration.GetValue<string>("ApiSettings:LoginEndpoint") ?? "/login";
        _tokenRefreshBuffer = configuration.GetValue<int?>("ApiSettings:TokenRefreshBuffer") ?? 300; // 5 minutes
        _maxRetryAttempts = configuration.GetValue<int?>("ApiSettings:MaxRetryAttempts") ?? 2;
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

    /// <summary>
    /// Login with username and password
    /// </summary>
    public async Task<LoginResponse?> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Login attempted with empty username or password");
            return null;
        }

        try
        {
            // Handle the case where LoginEndpoint might be empty or the URL already includes the full path
            var endpoint = string.IsNullOrWhiteSpace(_loginEndpoint) 
                ? _authApiUrl 
                : $"{_authApiUrl}{_loginEndpoint}";
            
            _logger.LogInformation("Attempting login at: {Endpoint} for user: {Username}", endpoint, username);

            var loginRequest = new LoginRequest
            {
                phone = username,
                password = password
            };

            var requestJson = JsonSerializer.Serialize(loginRequest, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            _logger.LogDebug("Login request payload: {RequestPayload}", requestJson);

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("accept", "application/json");
            request.Content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            // Log the full response details for debugging
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Login response status: {StatusCode}, content: {Content}", 
                response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Login failed with status code: {StatusCode}, content: {Content}", 
                    response.StatusCode, responseContent);
                return null;
            }

            // Parse the API response format
            TokenResponse? tokenResponse = null;
            try
            {
                var apiResponse = JsonSerializer.Deserialize<LoginApiResponse>(responseContent, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (apiResponse != null)
                {
                    if (apiResponse.Success && apiResponse.Data != null)
                    {
                        tokenResponse = apiResponse.Data;
                        _logger.LogInformation("Login successful: {Message}", apiResponse.Message);
                    }
                    else
                    {
                        _logger.LogWarning("Login API returned failure: {Message}, ErrorCode: {ErrorCode}", 
                            apiResponse.Message, apiResponse.ErrorCode);
                        return null;
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning("Failed to deserialize API response: {Error}, Content: {Content}", 
                    ex.Message, responseContent);
                return null;
            }

            if (tokenResponse == null)
            {
                _logger.LogWarning("Failed to deserialize login response with any format. Raw content: {Content}", responseContent);
                return null;
            }

            // Create session and store tokens
            var sessionId = _sessionStorage.GetOrCreateSessionId(tokenResponse.AccessToken);
            var cachedTokens = new CachedTokenPair
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
                RefreshExpiresAt = DateTime.UtcNow.AddDays(30) // Assume refresh token lasts 30 days
            };

            _sessionStorage.StoreSessionTokens(sessionId, cachedTokens);

            _logger.LogInformation("Login successful for session {Session}", sessionId);

            return new LoginResponse
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                ExpiresIn = tokenResponse.ExpiresIn,
                TokenType = tokenResponse.TokenType,
                LoginTime = DateTime.UtcNow,
                SessionId = sessionId
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred during login");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize login response");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login");
            return null;
        }
    }

    /// <summary>
    /// Gets a valid access token for the session, refreshing if needed
    /// </summary>
    public async Task<string?> GetValidTokenAsync(string sessionId)
    {
        var tokens = _sessionStorage.GetSessionTokens(sessionId);
        
        if (tokens == null)
        {
            _logger.LogDebug("No tokens found for session {Session}", sessionId);
            return null;
        }

        // Check if access token is still valid
        if (!tokens.NeedsRefresh(_tokenRefreshBuffer))
        {
            return tokens.AccessToken;
        }

        // Check if refresh token is expired
        if (tokens.IsRefreshExpired())
        {
            _logger.LogWarning("Refresh token expired for session {Session}", sessionId);
            _sessionStorage.RemoveSessionTokens(sessionId);
            return null;
        }

        // Attempt to refresh the token
        _logger.LogDebug("Refreshing token for session {Session}", sessionId);
        var refreshedTokens = await RefreshAccessTokenAsync(tokens.RefreshToken);
        
        if (refreshedTokens == null)
        {
            _logger.LogWarning("Token refresh failed for session {Session}", sessionId);
            _sessionStorage.RemoveSessionTokens(sessionId);
            return null;
        }

        // Update stored tokens
        var updatedCachedTokens = new CachedTokenPair
        {
            AccessToken = refreshedTokens.AccessToken,
            RefreshToken = refreshedTokens.RefreshToken,
            ExpiresAt = DateTime.UtcNow.AddSeconds(refreshedTokens.ExpiresIn),
            RefreshExpiresAt = tokens.RefreshExpiresAt, // Keep original refresh expiry
            LoginTime = tokens.LoginTime
        };

        _sessionStorage.StoreSessionTokens(sessionId, updatedCachedTokens);

        return refreshedTokens.AccessToken;
    }

    /// <summary>
    /// Sends an authenticated HTTP request with automatic token refresh and retry logic
    /// </summary>
    public async Task<HttpResponseMessage> SendAuthenticatedRequestAsync(
        HttpRequestMessage request, 
        string sessionId)
    {
        var attempt = 0;
        
        while (attempt < _maxRetryAttempts)
        {
            attempt++;
            
            try
            {
                var validToken = await GetValidTokenAsync(sessionId);
                
                if (validToken == null)
                {
                    return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
                    {
                        Content = new StringContent(
                            JsonSerializer.Serialize(new { error = "No valid authentication token available" }),
                            System.Text.Encoding.UTF8,
                            "application/json")
                    };
                }

                // Clone the request to avoid issues with retry
                var requestClone = CloneHttpRequestMessage(request);
                requestClone.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", validToken);

                var response = await _httpClient.SendAsync(requestClone);

                // If we get a 401, try to refresh and retry once more
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && attempt == 1)
                {
                    _logger.LogDebug("Received 401, attempting token refresh and retry");
                    response.Dispose();
                    continue;
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in authenticated request attempt {Attempt}", attempt);
                
                if (attempt >= _maxRetryAttempts)
                {
                    return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                    {
                        Content = new StringContent(
                            JsonSerializer.Serialize(new { error = "Request failed after maximum retries" }),
                            System.Text.Encoding.UTF8,
                            "application/json")
                    };
                }
            }
        }

        return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
    }

    /// <summary>
    /// Clones an HTTP request message for retry scenarios
    /// </summary>
    private static HttpRequestMessage CloneHttpRequestMessage(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri)
        {
            Version = original.Version
        };

        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (original.Content != null)
        {
            var contentBytes = original.Content.ReadAsByteArrayAsync().Result;
            clone.Content = new ByteArrayContent(contentBytes);

            foreach (var header in original.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }

    }
