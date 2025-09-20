using System.Text.Json;
using System.Text.Json.Serialization;

namespace Enterprise.Shared.Validation.Extensions;

/// <summary>
/// JSON utility helper for Turkish localized applications
/// </summary>
public static class JsonHelper
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Serializes object to JSON string
    /// </summary>
    public static string Serialize<T>(T obj, JsonSerializerOptions? options = null)
    {
        try
        {
            return JsonSerializer.Serialize(obj, options ?? DefaultOptions);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"JSON serileştirme hatası: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deserializes JSON string to object
    /// </summary>
    public static T? Deserialize<T>(string json, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        
        try
        {
            return JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"JSON deserileştirme hatası: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Tries to deserialize JSON string to object
    /// </summary>
    public static bool TryDeserialize<T>(string json, out T? result, JsonSerializerOptions? options = null)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(json)) return false;

        try
        {
            result = JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);
            return result != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates if string is valid JSON
    /// </summary>
    public static bool IsValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;

        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Pretty prints JSON string
    /// </summary>
    public static string PrettyPrint(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;

        try
        {
            var jsonDocument = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
        catch
        {
            return json;
        }
    }

    /// <summary>
    /// Minifies JSON string
    /// </summary>
    public static string Minify(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;

        try
        {
            var jsonDocument = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions 
            { 
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
        catch
        {
            return json;
        }
    }

    /// <summary>
    /// Converts object to JSON and back to create a deep clone
    /// </summary>
    public static T? Clone<T>(T obj, JsonSerializerOptions? options = null)
    {
        if (obj == null) return default;
        
        var json = Serialize(obj, options);
        return Deserialize<T>(json, options);
    }

    /// <summary>
    /// Compares two objects by serializing them to JSON
    /// </summary>
    public static bool JsonEquals<T>(T obj1, T obj2, JsonSerializerOptions? options = null)
    {
        if (ReferenceEquals(obj1, obj2)) return true;
        if (obj1 == null || obj2 == null) return false;

        var json1 = Serialize(obj1, options);
        var json2 = Serialize(obj2, options);
        
        return string.Equals(json1, json2, StringComparison.Ordinal);
    }

    /// <summary>
    /// Merges two JSON objects (second object properties override first)
    /// </summary>
    public static string MergeJson(string json1, string json2)
    {
        if (string.IsNullOrWhiteSpace(json1)) return json2 ?? string.Empty;
        if (string.IsNullOrWhiteSpace(json2)) return json1;

        try
        {
            var dict1 = JsonSerializer.Deserialize<Dictionary<string, object>>(json1, DefaultOptions) ?? new();
            var dict2 = JsonSerializer.Deserialize<Dictionary<string, object>>(json2, DefaultOptions) ?? new();

            foreach (var kvp in dict2)
            {
                dict1[kvp.Key] = kvp.Value;
            }

            return JsonSerializer.Serialize(dict1, DefaultOptions);
        }
        catch
        {
            return json2; // Return second JSON if merge fails
        }
    }

    /// <summary>
    /// Gets JSON property value by path (dot notation)
    /// </summary>
    public static T? GetProperty<T>(string json, string propertyPath)
    {
        if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(propertyPath))
            return default;

        try
        {
            using var document = JsonDocument.Parse(json);
            var element = document.RootElement;

            var pathParts = propertyPath.Split('.');
            foreach (var part in pathParts)
            {
                if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(part, out var nextElement))
                {
                    element = nextElement;
                }
                else
                {
                    return default;
                }
            }

            return JsonSerializer.Deserialize<T>(element.GetRawText(), DefaultOptions);
        }
        catch
        {
            return default;
        }
    }
}