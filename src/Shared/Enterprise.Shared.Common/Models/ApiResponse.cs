using System.Net;
using System.Text.Json.Serialization;

namespace Enterprise.Shared.Common.Models;

/// <summary>
/// Standard API response wrapper without data
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the request was successful
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code
    /// </summary>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the response message
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of error messages
    /// </summary>
    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// Gets or sets the timestamp when the response was created
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the correlation ID for request tracking
    /// </summary>
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets additional metadata
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = [];

    /// <summary>
    /// Initializes a new instance of the ApiResponse class
    /// </summary>
    public ApiResponse() { }

    /// <summary>
    /// Initializes a new instance of the ApiResponse class
    /// </summary>
    /// <param name="success">Whether the request was successful</param>
    /// <param name="statusCode">The HTTP status code</param>
    /// <param name="message">The response message</param>
    protected ApiResponse(bool success, int statusCode, string message)
    {
        Success = success;
        StatusCode = statusCode;
        Message = message;
    }

    /// <summary>
    /// Creates a successful API response
    /// </summary>
    /// <param name="message">The success message</param>
    /// <param name="statusCode">The HTTP status code</param>
    /// <returns>A successful API response</returns>
    public static ApiResponse SuccessResponse(string message = "Request completed successfully", 
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new ApiResponse(true, (int)statusCode, message);
    }

    /// <summary>
    /// Creates an error API response
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="statusCode">The HTTP status code</param>
    /// <returns>An error API response</returns>
    public static ApiResponse ErrorResponse(string message, 
        HttpStatusCode statusCode = HttpStatusCode.BadRequest)
    {
        return new ApiResponse(false, (int)statusCode, message);
    }

    /// <summary>
    /// Creates an error API response with multiple errors
    /// </summary>
    /// <param name="errors">The error messages</param>
    /// <param name="message">The primary error message</param>
    /// <param name="statusCode">The HTTP status code</param>
    /// <returns>An error API response</returns>
    public static ApiResponse ErrorResponse(IEnumerable<string> errors, 
        string message = "One or more errors occurred", 
        HttpStatusCode statusCode = HttpStatusCode.BadRequest)
    {
        var response = new ApiResponse(false, (int)statusCode, message);
        response.Errors = errors.ToList();
        return response;
    }

    /// <summary>
    /// Creates an API response from a Result
    /// </summary>
    /// <param name="result">The result to convert</param>
    /// <param name="successStatusCode">The status code for successful results</param>
    /// <param name="errorStatusCode">The status code for failed results</param>
    /// <returns>An API response</returns>
    public static ApiResponse FromResult(Result result, 
        HttpStatusCode successStatusCode = HttpStatusCode.OK,
        HttpStatusCode errorStatusCode = HttpStatusCode.BadRequest)
    {
        if (result.IsSuccess)
        {
            var response = SuccessResponse(
                result.Error.HasValue() ? result.Error : "Request completed successfully",
                successStatusCode);
            response.Metadata = result.Metadata;
            return response;
        }

        var errorResponse = ErrorResponse(result.Errors, result.Error, errorStatusCode);
        errorResponse.Metadata = result.Metadata;
        return errorResponse;
    }

    /// <summary>
    /// Adds metadata to the response
    /// </summary>
    /// <param name="key">The metadata key</param>
    /// <param name="value">The metadata value</param>
    /// <returns>The response instance for method chaining</returns>
    public ApiResponse WithMetadata(string key, object value)
    {
        Metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Adds a correlation ID to the response
    /// </summary>
    /// <param name="correlationId">The correlation ID</param>
    /// <returns>The response instance for method chaining</returns>
    public ApiResponse WithCorrelationId(string correlationId)
    {
        CorrelationId = correlationId;
        return this;
    }

    /// <summary>
    /// Validates the response and throws an exception if it represents an error
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the response indicates failure</exception>
    public void EnsureSuccess()
    {
        if (!Success)
        {
            throw new InvalidOperationException($"API response indicates failure: {Message}");
        }
    }
}

/// <summary>
/// Standard API response wrapper with data
/// </summary>
/// <typeparam name="T">The type of the response data</typeparam>
public class ApiResponse<T> : ApiResponse
{
    /// <summary>
    /// Gets or sets the response data
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    /// <summary>
    /// Gets a value indicating whether the response has data
    /// </summary>
    [JsonIgnore]
    public bool HasData => Data != null;

    /// <summary>
    /// Initializes a new instance of the ApiResponse{T} class
    /// </summary>
    public ApiResponse() { }

    /// <summary>
    /// Initializes a new instance of the ApiResponse{T} class
    /// </summary>
    /// <param name="success">Whether the request was successful</param>
    /// <param name="statusCode">The HTTP status code</param>
    /// <param name="message">The response message</param>
    /// <param name="data">The response data</param>
    protected ApiResponse(bool success, int statusCode, string message, T? data) 
        : base(success, statusCode, message)
    {
        Data = data;
    }

    /// <summary>
    /// Creates a successful API response with data
    /// </summary>
    /// <param name="data">The response data</param>
    /// <param name="message">The success message</param>
    /// <param name="statusCode">The HTTP status code</param>
    /// <returns>A successful API response with data</returns>
    public static ApiResponse<T> SuccessResponse(T data, 
        string message = "Request completed successfully", 
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new ApiResponse<T>(true, (int)statusCode, message, data);
    }

