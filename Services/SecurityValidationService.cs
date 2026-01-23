using System.Text.RegularExpressions;

namespace McpVersionVer2.Services;

///<summary>
/// Unified security validation service for all MCP tools
///</summary>
public class SecurityValidationService
{
    private readonly AuditLogService _auditLog;
    private readonly GitHubOpenAIService _openAIService;
    private readonly ILogger<SecurityValidationService> _logger;

    private static readonly HashSet<string> EducationalKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "learn", "tutorial", "how to", "teach", "course", "study",
        "understand", "explain", "guide", "documentation", "help me",
        "what is", "what are", "tell me about", "generate", "create",
        "write a", "make a", "build a", "develop", "programming"
    };

    private static readonly HashSet<string> DataPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "plate:", "id:", "vehicle_id", "51", "52", "from:", "to:",
        "start:", "end:", "date:", "status:", "group:", "type:"
    };

    private static readonly HashSet<string> DangerousPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "system(", "eval(", "exec(", "cmd", "powershell", "<script",
        "javascript:", "select ", "insert ", "update ", "delete ",
        "drop ", "create ", "alter ", "union ", "--", "/*", "*/"
    };

    private static readonly string[] SanitizationPatterns = new[]
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

    private readonly Dictionary<string, HashSet<string>> _domainTopics;

    public SecurityValidationService(AuditLogService auditLog, GitHubOpenAIService openAIService, ILogger<SecurityValidationService> logger)
    {
        _auditLog = auditLog;
        _openAIService = openAIService;
        _logger = logger;

        _domainTopics = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["vehicle_registry"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "vehicle", "fleet", "plate", "license", "registration",
                "insurance", "registry", "compliance", "group", "company",
                "type", "category", "max speed", "vehicle type"
            },
            ["live_status"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "status", "live", "real-time", "speed", "location", "gps",
                "moving", "stopped", "idle", "overspeed", "overspeeding",
                "mileage", "runtime", "engine", "heading", "direction"
            },
            ["history"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "history", "trip", "waypoint", "track", "route", "path",
                "journey", "distance", "duration", "past", "previous",
                "yesterday", "today", "last", "between"
            },
            ["auth"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "token", "refresh", "auth", "login", "password", "credential",
                "access", "expired", "jwt", "bearer"
            }
        };
    }

    /// <summary>
    /// Main validation pipeline with AI-powered guardrails and fallback to rule-based validation
    /// </summary>
    public async Task<SecurityValidationResult> ValidateQueryAsync(string query, string toolDomain, string? userId = null)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return SecurityValidationResult.Passed();
        }

        var lowerQuery = query.ToLowerInvariant();

        var securityCheck = ValidateSecurity(query);
        if (!securityCheck.isValid)
        {
            _auditLog.LogBlockedQuery(userId, toolDomain, query, "SECURITY_VIOLATION");
            return SecurityValidationResult.Failed(
                errorCode: "SECURITY_VIOLATION",
                message: securityCheck.errorMessage ?? "Invalid input detected."
            );
        }

        try
        {
            var aiResult = await _openAIService.ValidateIntentAsync(query, toolDomain, userId);
            if (aiResult != null)
            {
                _logger.LogDebug("AI validation: {IsValid}, Reason: {Reason}, Risk: {Risk}", 
                    aiResult.IsValid, aiResult.ErrorMessage, aiResult.RiskLevel);

                if (!aiResult.IsValid)
                {
                    _auditLog.LogBlockedQuery(userId, toolDomain, query, "AI_VALIDATION");
                    return SecurityValidationResult.FailedWithAI(
                        errorCode: aiResult.ErrorCode ?? "AI_VALIDATION",
                        message: aiResult.ErrorMessage ?? "AI validation failed",
                        confidence: aiResult.Confidence ?? 0.0,
                        riskLevel: aiResult.RiskLevel ?? "unknown",
                        allowedTopics: aiResult.AllowedTopics
                    );
                }

                return SecurityValidationResult.PassedWithAI(
                    confidence: aiResult.Confidence ?? 0.0,
                    riskLevel: aiResult.RiskLevel ?? "unknown"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI validation failed, falling back to rule-based validation");
        }

        return ValidateQueryWithRules(query, toolDomain, userId);
    }

    /// <summary>
    /// Rule-based validation as fallback when AI is unavailable
    /// </summary>
    public SecurityValidationResult ValidateQueryWithRules(string query, string toolDomain, string? userId = null)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return SecurityValidationResult.Passed();
        }

        var lowerQuery = query.ToLowerInvariant();

        if (!ContainsAllowedTopic(lowerQuery, toolDomain))
        {
            var allowedTopics = GetAllowedTopicsArray(toolDomain);
            _auditLog.LogBlockedQuery(userId, toolDomain, query, "TOPIC_MISMATCH");
            return SecurityValidationResult.Failed(
                errorCode: "OFF_TOPIC",
                message: $"This tool is for {FormatDomainName(toolDomain)} queries. Your query does not appear to be related.",
                allowedTopics: allowedTopics
            );
        }

        var securityCheck = ValidateSecurity(query);
        if (!securityCheck.isValid)
        {
            _auditLog.LogBlockedQuery(userId, toolDomain, query, "SECURITY_VIOLATION");
            return SecurityValidationResult.Failed(
                errorCode: "SECURITY_VIOLATION",
                message: securityCheck.errorMessage ?? "Invalid input detected."
            );
        }

        var hasEducational = ContainsAnyKeyword(lowerQuery, EducationalKeywords);
        var hasDataPattern = ContainsAnyPattern(lowerQuery, DataPatterns);

        if (hasEducational && !hasDataPattern)
        {
            _auditLog.LogBlockedQuery(userId, toolDomain, query, "EDUCATIONAL_QUERY");
            return SecurityValidationResult.Failed(
                errorCode: "EDUCATIONAL_QUERY",
                message: "This tool provides vehicle data, not tutorials or educational guidance."
            );
        }

        return SecurityValidationResult.Passed();
    }

    /// <summary>
    /// Format validation methods (from OutputSanitizer)
    /// </summary>
    public bool IsValidVehicleId(string? vehicleId)
    {
        if (string.IsNullOrWhiteSpace(vehicleId))
            return false;

        if (vehicleId.Length < 5 || vehicleId.Length > 100)
            return false;

        return Regex.IsMatch(vehicleId, @"^[a-zA-Z0-9\-_]+$");
    }

    public bool IsValidPlateNumber(string? plate)
    {
        if (string.IsNullOrWhiteSpace(plate))
            return false;

        if (plate.Length < 2 || plate.Length > 20)
            return false;

        return Regex.IsMatch(plate, @"^[a-zA-Z0-9\s\.\-]+$");
    }

    public bool IsValidBearerToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var parts = token.Split('.');
        if (parts.Length != 3)
            return false;

        if (token.Length < 50 || token.Length > 2000)
            return false;

        return true;
    }

    public bool IsValidDateTimeString(string? dateTime)
    {
        if (string.IsNullOrWhiteSpace(dateTime))
            return false;

        return DateTime.TryParse(dateTime, out _);
    }

    /// <summary>
    /// Sanitize string fields to prevent prompt injection while preserving data integrity
    /// </summary>
    public string SanitizeOutput(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input ?? string.Empty;

        var sanitized = input;
        
        foreach (var pattern in SanitizationPatterns)
        {
            sanitized = Regex.Replace(
                sanitized, 
                Regex.Escape(pattern), 
                "[FILTERED]", 
                RegexOptions.IgnoreCase
            );
        }

        sanitized = Regex.Replace(sanitized, @"[\x00-\x1F\x7F]", "");
        
        return sanitized;
    }

    private (bool isValid, string? errorMessage) ValidateSecurity(string query)
    {
        var lowerQuery = query.ToLowerInvariant();

        foreach (var pattern in DangerousPatterns)
        {
            if (lowerQuery.Contains(pattern))
            {
                return (false, "Invalid input detected. Dangerous patterns are not allowed.");
            }
        }

        return (true, null);
    }

    private bool ContainsAllowedTopic(string lowerQuery, string toolDomain)
    {
        if (_domainTopics.TryGetValue(toolDomain, out var topics))
        {
            return topics.Any(topic => lowerQuery.Contains(topic));
        }
        return false;
    }

    private bool ContainsAnyKeyword(string lowerQuery, HashSet<string> keywords)
    {
        return keywords.Any(keyword => lowerQuery.Contains(keyword));
    }

    private bool ContainsAnyPattern(string lowerQuery, HashSet<string> patterns)
    {
        return patterns.Any(pattern => lowerQuery.Contains(pattern));
    }

    private string[] GetAllowedTopicsArray(string toolDomain)
    {
        return _domainTopics.TryGetValue(toolDomain, out var topics) 
            ? topics.ToArray() 
            : Array.Empty<string>();
    }

    private string FormatDomainName(string domain)
    {
        return domain.ToLowerInvariant() switch
        {
            "vehicle_registry" => "vehicle registry and fleet management",
            "live_status" => "vehicle status and live tracking",
            "history" => "vehicle history and tracking data",
            "auth" => "authentication and token management",
            _ => "vehicle tracking and fleet management"
        };
    }
}
