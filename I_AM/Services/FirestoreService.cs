using System.Text.Json;
using System.Text.Json.Serialization;

namespace I_AM.Services;

public interface IFirestoreService
{
    Task<bool> SaveUserProfileAsync(string userId, UserProfile profile, string idToken);
    Task<UserProfile?> GetUserProfileAsync(string userId, string idToken);
    Task<bool> DeleteUserProfileAsync(string userId, string idToken);
}

public class UserProfile
{
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("age")]
    public int Age { get; set; }

    [JsonPropertyName("sex")]
    public string Sex { get; set; } = string.Empty;

    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("isCaregiver")]
    public bool IsCaregiver { get; set; }

    [JsonPropertyName("caretakersID")]
    public List<string> CaretakersID { get; set; } = new();

    [JsonPropertyName("caregiversID")]
    public List<string> CaregiversID { get; set; } = new();
}

public class FirestoreService : IFirestoreService
{
    private readonly HttpClient _httpClient;
    private readonly string _projectId = FirebaseConfig.ProjectId;

    public FirestoreService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<bool> SaveUserProfileAsync(string userId, UserProfile profile, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(idToken))
            {
                return false;
            }

            // Ustaw datê utworzenia
            profile.CreatedAt = DateTime.UtcNow;

            // URL do Firestore REST API
            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/users/{userId}?key={FirebaseConfig.WebApiKey}";

            // Buduj payload rêcznie jako JSON, aby mieæ pe³n¹ kontrolê nad struktur¹
            var payloadJson = BuildProfilePayload(profile);

            var content = new StringContent(
                payloadJson,
                System.Text.Encoding.UTF8,
                "application/json"
            );

            // Dodaj authorization header
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);

            var response = await _httpClient.PatchAsync(url, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"B³¹d zapisywania profilu. Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Response: {responseBody}");
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"B³¹d zapisywania profilu: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<UserProfile?> GetUserProfileAsync(string userId, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(idToken))
            {
                return null;
            }

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/users/{userId}?key={FirebaseConfig.WebApiKey}";

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);

            var response = await _httpClient.GetAsync(url);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var jsonDocument = JsonDocument.Parse(responseBody);
            var root = jsonDocument.RootElement;

            if (!root.TryGetProperty("fields", out var fields))
            {
                return null;
            }

            var profile = new UserProfile
            {
                FirstName = GetStringValue(fields, "firstName"),
                LastName = GetStringValue(fields, "lastName"),
                Age = GetIntValue(fields, "age"),
                Sex = GetStringValue(fields, "sex"),
                PhoneNumber = GetStringValue(fields, "phoneNumber"),
                Email = GetStringValue(fields, "email"),
                CreatedAt = GetTimestampValue(fields, "createdAt"),
                IsCaregiver = GetBoolValue(fields, "isCaregiver"),
                CaretakersID = GetStringArray(fields, "caretakersID"),
                CaregiversID = GetStringArray(fields, "caregiversID")
            };

            return profile;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"B³¹d pobierania profilu: {ex.Message}");
            return null;
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<bool> DeleteUserProfileAsync(string userId, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(idToken))
            {
                System.Diagnostics.Debug.WriteLine("? DeleteUserProfile: userId lub idToken s¹ puste");
                return false;
            }

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/users/{userId}?key={FirebaseConfig.WebApiKey}";
            
            System.Diagnostics.Debug.WriteLine($"Wysy³anie DELETE request do: {url}");

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);

            var response = await _httpClient.DeleteAsync(url);
            var responseBody = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"Delete response status: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"Delete response body: {responseBody}");

            if (response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"? Profil u¿ytkownika {userId} zosta³ usuniêty z Firestore");
                return true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"? B³¹d usuwania profilu. Status: {response.StatusCode}");
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? B³¹d usuwania profilu: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    private static string GetStringValue(JsonElement fields, string key)
    {
        if (fields.TryGetProperty(key, out var prop) && prop.TryGetProperty("stringValue", out var value))
        {
            return value.GetString() ?? string.Empty;
        }
        return string.Empty;
    }

    private static int GetIntValue(JsonElement fields, string key)
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

    private static DateTime GetTimestampValue(JsonElement fields, string key)
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

    private static bool GetBoolValue(JsonElement fields, string key)
    {
        if (fields.TryGetProperty(key, out var prop) && prop.TryGetProperty("booleanValue", out var value))
        {
            return value.GetBoolean();
        }
        return false;
    }

    private static List<string> GetStringArray(JsonElement fields, string key)
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

    private static string BuildProfilePayload(UserProfile profile)
    {
        using (var stream = new System.IO.MemoryStream())
        using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("fields");
            writer.WriteStartObject();

            // firstName
            writer.WritePropertyName("firstName");
            writer.WriteStartObject();
            writer.WriteString("stringValue", profile.FirstName);
            writer.WriteEndObject();

            // lastName
            writer.WritePropertyName("lastName");
            writer.WriteStartObject();
            writer.WriteString("stringValue", profile.LastName);
            writer.WriteEndObject();

            // age
            writer.WritePropertyName("age");
            writer.WriteStartObject();
            writer.WriteString("integerValue", profile.Age.ToString());
            writer.WriteEndObject();

            // sex
            writer.WritePropertyName("sex");
            writer.WriteStartObject();
            writer.WriteString("stringValue", profile.Sex);
            writer.WriteEndObject();

            // phoneNumber
            writer.WritePropertyName("phoneNumber");
            writer.WriteStartObject();
            writer.WriteString("stringValue", profile.PhoneNumber);
            writer.WriteEndObject();

            // email
            writer.WritePropertyName("email");
            writer.WriteStartObject();
            writer.WriteString("stringValue", profile.Email);
            writer.WriteEndObject();

            // createdAt
            writer.WritePropertyName("createdAt");
            writer.WriteStartObject();
            writer.WriteString("timestampValue", profile.CreatedAt.ToString("o"));
            writer.WriteEndObject();

            // isCaregiver
            writer.WritePropertyName("isCaregiver");
            writer.WriteStartObject();
            writer.WriteBoolean("booleanValue", profile.IsCaregiver);
            writer.WriteEndObject();

            // caretakersID
            writer.WritePropertyName("caretakersID");
            writer.WriteStartObject();
            writer.WritePropertyName("arrayValue");
            writer.WriteStartObject();
            if (profile.CaretakersID.Count > 0)
            {
                writer.WritePropertyName("values");
                writer.WriteStartArray();
                foreach (var id in profile.CaretakersID)
                {
                    writer.WriteStartObject();
                    writer.WriteString("stringValue", id);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
            writer.WriteEndObject();

            // caregiversID
            writer.WritePropertyName("caregiversID");
            writer.WriteStartObject();
            writer.WritePropertyName("arrayValue");
            writer.WriteStartObject();
            if (profile.CaregiversID.Count > 0)
            {
                writer.WritePropertyName("values");
                writer.WriteStartArray();
                foreach (var id in profile.CaregiversID)
                {
                    writer.WriteStartObject();
                    writer.WriteString("stringValue", id);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
            writer.WriteEndObject();

            writer.WriteEndObject();
            writer.WriteEndObject();

            writer.Flush();
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
