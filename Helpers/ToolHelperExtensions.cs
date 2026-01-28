using McpVersionVer2.Models;
using McpVersionVer2.Models.Dto;
using McpVersionVer2.Services;
using McpVersionVer2.Security;
using System.Net.Http;

namespace McpVersionVer2.Helpers;

public static class ToolHelperExtensions
{
    public static async Task<string> ExecuteValidatedToolRequest<T>(
        this SecurityValidationService securityService,
        string queryContext,
        string domain,
        string bearerToken,
        Func<string, Task<T>> action,
        Func<T, string> successResponse)
    {
        var tokenHash = RateLimiter.GetTokenHash(bearerToken);
        var userId = tokenHash;

        var validation = await securityService.ValidateQueryAsync(queryContext, domain, userId);
        if (!validation.IsValid)
        {
            throw new ToolValidationException(validation.ToJsonResponse());
        }

        if (!securityService.IsValidBearerToken(bearerToken))
        {
            throw new ToolValidationException(securityService.ValidateQueryWithRules(queryContext, domain, userId).ToJsonResponse());
        }

        var (allowed, rateLimitReason) = RateLimiter.IsAllowed(tokenHash);
        if (!allowed)
        {
            throw new ToolValidationException(System.Text.Json.JsonSerializer.Serialize(new
            {
                success = false,
                errorCode = "RATE_LIMIT_EXCEEDED",
                error = rateLimitReason!
            }));
        }

        var result = await action(bearerToken);
        return successResponse(result);
    }

    public static void RequireNonEmptyResult<T>(
        this IEnumerable<T> items,
        string entityName,
        string customMessage = null)
    {
        var message = customMessage ?? $"No {entityName} found in the fleet.";
        if (items == null || !items.Any())
        {
            throw new InvalidOperationException(message);
        }
    }

    public static async Task<List<VehicleStatus>> GetVehiclesWithFilterAsync(
        this VehicleStatusService service,
        string bearerToken,
        string? plate = null,
        string? id = null,
        string? group = null,
        string? type = null)
    {
        if (!string.IsNullOrEmpty(plate))
        {
            var vehicle = await service.GetVehicleStatusByPlateAsync(bearerToken, plate)
                .SafeGetSingleAsync("vehicle", $"plate '{plate}'");
            return new List<VehicleStatus> { vehicle };
        }

        if (!string.IsNullOrEmpty(id))
        {
            var vehicle = await service.GetVehicleStatusByIdAsync(bearerToken, id)
                .SafeGetSingleAsync("vehicle", $"ID '{id}'");
            return new List<VehicleStatus> { vehicle };
        }

        if (!string.IsNullOrEmpty(group))
        {
            return await service.GetVehiclesByGroupAsync(bearerToken, group);
        }

        if (!string.IsNullOrEmpty(type))
        {
            return await service.GetVehiclesByTypeAsync(bearerToken, type);
        }

        return await service.GetVehicleStatusesAsync(bearerToken);
    }

    public static async Task<List<VehicleResponse>> GetVehiclesWithFilterAsync(
        this VehicleService service,
        string bearerToken,
        string? plate = null,
        string? id = null,
        string? group = null)
    {
        if (!string.IsNullOrEmpty(plate))
        {
            var vehicle = await service.GetVehicleByPlateAsync(bearerToken, plate)
                .SafeGetSingleAsync("vehicle", $"plate '{plate}'");
            return new List<VehicleResponse> { vehicle };
        }

        if (!string.IsNullOrEmpty(id))
        {
            var vehicle = await service.GetVehicleByIdAsync(bearerToken, id)
                .SafeGetSingleAsync("vehicle", $"ID '{id}'");
            return new List<VehicleResponse> { vehicle };
        }

        if (!string.IsNullOrEmpty(group))
        {
            return await service.GetVehiclesByCompanyAsync(bearerToken, group);
        }

        return await service.GetVehiclesAsync(bearerToken);
    }

    public static async Task<string> ExecuteValidatedToolRequestWithContext<T>(
        this SecurityValidationService securityService,
        string queryContext,
        string domain,
        string bearerToken,
        IConversationContextService contextService,
        RequestContextService requestContext,
        Func<string, Task<T>> action,
        Func<T, string> successResponse)
    {
        try
        {
            RequestContextService.SetToken(bearerToken);
            
            var tokenHash = RateLimiter.GetTokenHash(bearerToken);
            var userId = tokenHash;

        var validation = await securityService.ValidateQueryAsync(queryContext, domain, userId);
            if (!validation.IsValid)
            {
                throw new ToolValidationException(validation.ToJsonResponse());
            }

            if (!securityService.IsValidBearerToken(bearerToken))
            {
                throw new ToolValidationException(securityService.ValidateQueryWithRules(queryContext, domain, userId).ToJsonResponse());
            }

            var (allowed, rateLimitReason) = RateLimiter.IsAllowed(tokenHash);
            if (!allowed)
            {
                throw new ToolValidationException(System.Text.Json.JsonSerializer.Serialize(new
                {
                    success = false,
                    errorCode = "RATE_LIMIT_EXCEEDED",
                    error = rateLimitReason!
                }));
            }

            var sessionId = requestContext.SessionId;
            var toolName = queryContext.Split(' ')[0];
            
            var toolCall = new ConversationEntry
            {
                SessionId = sessionId,
                Role = "tool_call",
                ToolName = toolName,
                Message = $"Called: {queryContext}",
                Timestamp = DateTime.UtcNow
            };
            contextService.AddMessage(sessionId, toolCall);

            var result = await action(bearerToken);
            var response = successResponse(result);

            var responseEntry = new ConversationEntry
            {
                SessionId = sessionId,
                Role = "assistant",
                ToolName = toolName,
                Message = response,
                Timestamp = DateTime.UtcNow
            };
            contextService.AddMessage(sessionId, responseEntry);

            return response;
        }
        finally
        {
            RequestContextService.Clear();
        }
    }

    /// <summary>
    /// Extension method for HttpClient to send authenticated requests with automatic token management
    /// </summary>
    public static async Task<HttpResponseMessage> SendAuthenticatedAsync(
        this HttpClient httpClient,
        HttpRequestMessage request,
        string sessionId,
        AuthService authService)
    {
        return await authService.SendAuthenticatedRequestAsync(request, sessionId);
    }

    /// <summary>
    /// Extension method for HttpClient to send GET requests with automatic authentication
    /// </summary>
    public static async Task<HttpResponseMessage> GetAuthenticatedAsync(
        this HttpClient httpClient,
        string requestUri,
        string sessionId,
        AuthService authService)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        return await authService.SendAuthenticatedRequestAsync(request, sessionId);
    }

    /// <summary>
    /// Extension method for HttpClient to send POST requests with automatic authentication
    /// </summary>
    public static async Task<HttpResponseMessage> PostAuthenticatedAsync(
        this HttpClient httpClient,
        string requestUri,
        HttpContent content,
        string sessionId,
        AuthService authService)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = content };
        return await authService.SendAuthenticatedRequestAsync(request, sessionId);
    }

    /// <summary>
    /// Creates a new HttpRequestMessage with authentication headers
    /// </summary>
    public static async Task<HttpRequestMessage> CreateAuthenticatedRequestAsync(
        this HttpRequestMessage request,
        string sessionId,
        AuthService authService)
    {
        var token = await authService.GetValidTokenAsync(sessionId);
        if (token != null)
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        return request;
    }
}
