using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace McpVersionVer2.Services;

/// <summary>
/// Centralized guardrail service for all MCP tools
/// </summary>
public class GuardrailService
{
    private readonly AuditLogService _auditLog;
    private readonly GitHubOpenAIService _openAIService;
    private readonly ILogger<GuardrailService> _logger;

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

    private readonly Dictionary<string, HashSet<string>> _domainTopics;

    public GuardrailService(AuditLogService auditLog, GitHubOpenAIService openAIService, ILogger<GuardrailService> logger)
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

    public GuardrailResult ValidateQuery(string query, string toolDomain, string? userId = null)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return GuardrailResult.Passed();
        }

        var lowerQuery = query.ToLowerInvariant();

        // Check 1: Does query contain any allowed topic keywords for the domain?
        if (!ContainsAllowedTopic(lowerQuery, toolDomain))
        {
            var allowedTopics = GetAllowedTopicsArray(toolDomain);
            _auditLog.LogBlockedQuery(userId, toolDomain, query, "TOPIC_MISMATCH");
            return GuardrailResult.Failed(
                errorCode: "OFF_TOPIC",
                message: $"This tool is for {FormatDomainName(toolDomain)} queries. Your query does not appear to be related.",
                allowedTopics: allowedTopics
            );
        }

        // Check 2: Security validations (always run first for safety)
        var securityCheck = ValidateSecurity(query);
        if (!securityCheck.isValid)
        {
            _auditLog.LogBlockedQuery(userId, toolDomain, query, "SECURITY_VIOLATION");
            return GuardrailResult.Failed(
                errorCode: "SECURITY_VIOLATION",
                message: securityCheck.errorMessage ?? "Invalid input detected."
            );
        }

        // Check 3: Is this an educational/deceptive query?
        var hasEducational = ContainsAnyKeyword(lowerQuery, EducationalKeywords);
        var hasDataPattern = ContainsAnyPattern(lowerQuery, DataPatterns);

        if (hasEducational && !hasDataPattern)
        {
            _auditLog.LogBlockedQuery(userId, toolDomain, query, "EDUCATIONAL_QUERY");
            return GuardrailResult.Failed(
                errorCode: "EDUCATIONAL_QUERY",
                message: "This tool provides vehicle data, not tutorials or educational guidance."
            );
        }

        return GuardrailResult.Passed();
    }

    /// <summary>
    /// Enhanced validation with AI-powered guardrails and fallback to rule-based validation
    /// </summary>
    public async Task<GuardrailResult> ValidateQueryWithAIAsync(string query, string toolDomain, string? userId = null)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return GuardrailResult.Passed();
        }

        var lowerQuery = query.ToLowerInvariant();

        // Always run security validation first (non-AI, critical)
        var securityCheck = ValidateSecurity(query);
        if (!securityCheck.isValid)
        {
            _auditLog.LogBlockedQuery(userId, toolDomain, query, "SECURITY_VIOLATION");
            return GuardrailResult.Failed(
                errorCode: "SECURITY_VIOLATION",
                message: securityCheck.errorMessage ?? "Invalid input detected."
            );
        }

        // Try AI validation first
        try
        {
            var aiResult = await _openAIService.ValidateIntentAsync(query, toolDomain, userId);
            if (aiResult != null)
            {
                // Log AI validation decision
                if (!aiResult.IsValid)
                {
                    _auditLog.LogBlockedQuery(userId, toolDomain, query, $"AI_BLOCKED_{aiResult.ErrorCode}");
                    _logger.LogInformation("AI validation blocked query in domain {Domain}: {Reason}", 
                        toolDomain, aiResult.ErrorMessage);
                }
                else
                {
                    _logger.LogDebug("AI validation passed query in domain {Domain}", toolDomain);
                }
                
                return aiResult;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI validation failed, falling back to rule-based validation");
        }

        // Fallback to rule-based validation
        _logger.LogInformation("Using fallback rule-based validation for domain {Domain}", toolDomain);
        return ValidateQuery(query, toolDomain, userId);
    }

    private bool ContainsAllowedTopic(string query, string domain)
    {
        if (!_domainTopics.TryGetValue(domain, out var topics))
            return false;

        return topics.Any(topic => query.Contains(topic));
    }

    private bool ContainsAnyKeyword(string query, IEnumerable<string> keywords)
    {
        return keywords.Any(k => query.Contains(k));
    }

    private bool ContainsAnyPattern(string query, IEnumerable<string> patterns)
    {
        return patterns.Any(p => query.Contains(p));
    }

    private (bool isValid, string? errorMessage) ValidateSecurity(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (true, null);

        var lowerInput = input.ToLowerInvariant();

        // Check for dangerous patterns
        foreach (var pattern in DangerousPatterns)
        {
            if (lowerInput.Contains(pattern))
            {
                return (false, "Invalid input detected. Potentially dangerous content is not allowed.");
            }
        }

        // Check for script tags
        if (lowerInput.Contains("<script") || lowerInput.Contains("javascript:"))
        {
            return (false, "Invalid input detected. Script content is not allowed.");
        }

        return (true, null);
    }

    private string[] GetAllowedTopicsArray(string domain)
    {
        if (_domainTopics.TryGetValue(domain, out var topics))
        {
            return topics.Take(6).ToArray();
        }
        return Array.Empty<string>();
    }

    private string FormatDomainName(string domain)
    {
        return domain.ToLowerInvariant() switch
        {
            "vehicle_registry" => "vehicle registry",
            "live_status" => "live vehicle status",
            "history" => "vehicle history/tracking",
            "auth" => "authentication",
            _ => domain
        };
    }
}

public class GuardrailResult
{
    public bool IsValid { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string[]? AllowedTopics { get; private set; }
    
    // AI validation metadata
    public bool UsedAIVerdict { get; private set; }
    public double? Confidence { get; private set; }
    public string? RiskLevel { get; private set; }
    public string? ValidationSource { get; private set; }

    public static GuardrailResult Passed() => new() { 
        IsValid = true,
        ValidationSource = "RuleBased"
    };

    public static GuardrailResult PassedWithAI(double confidence, string riskLevel) => new()
    {
        IsValid = true,
        UsedAIVerdict = true,
        Confidence = confidence,
        RiskLevel = riskLevel,
        ValidationSource = "AI"
    };

    public static GuardrailResult Failed(string errorCode, string message, string[]? allowedTopics = null)
    {
        return new GuardrailResult
        {
            IsValid = false,
            ErrorCode = errorCode,
            ErrorMessage = message,
            AllowedTopics = allowedTopics,
            ValidationSource = "RuleBased"
        };
    }

    public static GuardrailResult FailedWithAI(string errorCode, string message, double confidence, string riskLevel, string[]? allowedTopics = null)
    {
        return new GuardrailResult
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
