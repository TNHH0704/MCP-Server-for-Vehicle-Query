using System.Text.Json;
using McpVersionVer2.Services;
using McpVersionVer2.Security;

namespace McpVersionVer2.Helpers;

/// <summary>
/// Helper for centralized tool execution with standardized error handling.
/// </summary>
public static class ToolExecutionHelper
{
    /// <summary>
    /// Executes a validated tool request with standardized exception handling.
    /// </summary>
    /// <param name="securityService">The security validation service.</param>
    /// <param name="queryContext">Query context for validation.</param>
    /// <param name="domain">Tool domain.</param>
    /// <param name="bearerToken">Bearer token.</param>
    /// <param name="contextService">Conversation context service.</param>
    /// <param name="requestContext">Request context service.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="successResponse">Success response formatter.</param>
    /// <returns>The result of the action or a standardized error response.</returns>
    public static async Task<string> ExecuteValidatedToolRequestWithContextAsync<T>(
        SecurityValidationService securityService,
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
            return await securityService.ExecuteValidatedToolRequestWithContext(
                queryContext: queryContext,
                domain: domain,
                bearerToken: bearerToken,
                contextService: contextService,
                requestContext: requestContext,
                action: action,
                successResponse: successResponse);
        }
        catch (ToolValidationException ex)
        {
            return ex.ErrorResponse;
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, AppJsonSerializerOptions.Default);
        }
    }
}