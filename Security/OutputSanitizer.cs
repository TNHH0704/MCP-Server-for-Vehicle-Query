using System.Text.RegularExpressions;

namespace McpVersionVer2.Security;

/// <summary>
/// Sanitizes output to prevent prompt injection attacks and validates inputs
/// </summary>
public static class OutputSanitizer
{
    private static readonly string[] DangerousPatterns = new[]
    {
        "ignore previous",
        "ignore all previous",
        "disregard",
        "new instructions",
        "system:",
        "assistant:",
        "user:",
        "human:",
        "###",
        "<|",
        "|>",
        "override",
        "admin mode",
        "developer mode",
        "jailbreak"
    };

    // Off-topic keywords that indicate non-vehicle queries
    private static readonly HashSet<string> OffTopicKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "recipe", "weather", "joke", "story", "poem", "essay", "translate",
        "summarize", "explain", "how to", "tutorial", "homework", "calculate",
        "write a", "generate", "create a", "make a", "tell me about"
    };

    // SQL injection patterns
    private static readonly string[] SqlInjectionPatterns = new[]
    {
        "select ", "insert ", "update ", "delete ", "drop ", "create ",
        "alter ", "exec ", "execute ", "union ", "--", "/*", "*/"
    };

    /// <summary>
    /// Validate if input is relevant to vehicle tracking
    /// </summary>
    public static (bool isValid, string? errorMessage) ValidateVehicleQuery(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (true, null); // Empty inputs are handled elsewhere

        var lowerInput = input.ToLowerInvariant();

        // Check for off-topic keywords
        foreach (var keyword in OffTopicKeywords)
        {
            if (lowerInput.Contains(keyword.ToLowerInvariant()))
            {
                return (false, "This service only handles vehicle tracking queries. Please ask about vehicles, locations, or tracking data.");
            }
        }

        // Check for SQL injection attempts
        foreach (var pattern in SqlInjectionPatterns)
        {
            if (lowerInput.Contains(pattern))
            {
                return (false, "Invalid input detected. SQL commands are not allowed.");
            }
        }

        // Check for script injection
        if (lowerInput.Contains("<script") || lowerInput.Contains("javascript:"))
        {
            return (false, "Invalid input detected. Script content is not allowed.");
        }

        return (true, null);
    }

    /// <summary>
    /// Validate bearer token format
    /// </summary>
    public static bool IsValidBearerToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        // Basic JWT format check: three base64 segments separated by dots
        var parts = token.Split('.');
        if (parts.Length != 3)
            return false;

        // Check reasonable length (JWT tokens are typically 100-500 chars)
        if (token.Length < 50 || token.Length > 2000)
            return false;

        return true;
    }

    /// <summary>
    /// Validate vehicle ID format
    /// </summary>
    public static bool IsValidVehicleId(string? vehicleId)
    {
        if (string.IsNullOrWhiteSpace(vehicleId))
            return false;

        // Vehicle IDs should be alphanumeric, may include hyphens or underscores
        // Typical length: 10-50 characters
        if (vehicleId.Length < 5 || vehicleId.Length > 100)
            return false;

        // Only allow alphanumeric, hyphens, underscores
        return Regex.IsMatch(vehicleId, @"^[a-zA-Z0-9\-_]+$");
    }

    /// <summary>
    /// Validate license plate format
    /// </summary>
    public static bool IsValidPlateNumber(string? plate)
    {
        if (string.IsNullOrWhiteSpace(plate))
            return false;

        // Plate numbers: alphanumeric, spaces, dots, hyphens
        // Length: 2-20 characters
        if (plate.Length < 2 || plate.Length > 20)
            return false;

        return Regex.IsMatch(plate, @"^[a-zA-Z0-9\s\.\-]+$");
    }

    /// <summary>
    /// Validate datetime string format
    /// </summary>
    public static bool IsValidDateTimeString(string? dateTime)
    {
        if (string.IsNullOrWhiteSpace(dateTime))
            return false;

        return DateTime.TryParse(dateTime, out _);
    }

    /// <summary>
    /// Sanitize string fields to prevent prompt injection while preserving data integrity
    /// </summary>
    public static string Sanitize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input ?? string.Empty;

        // Replace suspicious patterns with safe alternatives
        var sanitized = input;
        
        foreach (var pattern in DangerousPatterns)
        {
            sanitized = Regex.Replace(
                sanitized, 
                Regex.Escape(pattern), 
                "[FILTERED]", 
                RegexOptions.IgnoreCase
            );
        }

        // Remove multiple newlines that could be used for prompt injection
        sanitized = Regex.Replace(sanitized, @"\n{3,}", "\n\n");
        
        // Remove control characters except newline, carriage return, tab
        sanitized = Regex.Replace(sanitized, @"[\x00-\x08\x0B-\x0C\x0E-\x1F\x7F]", "");

        return sanitized;
    }

    /// <summary>
    /// Wrap JSON output with a clear data boundary marker
    /// </summary>
    public static string WrapAsData(string jsonOutput)
    {
        return $"[DATA_START]\n{jsonOutput}\n[DATA_END]";
    }

    /// <summary>
    /// Create standardized error response
    /// </summary>
    public static string CreateErrorResponse(string errorMessage, string errorCode = "INVALID_INPUT")
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            success = false,
            errorCode,
            error = errorMessage
        });
    }
}
