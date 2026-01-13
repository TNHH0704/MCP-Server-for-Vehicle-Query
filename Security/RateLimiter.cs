using System.Collections.Concurrent;

namespace McpVersionVer2.Security;

/// <summary>
/// Rate limiter to prevent abuse and excessive API calls
/// </summary>
public class RateLimiter
{
    private static readonly ConcurrentDictionary<string, RequestInfo> _requests = new();
    private static readonly int MaxRequestsPerMinute = 60;
    private static readonly int MaxRequestsPerHour = 500;

    private class RequestInfo
    {
        public int CountPerMinute { get; set; }
        public int CountPerHour { get; set; }
        public DateTime MinuteResetTime { get; set; }
        public DateTime HourResetTime { get; set; }
    }

    /// <summary>
    /// Check if request is allowed for the given identifier (user ID, IP, token hash)
    /// </summary>
    public static (bool allowed, string? reason) IsAllowed(string identifier)
    {
        var now = DateTime.UtcNow;
        
        var info = _requests.GetOrAdd(identifier, _ => new RequestInfo
        {
            CountPerMinute = 0,
            CountPerHour = 0,
            MinuteResetTime = now.AddMinutes(1),
            HourResetTime = now.AddHours(1)
        });

        lock (info)
        {
            // Reset minute counter if time window expired
            if (info.MinuteResetTime < now)
            {
                info.CountPerMinute = 0;
                info.MinuteResetTime = now.AddMinutes(1);
            }

            // Reset hour counter if time window expired
            if (info.HourResetTime < now)
            {
                info.CountPerHour = 0;
                info.HourResetTime = now.AddHours(1);
            }

            // Check limits
            if (info.CountPerMinute >= MaxRequestsPerMinute)
            {
                var waitSeconds = (int)(info.MinuteResetTime - now).TotalSeconds;
                return (false, $"Rate limit exceeded: {MaxRequestsPerMinute} requests per minute. Try again in {waitSeconds} seconds.");
            }

            if (info.CountPerHour >= MaxRequestsPerHour)
            {
                var waitMinutes = (int)(info.HourResetTime - now).TotalMinutes;
                return (false, $"Rate limit exceeded: {MaxRequestsPerHour} requests per hour. Try again in {waitMinutes} minutes.");
            }

            // Increment counters
            info.CountPerMinute++;
            info.CountPerHour++;

            return (true, null);
        }
    }

    /// <summary>
    /// Clean up old entries (call periodically)
    /// </summary>
    public static void Cleanup()
    {
        var now = DateTime.UtcNow;
        var keysToRemove = _requests
            .Where(kvp => kvp.Value.HourResetTime < now.AddHours(-2))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _requests.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Get hash of token for rate limiting (to avoid storing full tokens)
    /// </summary>
    public static string GetTokenHash(string token)
    {
        if (string.IsNullOrEmpty(token))
            return "anonymous";

        // Use last 10 characters as identifier (safe, doesn't expose full token)
        return token.Length > 10 ? token.Substring(token.Length - 10) : token;
    }
}
