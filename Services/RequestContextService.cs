namespace McpVersionVer2.Services;

/// <summary>
/// Scoped service that stores the current request's bearer token and session ID.
/// Automatically set during validation and available for downstream services.
/// </summary>
public class RequestContextService
{
    private static readonly AsyncLocal<string?> _currentToken = new AsyncLocal<string?>();
    private static readonly AsyncLocal<string?> _currentSessionId = new AsyncLocal<string?>();
    
    private readonly ISessionStorageService _sessionStorage;
    
    public RequestContextService(ISessionStorageService sessionStorage)
    {
        _sessionStorage = sessionStorage;
    }
    
    public string? BearerToken
    {
        get => _currentToken.Value;
        set => _currentToken.Value = value;
    }
    
    public string SessionId
    {
        get
        {
            if (!string.IsNullOrEmpty(_currentSessionId.Value))
            {
                return _currentSessionId.Value;
            }
            
            if (!string.IsNullOrEmpty(BearerToken))
            {
                var sessionId = _sessionStorage.GetOrCreateSessionId(BearerToken);
                _currentSessionId.Value = sessionId;
                return sessionId;
            }
            
            return "anonymous";
        }
    }
    
    public static void SetToken(string? token)
    {
        _currentToken.Value = token;
    }
    
    public static string? GetToken()
    {
        return _currentToken.Value;
    }
    
    public static void Clear()
    {
        _currentToken.Value = null;
        _currentSessionId.Value = null;
    }
}
