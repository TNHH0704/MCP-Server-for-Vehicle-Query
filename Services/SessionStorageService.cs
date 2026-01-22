using System.Collections.Concurrent;

namespace McpVersionVer2.Services;

public interface ISessionStorageService
{
    /// <summary>
    /// Gets or creates a unique session ID for the given bearer token
    /// </summary>
    string GetOrCreateSessionId(string bearerToken);
    
    /// <summary>
    /// Gets the session ID associated with a bearer token, if it exists
    /// </summary>
    string? GetSessionId(string bearerToken);
    
    /// <summary>
    /// Clears the session mapping for a specific bearer token
    /// </summary>
    void ClearSession(string bearerToken);
    
    /// <summary>
    /// Gets all active session IDs
    /// </summary>
    IEnumerable<string> GetAllSessionIds();
}

public class InMemorySessionStorageService : ISessionStorageService
{
    private readonly ConcurrentDictionary<string, string> _tokenToSessionMap = new();
    
    private readonly ConcurrentDictionary<string, DateTime> _sessionLastAccess = new();
    
    private readonly TimeSpan _sessionTimeout = TimeSpan.FromHours(24);
    
    public string GetOrCreateSessionId(string bearerToken)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            return "anonymous";
        }
        
        var tokenHash = GetTokenHash(bearerToken);
        
        CleanupExpiredSessions();
        
        var sessionId = _tokenToSessionMap.GetOrAdd(tokenHash, _ => GenerateSessionId());
        
        _sessionLastAccess[sessionId] = DateTime.UtcNow;
        
        return sessionId;
    }
    
    public string? GetSessionId(string bearerToken)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            return null;
        }
        
        var tokenHash = GetTokenHash(bearerToken);
        
        if (_tokenToSessionMap.TryGetValue(tokenHash, out var sessionId))
        {
            _sessionLastAccess[sessionId] = DateTime.UtcNow;
            return sessionId;
        }
        
        return null;
    }
    
    public void ClearSession(string bearerToken)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            return;
        }
        
        var tokenHash = GetTokenHash(bearerToken);
        
        if (_tokenToSessionMap.TryRemove(tokenHash, out var sessionId))
        {
            _sessionLastAccess.TryRemove(sessionId, out _);
        }
    }
    
    public IEnumerable<string> GetAllSessionIds()
    {
        return _sessionLastAccess.Keys.ToList();
    }
    
    /// <summary>
    /// Generates a unique session ID in the format: ses_[22-char-base64]
    /// </summary>
    private static string GenerateSessionId()
    {
        var randomBytes = new byte[16];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        
        var base64 = Convert.ToBase64String(randomBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        
        return $"ses_{base64}";
    }
    
    /// <summary>
    /// Creates a SHA256 hash of the bearer token for storage
    /// </summary>
    private static string GetTokenHash(string token)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashBytes).ToLower();
    }
    
    /// <summary>
    /// Cleans up sessions that haven't been accessed within the timeout period
    /// </summary>
    private void CleanupExpiredSessions()
    {
        var now = DateTime.UtcNow;
        var expiredSessions = _sessionLastAccess
            .Where(kvp => now - kvp.Value > _sessionTimeout)
            .Select(kvp => kvp.Key)
            .ToList();
        
        foreach (var sessionId in expiredSessions)
        {
            _sessionLastAccess.TryRemove(sessionId, out _);
            
            var tokenHashToRemove = _tokenToSessionMap
                .FirstOrDefault(kvp => kvp.Value == sessionId)
                .Key;
            
            if (tokenHashToRemove != null)
            {
                _tokenToSessionMap.TryRemove(tokenHashToRemove, out _);
            }
        }
    }
}
