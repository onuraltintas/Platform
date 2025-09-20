using Identity.Core.Interfaces;
using Identity.Core.Models;
using Identity.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace Identity.Application.Services;

public interface IConditionalPermissionService
{
    Task<bool> EvaluatePermissionConditionsAsync(string userId, string permission, Guid? groupId = null, string? resource = null);
    Task<PermissionCondition?> GetPermissionConditionsAsync(string roleId, Guid permissionId, Guid? groupId = null);
    Task<bool> SetPermissionConditionsAsync(string roleId, Guid permissionId, PermissionCondition conditions, Guid? groupId = null);
}

public class ConditionalPermissionService : IConditionalPermissionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ConditionalPermissionService> _logger;
    private readonly IMemoryCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConditionalPermissionService(
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ConditionalPermissionService> logger,
        IMemoryCache cache)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _cache = cache;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new PermissionConditionJsonConverter() }
        };
    }

    public async Task<bool> EvaluatePermissionConditionsAsync(
        string userId,
        string permission,
        Guid? groupId = null,
        string? resource = null)
    {
        try
        {
            // Simplified implementation for now - always return true for basic permissions
            // TODO: Implement full conditional permission logic when RolePermission repository is available
            _logger.LogDebug("Conditional permission check for {Permission} - returning true (basic implementation)", permission);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating permission conditions for user {UserId}", userId);
            return false;
        }
    }

    public async Task<PermissionCondition?> GetPermissionConditionsAsync(
        string roleId,
        Guid permissionId,
        Guid? groupId = null)
    {
        try
        {
            // Simplified implementation for now
            _logger.LogDebug("GetPermissionConditionsAsync - returning null (basic implementation)");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permission conditions for role {RoleId}", roleId);
            return null;
        }
    }

    public async Task<bool> SetPermissionConditionsAsync(
        string roleId,
        Guid permissionId,
        PermissionCondition conditions,
        Guid? groupId = null)
    {
        try
        {
            // Simplified implementation for now
            _logger.LogDebug("SetPermissionConditionsAsync - returning true (basic implementation)");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting permission conditions for role {RoleId}", roleId);
            return false;
        }
    }

    private PermissionConditionContext CreateConditionContext(
        string userId,
        Guid? groupId,
        string? resource)
    {
        var context = new PermissionConditionContext
        {
            UserId = userId,
            GroupId = groupId,
            Resource = resource,
            RequestTime = DateTime.UtcNow
        };

        // Get IP address from HTTP context
        if (_httpContextAccessor.HttpContext != null)
        {
            context.IpAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString();

            // Add custom data from headers or claims
            if (_httpContextAccessor.HttpContext.Request.Headers.TryGetValue("X-Department", out var dept))
            {
                context.CustomData["department"] = dept.ToString();
            }

            // Add any amount from query or body if present
            if (_httpContextAccessor.HttpContext.Request.Query.TryGetValue("amount", out var amount))
            {
                if (decimal.TryParse(amount, out var amountValue))
                {
                    context.CustomData["amount"] = amountValue;
                }
            }
        }

        return context;
    }
}

/// <summary>
/// Custom JSON converter for PermissionCondition polymorphic deserialization
/// </summary>
public class PermissionConditionJsonConverter : System.Text.Json.Serialization.JsonConverter<PermissionCondition>
{
    public override PermissionCondition? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeElement))
            return null;

        var type = typeElement.GetString();
        var json = root.GetRawText();

        return type switch
        {
            "time" => JsonSerializer.Deserialize<TimeCondition>(json, options),
            "location" => JsonSerializer.Deserialize<LocationCondition>(json, options),
            "resource" => JsonSerializer.Deserialize<ResourceCondition>(json, options),
            "composite" => JsonSerializer.Deserialize<CompositeCondition>(json, options),
            _ => null
        };
    }

    public override void Write(Utf8JsonWriter writer, PermissionCondition value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}