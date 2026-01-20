using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static McpVersionVer2.Services.AppJsonSerializerOptions;

namespace McpVersionVer2.Services;

/// <summary>
/// Service for logging security events and blocked queries
/// </summary>
public class AuditLogService
{
    private readonly ILogger<AuditLogService> _logger;
    private readonly string _logDirectory;
    private readonly string _logFilePath;
    private const long MaxLogSizeBytes = 10 * 1024 * 1024; // 10MB
    private const int MaxLogFiles = 5;

    public AuditLogService(ILogger<AuditLogService> logger, IConfiguration configuration)
    {
        _logger = logger;
        
        var logPath = configuration["GuardrailSettings:AuditLogPath"] ?? "./logs";
        _logDirectory = Path.GetFullPath(logPath);
        _logFilePath = Path.Combine(_logDirectory, "audit.log");
        
        Directory.CreateDirectory(_logDirectory);
    }

    public void LogBlockedQuery(
        string? userId,
        string toolName,
        string query,
        string reason)
    {
        var entry = new AuditEntry
        {
            Timestamp = DateTime.UtcNow,
            UserId = userId ?? "anonymous",
            ToolName = toolName,
            Reason = reason,
            QueryPreview = query.Length > 200 ? query[..200] + "..." : query
        };

        try
        {
            var json = JsonSerializer.Serialize(entry, Default);
            
            CheckAndRotateLog();
            
            File.AppendAllText(_logFilePath, json + "\n");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log");
        }

        _logger.LogWarning(
            "BLOCKED_QUERY: User={UserId} Tool={Tool} Reason={Reason}",
            entry.UserId, toolName, reason
        );
    }

    private void CheckAndRotateLog()
    {
        try
        {
            if (!File.Exists(_logFilePath))
                return;

            var fileInfo = new FileInfo(_logFilePath);
            if (fileInfo.Length < MaxLogSizeBytes)
                return;

            for (int i = MaxLogFiles - 1; i >= 1; i--)
            {
                var oldFile = Path.Combine(_logDirectory, $"audit.log.{i}");
                var newFile = Path.Combine(_logDirectory, $"audit.log.{i + 1}");
                
                if (File.Exists(oldFile))
                {
                    if (i + 1 > MaxLogFiles)
                        File.Delete(oldFile);
                    else
                        File.Move(oldFile, newFile);
                }
            }

            var firstBackup = Path.Combine(_logDirectory, "audit.log.1");
            File.Move(_logFilePath, firstBackup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate audit log");
        }
    }
}

public class AuditEntry
{
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; } = "";
    public string ToolName { get; set; } = "";
    public string Reason { get; set; } = "";
    public string QueryPreview { get; set; } = "";
}

public class SecurityEvent
{
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = "";
    public string Details { get; set; } = "";
}
