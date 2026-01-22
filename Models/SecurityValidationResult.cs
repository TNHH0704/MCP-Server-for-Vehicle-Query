using System.Text.Json;

public class SecurityValidationResult
{
    public bool IsValid { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string[]? AllowedTopics { get; private set; }
    public bool UsedAIVerdict { get; private set; }
    public double? Confidence { get; private set; }
    public string? RiskLevel { get; private set; }
    public string? ValidationSource { get; private set; }

    public static SecurityValidationResult Passed() => new() { 
        IsValid = true,
        ValidationSource = "RuleBased"
    };

    public static SecurityValidationResult PassedWithAI(double confidence, string riskLevel) => new()
    {
        IsValid = true,
        UsedAIVerdict = true,
        Confidence = confidence,
        RiskLevel = riskLevel,
        ValidationSource = "AI"
    };

    public static SecurityValidationResult Failed(string errorCode, string message, string[]? allowedTopics = null)
    {
        return new SecurityValidationResult
        {
            IsValid = false,
            ErrorCode = errorCode,
            ErrorMessage = message,
            AllowedTopics = allowedTopics,
            ValidationSource = "RuleBased"
        };
    }

    public static SecurityValidationResult FailedWithAI(string errorCode, string message, double confidence, string riskLevel, string[]? allowedTopics = null)
    {
        return new SecurityValidationResult
        {
            IsValid = false,
            ErrorCode = errorCode,
            ErrorMessage = message,
            AllowedTopics = allowedTopics,
            UsedAIVerdict = true,
            Confidence = confidence,
            RiskLevel = riskLevel,
            ValidationSource = "AI"
        };
    }

    public string ToJsonResponse()
    {
        return JsonSerializer.Serialize(new
        {
            success = false,
            errorCode = ErrorCode,
            error = ErrorMessage,
            allowedTopics = AllowedTopics,
            hint = "This MCP server only handles fleet management and vehicle tracking queries."
        }, new JsonSerializerOptions { WriteIndented = true });
    }
}