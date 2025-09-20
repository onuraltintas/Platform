using System.Text.Json.Serialization;

namespace SpeedReading.API.Models;

/// <summary>
/// Standard API response wrapper for all endpoints
/// </summary>
/// <typeparam name="T">The type of data being returned</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates if the request was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The response data
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Error message if the request failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Additional error details for debugging
    /// </summary>
    public object? ErrorDetails { get; set; }

    /// <summary>
    /// Response timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful response
    /// </summary>
    public static ApiResponse<T> Ok(T data)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data
        };
    }

    /// <summary>
    /// Creates a successful response without data
    /// </summary>
    public static ApiResponse<T> Ok()
    {
        return new ApiResponse<T>
        {
            Success = true
        };
    }

    /// <summary>
    /// Creates an error response
    /// </summary>
    public static ApiResponse<T> Fail(string error, object? errorDetails = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = error,
            ErrorDetails = errorDetails
        };
    }
}

/// <summary>
/// Standard API response without data
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// Indicates if the request was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the request failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Additional error details for debugging
    /// </summary>
    public object? ErrorDetails { get; set; }

    /// <summary>
    /// Response timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful response
    /// </summary>
    public static ApiResponse Ok()
    {
        return new ApiResponse
        {
            Success = true
        };
    }

    /// <summary>
    /// Creates an error response
    /// </summary>
    public static ApiResponse Fail(string error, object? errorDetails = null)
    {
        return new ApiResponse
        {
            Success = false,
            Error = error,
            ErrorDetails = errorDetails
        };
    }
}

/// <summary>
/// Paginated response wrapper
/// </summary>
/// <typeparam name="T">The type of items in the collection</typeparam>
public class PagedApiResponse<T>
{
    /// <summary>
    /// Indicates if the request was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The response data
    /// </summary>
    public IEnumerable<T>? Data { get; set; }

    /// <summary>
    /// Error message if the request failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Additional error details for debugging
    /// </summary>
    public object? ErrorDetails { get; set; }

    /// <summary>
    /// Response timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

    /// <summary>
    /// Indicates if there's a next page
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Indicates if there's a previous page
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Creates a successful paginated response
    /// </summary>
    public static PagedApiResponse<T> Ok(IEnumerable<T> data, int page, int pageSize, int totalItems)
    {
        return new PagedApiResponse<T>
        {
            Success = true,
            Data = data,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    /// <summary>
    /// Creates an error paginated response
    /// </summary>
    public static PagedApiResponse<T> Fail(string error, object? errorDetails = null)
    {
        return new PagedApiResponse<T>
        {
            Success = false,
            Error = error,
            ErrorDetails = errorDetails
        };
    }
}