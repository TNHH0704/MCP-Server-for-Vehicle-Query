using System.ComponentModel;
using McpVersionVer2.Models;
using McpVersionVer2.Services;
using McpVersionVer2.Security;
using McpVersionVer2.Tools;

namespace McpVersionVer2.Helpers;

public static class ToolHelperExtensions
{
    public static async Task<string> ExecuteValidatedToolRequest<T>(
        this GuardrailService guardrail,
        string queryContext,
        string domain,
        string bearerToken,
        Func<string, Task<T>> action,
        Func<T, string> successResponse)
    {
        var tokenHash = RateLimiter.GetTokenHash(bearerToken);
        var userId = tokenHash;

        var validation = await guardrail.ValidateQueryWithAIAsync(queryContext, domain, userId);
        if (!validation.IsValid)
        {
            throw new ToolValidationException(validation.ToJsonResponse());
        }

        var (isValid, errorMessage) = OutputSanitizer.ValidateVehicleQuery(queryContext);
        if (!isValid)
        {
            throw new ToolValidationException(OutputSanitizer.CreateErrorResponse(
                "This tool is ONLY for vehicle tracking queries. " + errorMessage,
                "OFF_TOPIC"));
        }

        if (!OutputSanitizer.IsValidBearerToken(bearerToken))
        {
            throw new ToolValidationException(OutputSanitizer.CreateErrorResponse(
                "Invalid bearer token format.",
                "INVALID_TOKEN"));
        }

        var (allowed, rateLimitReason) = RateLimiter.IsAllowed(tokenHash);
        if (!allowed)
        {
            throw new ToolValidationException(OutputSanitizer.CreateErrorResponse(
                rateLimitReason!,
                "RATE_LIMIT_EXCEEDED"));
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
        this GuardrailService guardrail,
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

            var validation = await guardrail.ValidateQueryWithAIAsync(queryContext, domain, userId);
            if (!validation.IsValid)
            {
                throw new ToolValidationException(validation.ToJsonResponse());
            }

            var (isValid, errorMessage) = OutputSanitizer.ValidateVehicleQuery(queryContext);
            if (!isValid)
            {
                throw new ToolValidationException(OutputSanitizer.CreateErrorResponse(
                    "This tool is ONLY for vehicle tracking queries. " + errorMessage,
                    "OFF_TOPIC"));
            }

            if (!OutputSanitizer.IsValidBearerToken(bearerToken))
            {
                throw new ToolValidationException(OutputSanitizer.CreateErrorResponse(
                    "Invalid bearer token format.",
                    "INVALID_TOKEN"));
            }

            var (allowed, rateLimitReason) = RateLimiter.IsAllowed(tokenHash);
            if (!allowed)
            {
                throw new ToolValidationException(OutputSanitizer.CreateErrorResponse(
                    rateLimitReason!,
                    "RATE_LIMIT_EXCEEDED"));
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
}
