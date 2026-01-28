namespace McpVersionVer2.Services.Mappers;

/// <summary>
/// Generic interface for mapping between source and destination types.
/// </summary>
public interface IMapper<TSource, TDestination>
{
    /// <summary>
    /// Maps a single source object to destination type.
    /// </summary>
    TDestination MapToDto(TSource source);

    /// <summary>
    /// Maps a collection of source objects to destination types.
    /// </summary>
    List<TDestination> MapToDtos(IEnumerable<TSource> sources);
}

/// <summary>
/// Helper methods for common mapping operations.
/// </summary>
public static class MapperHelpers
{
    /// <summary>
    /// Maps a collection of source objects to destination types using a mapper.
    /// </summary>
    public static List<TDestination> MapList<TSource, TDestination>(
        this IMapper<TSource, TDestination> mapper,
        IEnumerable<TSource> sources)
    {
        return sources.Select(mapper.MapToDto).ToList();
    }

    /// <summary>
    /// Creates a search result wrapper for a list of DTOs.
    /// </summary>
    public static SearchResult<TDto> CreateSearchResult<TDto>(
        IEnumerable<TDto> items,
        string searchCriteria)
    {
        return new SearchResult<TDto>
        {
            TotalCount = items.Count(),
            Vehicles = items.ToList(),
            SearchCriteria = searchCriteria
        };
    }
}

/// <summary>
/// Generic search result wrapper.
/// </summary>
public class SearchResult<T>
{
    public int TotalCount { get; set; }
    public List<T> Vehicles { get; set; } = new();
    public string SearchCriteria { get; set; } = string.Empty;
}