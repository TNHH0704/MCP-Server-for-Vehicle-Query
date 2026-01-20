namespace McpVersionVer2.Services;

public static class ServiceExtensions
{
    public static async Task<List<T>> SafeGetListAsync<T>(this Task<List<T>> task, string entityName)
    {
        try
        {
            var result = await task;
            return result ?? new List<T>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to retrieve {entityName}: {ex.Message}", ex);
        }
    }

    public static async Task<T?> SafeGetSingleAsync<T>(this Task<T?> task, string entityName, string? identifier = null)
        where T : class
    {
        try
        {
            var result = await task;
            if (result == null)
            {
                var idPart = string.IsNullOrEmpty(identifier) ? "" : $" with {identifier}";
                throw new InvalidOperationException($"No {entityName} found{idPart}.");
            }
            return result;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to retrieve {entityName}: {ex.Message}", ex);
        }
    }
}
