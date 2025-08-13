using Microsoft.Extensions.Logging;
using Polly;
using System.Text;
using System.Text.Json;
using EgitimPlatform.Shared.Resilience.Policies;

namespace EgitimPlatform.Shared.Resilience.Services;

public interface IResilientHttpClient
{
    Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default);
    Task<T?> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default) where T : class;
    Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent? content, CancellationToken cancellationToken = default);
    Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default) 
        where TRequest : class where TResponse : class;
    Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent? content, CancellationToken cancellationToken = default);
    Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default) 
        where TRequest : class where TResponse : class;
    Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);
}

public class ResilientHttpClient : IResilientHttpClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ResiliencePipeline<HttpResponseMessage> _resiliencePipeline;
    private readonly ILogger<ResilientHttpClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ResilientHttpClient(
        HttpClient httpClient,
        IResiliencePolicyFactory policyFactory,
        ILogger<ResilientHttpClient> logger,
        string? serviceName = null)
    {
        _httpClient = httpClient;
        _resiliencePipeline = policyFactory.CreateHttpPipeline<HttpResponseMessage>(serviceName);
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithResilience(
            async () => await _httpClient.GetAsync(requestUri, cancellationToken),
            $"GET {requestUri}",
            cancellationToken);
    }

    public async Task<T?> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default) where T : class
    {
        var response = await GetAsync(requestUri, cancellationToken);
        return await DeserializeResponse<T>(response);
    }

    public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent? content, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithResilience(
            async () => await _httpClient.PostAsync(requestUri, content, cancellationToken),
            $"POST {requestUri}",
            cancellationToken);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default) 
        where TRequest : class where TResponse : class
    {
        var content = SerializeRequest(request);
        var response = await PostAsync(requestUri, content, cancellationToken);
        return await DeserializeResponse<TResponse>(response);
    }

    public async Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent? content, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithResilience(
            async () => await _httpClient.PutAsync(requestUri, content, cancellationToken),
            $"PUT {requestUri}",
            cancellationToken);
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default) 
        where TRequest : class where TResponse : class
    {
        var content = SerializeRequest(request);
        var response = await PutAsync(requestUri, content, cancellationToken);
        return await DeserializeResponse<TResponse>(response);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithResilience(
            async () => await _httpClient.DeleteAsync(requestUri, cancellationToken),
            $"DELETE {requestUri}",
            cancellationToken);
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithResilience(
            async () => await _httpClient.SendAsync(request, cancellationToken),
            $"{request.Method} {request.RequestUri}",
            cancellationToken);
    }

    private async Task<HttpResponseMessage> ExecuteWithResilience(
        Func<Task<HttpResponseMessage>> operation, 
        string operationName, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Executing resilient HTTP operation: {OperationName}", operationName);

            var result = await _resiliencePipeline.ExecuteAsync(async (ct) =>
            {
                return await operation();
            }, cancellationToken);

            _logger.LogDebug("Successfully completed HTTP operation: {OperationName} with status {StatusCode}", 
                operationName, result.StatusCode);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute HTTP operation: {OperationName}", operationName);
            throw;
        }
    }

    private StringContent SerializeRequest<T>(T request) where T : class
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private async Task<T?> DeserializeResponse<T>(HttpResponseMessage response) where T : class
    {
        try
        {
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(content, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed with status {StatusCode}", response.StatusCode);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize response to {Type}", typeof(T).Name);
            throw;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}