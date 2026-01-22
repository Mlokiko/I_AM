using System.Text.Json;
using I_AM.Models;

namespace I_AM.Services.Helpers;

/// <summary>
/// Helper class for extracting values from Firestore REST API JSON responses
/// </summary>
public static class FirestoreValueExtractor
{
    /// <summary>
    /// Extracts a string value from Firestore fields
    /// </summary>
    public static string GetStringValue(JsonElement fields, string key)
    {
        if (fields.TryGetProperty(key, out var prop) && prop.TryGetProperty("stringValue", out var value))
        {
            return value.GetString() ?? string.Empty;
        }
        return string.Empty;
    }

    /// <summary>
    /// Extracts an integer value from Firestore fields
    /// </summary>
    public static int GetIntValue(JsonElement fields, string key)
    {
        if (fields.TryGetProperty(key, out var prop) && prop.TryGetProperty("integerValue", out var value))
        {
            if (int.TryParse(value.GetString(), out var result))
            {
                return result;
            }
        }
        return 0;
    }

    /// <summary>
    /// Extracts a decimal value from Firestore fields
    /// </summary>
    public static decimal GetDecimalValue(JsonElement fields, string key)
    {
        if (fields.TryGetProperty(key, out var prop) && prop.TryGetProperty("doubleValue", out var value))
        {
            if (decimal.TryParse(value.GetString(), out var result))
            {
                return result;
            }
        }
        return 0m;
    }

    /// <summary>
    /// Extracts a timestamp value from Firestore fields
    /// </summary>
    public static DateTime GetTimestampValue(JsonElement fields, string key)
    {
        if (fields.TryGetProperty(key, out var prop) && prop.TryGetProperty("timestampValue", out var value))
        {
            if (DateTime.TryParse(value.GetString(), out var result))
            {
                return result;
            }
        }
        return DateTime.MinValue;
    }

    /// <summary>
    /// Extracts a nullable timestamp value from Firestore fields
    /// </summary>
    public static DateTime? GetTimestampValueNullable(JsonElement fields, string key)
    {
        if (fields.TryGetProperty(key, out var prop) && prop.TryGetProperty("timestampValue", out var value))
        {
            if (DateTime.TryParse(value.GetString(), out var result))
            {
                return result;
            }
        }
        return null;
    }

    /// <summary>
    /// Extracts a boolean value from Firestore fields
    /// </summary>
    public static bool GetBoolValue(JsonElement fields, string key)
    {
        if (fields.TryGetProperty(key, out var prop) && prop.TryGetProperty("booleanValue", out var value))
        {
            return value.GetBoolean();
        }
        return false;
    }

    /// <summary>
    /// Extracts a string array from Firestore fields
    /// </summary>
    public static List<string> GetStringArray(JsonElement fields, string key)
    {
        var result = new List<string>();
        if (fields.TryGetProperty(key, out var prop) && prop.TryGetProperty("arrayValue", out var arrayValue))
        {
            if (arrayValue.TryGetProperty("values", out var values))
            {
                foreach (var item in values.EnumerateArray())
                {
                    if (item.TryGetProperty("stringValue", out var stringValue))
                    {
                        result.Add(stringValue.GetString() ?? string.Empty);
                    }
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Extracts question options from Firestore fields
    /// </summary>
    public static List<QuestionOption> GetQuestionOptions(JsonElement fields, string key)
    {
        var result = new List<QuestionOption>();
        if (fields.TryGetProperty(key, out var prop) && prop.TryGetProperty("arrayValue", out var arrayValue))
        {
            if (arrayValue.TryGetProperty("values", out var values))
            {
                foreach (var item in values.EnumerateArray())
                {
                    if (item.TryGetProperty("mapValue", out var mapValue) && mapValue.TryGetProperty("fields", out var optionFields))
                    {
                        var option = new QuestionOption
                        {
                            Text = GetStringValue(optionFields, "text"),
                            Points = GetDecimalValue(optionFields, "points"),
                            Order = GetIntValue(optionFields, "order")
                        };
                        result.Add(option);
                    }
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Extracts the document ID from a Firestore document
    /// </summary>
    public static string GetDocumentId(JsonElement document)
    {
        if (document.TryGetProperty("name", out var nameElement))
        {
            var name = nameElement.GetString() ?? string.Empty;
            var parts = name.Split('/');
            return parts.Length > 0 ? parts[^1] : string.Empty;
        }
        return string.Empty;
    }
}
