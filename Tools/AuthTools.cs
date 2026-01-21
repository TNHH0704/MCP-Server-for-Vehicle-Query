using System.ComponentModel;
using McpVersionVer2.Services;
using McpVersionVer2.Security;
using ModelContextProtocol.Server;
using static McpVersionVer2.Services.AppJsonSerializerOptions;

namespace McpVersionVer2.Tools;

/// <summary>
/// MCP tools for authentication operations
/// </summary>
[McpServerToolType]
public class AuthTools
{
    private readonly AuthService _authService;
    private readonly GuardrailService _guardrail;

    public AuthTools(AuthService authService, GuardrailService guardrail)
    {
        _authService = authService;
        _guardrail = guardrail;
    }

    [McpServerTool, Description("AUTH ONLY: Refresh an expired JWT access token using a refresh token. Returns new access token and refresh token. REJECT: non-auth queries.")]
    public async Task<string> RefreshToken(
        [Description("Refresh token received during login or previous refresh")] string refreshToken)
    {
        var queryContext = "refresh token auth";
        var validation = await _guardrail.ValidateQueryWithAIAsync(queryContext, "auth", "auth_tool");
        if (!validation.IsValid)
        {
            return validation.ToJsonResponse();
        }

        try
        {
            // Validate input format
            if (!OutputSanitizer.IsValidBearerToken(refreshToken))
            {
                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Invalid refresh token format",
                    errorCode = "INVALID_TOKEN_FORMAT"
                }, Default);
            }

            // Attempt token refresh
            var result = await _authService.RefreshAccessTokenAsync(refreshToken);

            if (result == null)
            {
                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Failed to refresh token. The refresh token may be invalid, expired, or revoked. Please log in again.",
                    errorCode = "REFRESH_FAILED"
                }, Default);
            }

            // Success - return new tokens
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                success = true,
                data = new
                {
                    accessToken = result.AccessToken,
                    refreshToken = result.RefreshToken,
                    expiresIn = result.ExpiresIn,
                    tokenType = result.TokenType,
                    expiresAt = DateTime.UtcNow.AddSeconds(result.ExpiresIn).ToString("dd-MM-yyyy HH:mm:ss") + " UTC"
                },
                message = "Token refreshed successfully"
            }, Default);
        }
        catch (Exception ex)
        {
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                errorCode = "INTERNAL_ERROR"
            }, Default);
        }
    }
}
