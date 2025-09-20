namespace Enterprise.Shared.Validation.Models;

/// <summary>
/// Represents a paginated request with sorting and filtering
/// </summary>
public class PagedRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;
    public string? Search { get; set; }
    public Dictionary<string, object> Filters { get; set; } = new();

    public int Skip => (Page - 1) * PageSize;
    public int Take => PageSize;

    public void ValidatePage()
    {
        if (Page < 1) Page = 1;
        if (PageSize < 1) PageSize = 10;
        if (PageSize > 100) PageSize = 100;
    }
}

/// <summary>
/// Represents a paginated result
/// </summary>
/// <typeparam name="T">The type of items in the result</typeparam>
public class PagedResult<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
    public int? NextPage => HasNextPage ? Page + 1 : null;
    public int? PreviousPage => HasPreviousPage ? Page - 1 : null;

    public static PagedResult<T> Create(List<T> data, int totalCount, int page, int pageSize)
    {
        return new PagedResult<T>
        {
            Data = data,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}

/// <summary>
/// Sort direction enum for pagination
/// </summary>
public enum SortDirection
{
    Ascending,
    Descending
}