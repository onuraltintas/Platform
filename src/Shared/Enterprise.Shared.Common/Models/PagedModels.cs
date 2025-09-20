using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Enterprise.Shared.Common.Enums;

namespace Enterprise.Shared.Common.Models;

/// <summary>
/// Request model for paginated queries
/// </summary>
public class PagedRequest
{
    /// <summary>
    /// Gets or sets the page number (1-based)
    /// </summary>
    [JsonPropertyName("page")]
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of items per page
    /// </summary>
    [JsonPropertyName("pageSize")]
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Gets or sets the field to sort by
    /// </summary>
    [JsonPropertyName("sortBy")]
    public string? SortBy { get; set; }

    /// <summary>
    /// Gets or sets the sort direction
    /// </summary>
    [JsonPropertyName("sortDirection")]
    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;

    /// <summary>
    /// Gets or sets the search term
    /// </summary>
    [JsonPropertyName("search")]
    public string? Search { get; set; }

    /// <summary>
    /// Gets or sets additional filters
    /// </summary>
    [JsonPropertyName("filters")]
    public Dictionary<string, object> Filters { get; set; } = [];

    /// <summary>
    /// Gets the number of items to skip
    /// </summary>
    [JsonIgnore]
    public int Skip => (Page - 1) * PageSize;

    /// <summary>
    /// Gets the number of items to take
    /// </summary>
    [JsonIgnore]
    public int Take => PageSize;

    /// <summary>
    /// Validates and normalizes the page request
    /// </summary>
    public void Normalize()
    {
        if (Page < 1) Page = 1;
        if (PageSize < 1) PageSize = 10;
        if (PageSize > 100) PageSize = 100;

        // Trim and normalize search term
        if (!string.IsNullOrEmpty(Search))
        {
            Search = Search.Trim();
            if (string.IsNullOrEmpty(Search)) Search = null;
        }

        // Normalize sort field
        if (!string.IsNullOrEmpty(SortBy))
        {
            SortBy = SortBy.Trim();
            if (string.IsNullOrEmpty(SortBy)) SortBy = null;
        }
    }

    /// <summary>
    /// Adds a filter to the request
    /// </summary>
    /// <param name="key">The filter key</param>
    /// <param name="value">The filter value</param>
    /// <returns>The request instance for method chaining</returns>
    public PagedRequest AddFilter(string key, object value)
    {
        Filters[key] = value;
        return this;
    }

    /// <summary>
    /// Removes a filter from the request
    /// </summary>
    /// <param name="key">The filter key to remove</param>
    /// <returns>The request instance for method chaining</returns>
    public PagedRequest RemoveFilter(string key)
    {
        Filters.Remove(key);
        return this;
    }

    /// <summary>
    /// Gets a filter value by key
    /// </summary>
    /// <typeparam name="T">The type of the filter value</typeparam>
    /// <param name="key">The filter key</param>
    /// <param name="defaultValue">The default value if not found</param>
    /// <returns>The filter value</returns>
    public T? GetFilter<T>(string key, T? defaultValue = default)
    {
        if (Filters.TryGetValue(key, out var value))
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// Checks if a filter exists
    /// </summary>
    /// <param name="key">The filter key</param>
    /// <returns>True if the filter exists</returns>
    public bool HasFilter(string key)
    {
        return Filters.ContainsKey(key);
    }

    /// <summary>
    /// Creates a copy of the request with a different page number
    /// </summary>
    /// <param name="pageNumber">The new page number</param>
    /// <returns>A new paged request</returns>
    public PagedRequest WithPage(int pageNumber)
    {
        return new PagedRequest
        {
            Page = pageNumber,
            PageSize = PageSize,
            SortBy = SortBy,
            SortDirection = SortDirection,
            Search = Search,
            Filters = new Dictionary<string, object>(Filters)
        };
    }

    /// <summary>
    /// Creates a copy of the request with a different page size
    /// </summary>
    /// <param name="pageSize">The new page size</param>
    /// <returns>A new paged request</returns>
    public PagedRequest WithPageSize(int pageSize)
    {
        return new PagedRequest
        {
            Page = Page,
            PageSize = pageSize,
            SortBy = SortBy,
            SortDirection = SortDirection,
            Search = Search,
            Filters = new Dictionary<string, object>(Filters)
        };
    }
}

/// <summary>
/// Result model for paginated queries
/// </summary>
/// <typeparam name="T">The type of items in the result</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Gets or sets the data items for the current page
    /// </summary>
    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = [];

