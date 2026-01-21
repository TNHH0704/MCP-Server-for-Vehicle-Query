namespace McpVersionVer2.Security;

public class ToolValidationException : Exception
{
    public string ErrorResponse { get; }

    public ToolValidationException(string errorResponse)
        : base("Tool validation failed")
    {
        ErrorResponse = errorResponse;
    }
}
