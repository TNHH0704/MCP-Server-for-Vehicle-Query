using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace McpVersionVer2.Security;

/// <summary>
/// Token-based rate limiter using sliding window algorithm.
/// Tracks requests per minute and per hour per token.
/// All methods are static for backward compatibility.
/// </summary>
public static class RateLimiter
{
    private const int DefaultMaxRequestsPerMinute = 60;
    private const int DefaultMaxRequestsPerHour = 500;
    private const int CleanupIntervalMinutes = 5;
    private const int EntryExpirationHours = 2;

    private static readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();
    private static readonly System.Threading.Timer _cleanupTimer;
    private static int _maxRequestsPerMinute = DefaultMaxRequestsPerMinute;
    private static int _maxRequestsPerHour = DefaultMaxRequestsPerHour;

    private class TokenBucket
    {
        public int MinuteCount;
        public int HourCount;
        public DateTime MinuteWindowStart = DateTime.UtcNow;
        public DateTime HourWindowStart = DateTime.UtcNow;
        public DateTime LastAccessTime = DateTime.UtcNow;
    }

    static RateLimiter()
    {
        _cleanupTimer = new System.Threading.Timer(_ => Cleanup(), null, TimeSpan.FromMinutes(CleanupIntervalMinutes), TimeSpan.FromMinutes(CleanupIntervalMinutes));
    }

    /// <summary>
    /// Configure rate limits. Call before first request.
    /// </summary>
    public static void Configure(int maxRequestsPerMinute, int maxRequestsPerHour)
    {
        _maxRequestsPerMinute = maxRequestsPerMinute;
        _maxRequestsPerHour = maxRequestsPerHour;
    }

    /// <summary>
    /// Check if request is allowed for the given token identifier.
    /// </summary>
    public static (bool allowed, string? reason) IsAllowed(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            identifier = "anonymous";
        }

        var now = DateTime.UtcNow;

        var bucket = _buckets.GetOrAdd(identifier, _ => new TokenBucket
        {
            MinuteCount = 0,
            HourCount = 0,
            MinuteWindowStart = now,
            HourWindowStart = now,
            LastAccessTime = now
        });

        lock (bucket)
        {
            bucket.LastAccessTime = now;

            var minuteWindowEnd = bucket.MinuteWindowStart.AddMinutes(1);
            var hourWindowEnd = bucket.HourWindowStart.AddHours(1);

            if (now >= minuteWindowEnd)
            {
                bucket.MinuteCount = 0;
                bucket.MinuteWindowStart = now;
            }

            if (now >= hourWindowEnd)
            {
                bucket.HourCount = 0;
                bucket.HourWindowStart = now;
            }

            if (bucket.MinuteCount >= _maxRequestsPerMinute)
            {
                var waitSeconds = (int)(minuteWindowEnd - now).TotalSeconds;
                return (false, $"Rate limit exceeded: {_maxRequestsPerMinute} requests per minute. Try again in {waitSeconds} seconds.");
            }

            if (bucket.HourCount >= _maxRequestsPerHour)
            {
                var waitMinutes = (int)(hourWindowEnd - now).TotalMinutes;
                return (false, $"Rate limit exceeded: {_maxRequestsPerHour} requests per hour. Try again in {waitMinutes} minutes.");
            }

            bucket.MinuteCount++;
            bucket.HourCount++;

            return (true, null);
        }
    }

    /// <summary>
    /// Get hash of token for rate limiting using SHA256.
    /// Only stores partial hash to avoid exposing full token.
    /// </summary>
    public static string GetTokenHash(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return "anonymous";
        }

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash)[..16];
    }

    /// <summary>
    /// Get remaining requests for the minute.
    /// </summary>
    public static int GetRemainingMinuteRequests(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            identifier = "anonymous";
        }

        if (_buckets.TryGetValue(identifier, out var bucket))
        {
            var now = DateTime.UtcNow;
            if (now >= bucket.MinuteWindowStart.AddMinutes(1))
            {
                return _maxRequestsPerMinute;
            }
            return Math.Max(0, _maxRequestsPerMinute - bucket.MinuteCount);
        }

        return _maxRequestsPerMinute;
    }

    /// <summary>
    /// Get remaining requests for the hour.
    /// </summary>
    public static int GetRemainingHourRequests(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            identifier = "anonymous";
        }

        if (_buckets.TryGetValue(identifier, out var bucket))
        {
            var now = DateTime.UtcNow;
            if (now >= bucket.HourWindowStart.AddHours(1))
            {
                return _maxRequestsPerHour;
            }
            return Math.Max(0, _maxRequestsPerHour - bucket.HourCount);
        }

        return _maxRequestsPerHour;
    }

    /// <summary>
    /// Reset rate limit for a specific identifier.
    /// </summary>
    public static void Reset(string identifier)
    {
        _buckets.TryRemove(identifier, out _);
    }

    /// <summary>
    /// Reset all rate limits.
    /// </summary>
    public static void ResetAll()
    {
        _buckets.Clear();
    }

    /// <summary>
    /// Get current number of tracked tokens.
    /// </summary>
    public static int GetTrackedTokenCount()
    {
        return _buckets.Count;
    }

    /// <summary>
    /// Clean up expired entries. Called automatically by timer.
    /// </summary>
    public static void Cleanup()
    {
        var now = DateTime.UtcNow;
        var expirationThreshold = now.AddHours(-EntryExpirationHours);

        foreach (var kvp in _buckets)
        {
            if (kvp.Value.LastAccessTime < expirationThreshold)
            {
                _buckets.TryRemove(kvp.Key, out _);
            }
        }
    }

    /// <summary>
    /// Get rate limit status for an identifier.
    /// </summary>
    public static (int minuteLimit, int hourLimit, int remainingMinute, int remainingHour) GetStatus(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            identifier = "anonymous";
        }

        if (_buckets.TryGetValue(identifier, out var bucket))
        {
            var now = DateTime.UtcNow;
            var minuteRemaining = now >= bucket.MinuteWindowStart.AddMinutes(1)
                ? _maxRequestsPerMinute
                : Math.Max(0, _maxRequestsPerMinute - bucket.MinuteCount);

            var hourRemaining = now >= bucket.HourWindowStart.AddHours(1)
                ? _maxRequestsPerHour
                : Math.Max(0, _maxRequestsPerHour - bucket.HourCount);

            return (_maxRequestsPerMinute, _maxRequestsPerHour, minuteRemaining, hourRemaining);
        }

        return (_maxRequestsPerMinute, _maxRequestsPerHour, _maxRequestsPerMinute, _maxRequestsPerHour);
    }

    /// <summary>
    /// Stop the cleanup timer. Call during application shutdown.
    /// </summary>
    public static void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}