    /// <summary>
    /// Gets or sets the total count of items across all pages
    /// </summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current page number
    /// </summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the page size
    /// </summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    /// <summary>
    /// Gets the total number of pages
    /// </summary>
    [JsonPropertyName("totalPages")]
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Gets a value indicating whether there is a next page
    /// </summary>
    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Gets a value indicating whether there is a previous page
    /// </summary>
    [JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Gets the next page number or null if there is no next page
    /// </summary>
    [JsonPropertyName("nextPage")]
    public int? NextPage => HasNextPage ? Page + 1 : null;

    /// <summary>
    /// Gets the previous page number or null if there is no previous page
    /// </summary>
    [JsonPropertyName("previousPage")]
    public int? PreviousPage => HasPreviousPage ? Page - 1 : null;

    /// <summary>
    /// Gets the starting item number for the current page
    /// </summary>
    [JsonPropertyName("startItem")]
    public int StartItem => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;

    /// <summary>
    /// Gets the ending item number for the current page
    /// </summary>
    [JsonPropertyName("endItem")]
    public int EndItem 
    {
        get
        {
            if (TotalCount == 0) return 0;
            
            // If we have actual data, use the actual count from start position
            if (Data.Count > 0)
            {
                return StartItem + Data.Count - 1;
            }
            
            // Otherwise, calculate theoretical end based on page size
            return Math.Min(Page * PageSize, TotalCount);
        }
    }

    /// <summary>
    /// Gets a value indicating whether the result is empty
    /// </summary>
    [JsonIgnore]
    public bool IsEmpty => Data.Count == 0;

    /// <summary>
    /// Gets a value indicating whether this is the first page
    /// </summary>
    [JsonIgnore]
    public bool IsFirstPage => Page == 1;

    /// <summary>
    /// Gets a value indicating whether this is the last page
    /// </summary>
    [JsonIgnore]
    public bool IsLastPage => Page == TotalPages;

    /// <summary>
    /// Initializes a new instance of the PagedResult{T} class
    /// </summary>
    public PagedResult() { }

    /// <summary>
    /// Initializes a new instance of the PagedResult{T} class
    /// </summary>
    /// <param name="data">The data items</param>
    /// <param name="totalCount">The total count</param>
    /// <param name="page">The page number</param>
    /// <param name="pageSize">The page size</param>
    public PagedResult(List<T> data, int totalCount, int page, int pageSize)
    {
        Data = data ?? [];
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    /// <summary>
    /// Creates a new paged result
    /// </summary>
    /// <param name="data">The data items</param>
    /// <param name="totalCount">The total count</param>
    /// <param name="page">The page number</param>
    /// <param name="pageSize">The page size</param>
    /// <returns>A new paged result</returns>
    public static PagedResult<T> Create(List<T> data, int totalCount, int page, int pageSize)
    {
        return new PagedResult<T>(data, totalCount, page, pageSize);
    }

    /// <summary>
    /// Creates an empty paged result
    /// </summary>
    /// <param name="page">The page number</param>
    /// <param name="pageSize">The page size</param>
    /// <returns>An empty paged result</returns>
    public static PagedResult<T> Empty(int page = 1, int pageSize = 10)
    {
        return new PagedResult<T>([], 0, page, pageSize);
    }

    /// <summary>
    /// Maps the paged result to another type
    /// </summary>
    /// <typeparam name="TOutput">The output type</typeparam>
    /// <param name="mapper">The mapping function</param>
    /// <returns>A paged result with mapped data</returns>
    public PagedResult<TOutput> Map<TOutput>(Func<T, TOutput> mapper)
    {
        var mappedData = Data.Select(mapper).ToList();
        return PagedResult<TOutput>.Create(mappedData, TotalCount, Page, PageSize);
    }

    /// <summary>
    /// Filters the current page data
    /// </summary>
    /// <param name="predicate">The filter predicate</param>
    /// <returns>A new paged result with filtered data</returns>
    public PagedResult<T> Filter(Func<T, bool> predicate)
    {
        var filteredData = Data.Where(predicate).ToList();
        return PagedResult<T>.Create(filteredData, filteredData.Count, Page, PageSize);
    }

    /// <summary>
    /// Gets page information as a formatted string
    /// </summary>
    /// <returns>Page information string</returns>
    public string GetPageInfo()
    {
        if (TotalCount == 0)
            return "No items";

        return $"Showing {StartItem}-{EndItem} of {TotalCount} items (Page {Page} of {TotalPages})";
    }

    /// <summary>
    /// Converts the paged result to a simple list
    /// </summary>
    /// <returns>The data items as a list</returns>
    public List<T> ToList()
    {
        return Data.ToList();
    }

    /// <summary>
    /// Implicit conversion from PagedResult{T} to List{T}
    /// </summary>
    /// <param name="pagedResult">The paged result</param>
    public static implicit operator List<T>(PagedResult<T> pagedResult) => pagedResult.Data;
}