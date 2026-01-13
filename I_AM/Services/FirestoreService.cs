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

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
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

            var payload = new
            {
                fields = new
                {
                    firstName = new { stringValue = profile.FirstName },
                    lastName = new { stringValue = profile.LastName },
                    age = new { integerValue = profile.Age.ToString() },
                    sex = new { stringValue = profile.Sex },
                    createdAt = new { timestampValue = profile.CreatedAt.ToString("o") },
                    email = new { stringValue = profile.Email }
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            // Dodaj authorization header
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);

            var response = await _httpClient.PatchAsync(url, content);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"B³¹d zapisywania profilu: {ex.Message}");
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
                Email = GetStringValue(fields, "email"),
                CreatedAt = GetTimestampValue(fields, "createdAt")
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
}