    /// <summary>
    /// Creates an error API response without data
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="statusCode">The HTTP status code</param>
    /// <returns>An error API response</returns>
    public static new ApiResponse<T> ErrorResponse(string message, 
        HttpStatusCode statusCode = HttpStatusCode.BadRequest)
    {
        return new ApiResponse<T>(false, (int)statusCode, message, default);
    }

    /// <summary>
    /// Creates an error API response with multiple errors
    /// </summary>
    /// <param name="errors">The error messages</param>
    /// <param name="message">The primary error message</param>
    /// <param name="statusCode">The HTTP status code</param>
    /// <returns>An error API response</returns>
    public static new ApiResponse<T> ErrorResponse(IEnumerable<string> errors, 
        string message = "One or more errors occurred", 
        HttpStatusCode statusCode = HttpStatusCode.BadRequest)
    {
        var response = new ApiResponse<T>(false, (int)statusCode, message, default);
        response.Errors = errors.ToList();
        return response;
    }

    /// <summary>
    /// Creates an API response from a Result{T}
    /// </summary>
    /// <param name="result">The result to convert</param>
    /// <param name="successStatusCode">The status code for successful results</param>
    /// <param name="errorStatusCode">The status code for failed results</param>
    /// <returns>An API response</returns>
    public static ApiResponse<T> FromResult(Result<T> result, 
        HttpStatusCode successStatusCode = HttpStatusCode.OK,
        HttpStatusCode errorStatusCode = HttpStatusCode.BadRequest)
    {
        if (result.IsSuccess)
        {
            var response = SuccessResponse(
                result.Value!,
                result.Error.HasValue() ? result.Error : "Request completed successfully",
                successStatusCode);
            response.Metadata = result.Metadata;
            return response;
        }

        var errorResponse = ErrorResponse(result.Errors, result.Error, errorStatusCode);
        errorResponse.Metadata = result.Metadata;
        return errorResponse;
    }

    /// <summary>
    /// Maps the response data to another type
    /// </summary>
    /// <typeparam name="TOutput">The output type</typeparam>
    /// <param name="mapper">The mapping function</param>
    /// <returns>A response with the mapped data</returns>
    public ApiResponse<TOutput> Map<TOutput>(Func<T, TOutput> mapper)
    {
        if (!Success || Data == null)
        {
            return new ApiResponse<TOutput>(Success, StatusCode, Message, default)
            {
                Errors = Errors,
                Timestamp = Timestamp,
                CorrelationId = CorrelationId,
                Metadata = Metadata
            };
        }

        try
        {
            var mappedData = mapper(Data);
            return new ApiResponse<TOutput>(Success, StatusCode, Message, mappedData)
            {
                Errors = Errors,
                Timestamp = Timestamp,
                CorrelationId = CorrelationId,
                Metadata = Metadata
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<TOutput>(false, (int)HttpStatusCode.InternalServerError, ex.Message, default)
            {
                Timestamp = Timestamp,
                CorrelationId = CorrelationId,
                Metadata = Metadata
            };
        }
    }

    /// <summary>
    /// Gets the data if successful, otherwise returns the default value
    /// </summary>
    /// <param name="defaultValue">The default value to return on failure</param>
    /// <returns>The data or default value</returns>
    public T? GetDataOrDefault(T? defaultValue = default)
    {
        return Success && HasData ? Data : defaultValue;
    }

    /// <summary>
    /// Implicit conversion from T to successful ApiResponse{T}
    /// </summary>
    /// <param name="data">The data</param>
    public static implicit operator ApiResponse<T>(T data) => SuccessResponse(data);

    /// <summary>
    /// Implicit conversion from Result{T} to ApiResponse{T}
    /// </summary>
    /// <param name="result">The result</param>
    public static implicit operator ApiResponse<T>(Result<T> result) => FromResult(result);
}

/// <summary>
/// Paged API response wrapper
/// </summary>
/// <typeparam name="T">The type of the response data items</typeparam>
public class PagedApiResponse<T> : ApiResponse<PagedResult<T>>
{
    /// <summary>
    /// Gets or sets pagination information
    /// </summary>
    [JsonPropertyName("pagination")]
    public PaginationInfo Pagination { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the PagedApiResponse{T} class
    /// </summary>
    public PagedApiResponse() { }

    /// <summary>
    /// Creates a successful paged API response
    /// </summary>
    /// <param name="pagedResult">The paged result</param>
    /// <param name="message">The success message</param>
    /// <param name="statusCode">The HTTP status code</param>
    /// <returns>A successful paged API response</returns>
    public static new PagedApiResponse<T> SuccessResponse(PagedResult<T> pagedResult, 
        string message = "Request completed successfully", 
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new PagedApiResponse<T>
        {
            Success = true,
            StatusCode = (int)statusCode,
            Message = message,
            Data = pagedResult,
            Pagination = new PaginationInfo
            {
                Page = pagedResult.Page,
                PageSize = pagedResult.PageSize,
                TotalCount = pagedResult.TotalCount,
                TotalPages = pagedResult.TotalPages,
                HasNextPage = pagedResult.HasNextPage,
                HasPreviousPage = pagedResult.HasPreviousPage
            }
        };
    }
}

/// <summary>
/// Pagination information for paged responses
/// </summary>
public class PaginationInfo
{
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
    /// Gets or sets the total count of items
    /// </summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of pages
    /// </summary>
    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether there is a next page
    /// </summary>
    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether there is a previous page
    /// </summary>
    [JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage { get; set; }
}