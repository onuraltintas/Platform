using System.Net.Http.Json;
using Identity.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Identity.Infrastructure.Services;

public class GatewayCacheInvalidationClient : IGatewayCacheInvalidationClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _gatewayBaseUrl;
    private readonly string _internalApiKey;

    public GatewayCacheInvalidationClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _gatewayBaseUrl = configuration["GATEWAY_BASE_URL"]
            ?? Environment.GetEnvironmentVariable("GATEWAY_BASE_URL")
            ?? "http://localhost:5000";
        _internalApiKey = configuration["GATEWAY_INTERNAL_API_KEY"]
            ?? Environment.GetEnvironmentVariable("GATEWAY_INTERNAL_API_KEY")
            ?? string.Empty;
    }

    public async Task InvalidateUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{_gatewayBaseUrl}/api/gateway/authorization/users/{userId}/cache/internal");
        if (!string.IsNullOrEmpty(_internalApiKey))
        {
            request.Headers.Add("X-Internal-API-Key", _internalApiKey);
        }
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task BulkInvalidateAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"{_gatewayBaseUrl}/api/gateway/authorization/users/bulk-invalidate/internal";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (!string.IsNullOrEmpty(_internalApiKey))
        {
            request.Headers.Add("X-Internal-API-Key", _internalApiKey);
        }
        request.Content = JsonContent.Create(new { userIds });
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

