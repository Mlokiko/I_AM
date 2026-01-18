using System.Text.Json;
using System.Text.Json.Serialization;

namespace I_AM.Services;

public interface IFirestoreService
{
    Task<bool> SaveUserProfileAsync(string userId, UserProfile profile, string idToken);
    Task<UserProfile?> GetUserProfileAsync(string userId, string idToken);
    Task<bool> DeleteUserProfileAsync(string userId, string idToken);
    Task<bool> SaveUserPublicProfileAsync(string userId, UserPublicProfile profile, string idToken);
    Task<(UserPublicProfile? profile, string? userId)> GetUserPublicProfileByEmailAsync(string email, string idToken);
    Task<bool> SaveCaregiverInvitationAsync(string invitationId, CaregiverInvitation invitation, string idToken);
    Task<List<CaregiverInvitation>> GetPendingInvitationsAsync(string userId, string idToken);
    Task<List<CaregiverInvitation>> GetSentPendingInvitationsAsync(string userId, string idToken);
    Task<List<CaregiverInvitation>> GetSentRejectedInvitationsAsync(string userId, string idToken);
    Task<List<CaregiverInvitation>> GetAllCaregiverInvitationsAsync(string userId, string idToken);
    Task<bool> AcceptCaregiverInvitationAsync(string userId, string invitationId, string caregiverId, string idToken);
    Task<bool> RejectCaregiverInvitationAsync(string userId, string invitationId, string idToken);
    Task<bool> DeleteCaregiverInvitationAsync(string invitationId, string idToken);
    Task<bool> RemoveCaregiverAsync(string userId, string caregiverId, string idToken);
    Task<List<CaregiverInfo>> GetCaregiversAsync(string userId, string idToken);
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

public class UserPublicProfile
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

public class CaregiverInvitation
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("fromUserId")]
    public string FromUserId { get; set; } = string.Empty;

    [JsonPropertyName("toUserId")]
    public string ToUserId { get; set; } = string.Empty;

    [JsonPropertyName("toUserEmail")]
    public string ToUserEmail { get; set; } = string.Empty;

    [JsonPropertyName("fromUserName")]
    public string FromUserName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "pending"; // pending, accepted, rejected

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("respondedAt")]
    public DateTime? RespondedAt { get; set; }
}

public class CaregiverInfo
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "accepted"; // accepted, pending

    [JsonPropertyName("addedAt")]
    public DateTime AddedAt { get; set; }
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

    public async Task<(UserProfile? profile, string? userId)> GetUserProfileByEmailAsync(string email, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(idToken))
            {
                System.Diagnostics.Debug.WriteLine($"GetUserProfileByEmailAsync: email lub idToken s¹ puste");
                return (null, null);
            }

            System.Diagnostics.Debug.WriteLine($"GetUserProfileByEmailAsync: Szukam u¿ytkownika z emailem: {email}");

            // Spróbuj najpierw z kolekcji email_to_user_id (jeœli istnieje)
            var emailMapUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/email_to_user_id/{email}?key={FirebaseConfig.WebApiKey}";

            System.Diagnostics.Debug.WriteLine($"GetUserProfileByEmailAsync: Próbujê znaleŸæ userId w email_to_user_id");

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);

            var emailMapResponse = await _httpClient.GetAsync(emailMapUrl);
            
            if (emailMapResponse.IsSuccessStatusCode)
            {
                var emailMapBody = await emailMapResponse.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"GetUserProfileByEmailAsync: Znaleziono w email_to_user_id");

                var emailMapDoc = JsonDocument.Parse(emailMapBody);
                if (emailMapDoc.RootElement.TryGetProperty("fields", out var mapFields) &&
                    mapFields.TryGetProperty("userId", out var userIdProp) &&
                    userIdProp.TryGetProperty("stringValue", out var userIdValue))
                {
                    var userId = userIdValue.GetString();
                    System.Diagnostics.Debug.WriteLine($"GetUserProfileByEmailAsync: Uzyskano userId z email_to_user_id: {userId}");

                    if (!string.IsNullOrEmpty(userId))
                    {
                        // Teraz pobierz profil u¿ytkownika
                        var profile = await GetUserProfileAsync(userId, idToken);
                        if (profile != null)
                        {
                            return (profile, userId);
                        }
                    }
                }
            }

            // Jeœli nie znaleziono w email_to_user_id, przeszukaj wszystkich u¿ytkowników
            System.Diagnostics.Debug.WriteLine($"GetUserProfileByEmailAsync: Przeszukujê wszystkich u¿ytkowników");

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/users?key={FirebaseConfig.WebApiKey}";

            System.Diagnostics.Debug.WriteLine($"GetUserProfileByEmailAsync: URL: {url}");

            var response = await _httpClient.GetAsync(url);
            var responseBody = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"GetUserProfileByEmailAsync: Response Status: {response.StatusCode}");
            
            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"GetUserProfileByEmailAsync: ¯¹danie siê nie powiod³o. Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"GetUserProfileByEmailAsync: Response Body: {responseBody}");
                return (null, null);
            }

            var jsonDocument = JsonDocument.Parse(responseBody);
            var root = jsonDocument.RootElement;

            if (!root.TryGetProperty("documents", out var documents))
            {
                System.Diagnostics.Debug.WriteLine($"GetUserProfileByEmailAsync: Brak 'documents' w odpowiedzi");
                return (null, null);
            }

            var docsCount = 0;
            try { docsCount = documents.GetArrayLength(); } catch { }
            System.Diagnostics.Debug.WriteLine($"GetUserProfileByEmailAsync: Liczba dokumentów: {docsCount}");

            foreach (var doc in documents.EnumerateArray())
            {
                if (!doc.TryGetProperty("fields", out var fields))
                {
                    continue;
                }

                var docEmail = GetStringValue(fields, "email");
                
                if (!string.IsNullOrEmpty(docEmail))
                {
                    System.Diagnostics.Debug.WriteLine($"GetUserProfileByEmailAsync: Sprawdzam email: {docEmail} vs {email}");
                }

                if (docEmail.Equals(email, StringComparison.OrdinalIgnoreCase))
                {
                    var userId = GetDocumentId(doc);
                    System.Diagnostics.Debug.WriteLine($"GetUserProfileByEmailAsync: Znaleziono u¿ytkownika! ID: {userId}, Email: {docEmail}");

                    var profile = new UserProfile
                    {
                        FirstName = GetStringValue(fields, "firstName"),
                        LastName = GetStringValue(fields, "lastName"),
                        Age = GetIntValue(fields, "age"),
                        Sex = GetStringValue(fields, "sex"),
                        PhoneNumber = GetStringValue(fields, "phoneNumber"),
                        Email = docEmail,
                        CreatedAt = GetTimestampValue(fields, "createdAt"),
                        IsCaregiver = GetBoolValue(fields, "isCaregiver"),
                        CaretakersID = GetStringArray(fields, "caretakersID"),
                        CaregiversID = GetStringArray(fields, "caregiversID")
                    };
                    return (profile, userId);
                }
            }

            System.Diagnostics.Debug.WriteLine($"GetUserProfileByEmailAsync: Nie znaleziono u¿ytkownika z emailem: {email}");
            return (null, null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetUserProfileByEmailAsync - B³¹d: {ex.Message}\n{ex.StackTrace}");
            return (null, null);
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<bool> SaveCaregiverInvitationAsync(string invitationId, CaregiverInvitation invitation, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(invitationId) || string.IsNullOrWhiteSpace(idToken))
            {
                return false;
            }

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/caregiver_invitations/{invitationId}?key={FirebaseConfig.WebApiKey}";

            var payloadJson = BuildInvitationPayload(invitation);

            var content = new StringContent(
                payloadJson,
                System.Text.Encoding.UTF8,
                "application/json"
            );

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);

            var response = await _httpClient.PatchAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"B³¹d zapisywania zaproszenia. Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Response: {responseBody}");
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"B³¹d zapisywania zaproszenia: {ex.Message}");
            return false;
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<List<CaregiverInvitation>> GetPendingInvitationsAsync(string userId, string idToken)
    {
        var invitations = new List<CaregiverInvitation>();

        try
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(idToken))
            {
                return invitations;
            }

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/caregiver_invitations?key={FirebaseConfig.WebApiKey}";

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);

            var response = await _httpClient.GetAsync(url);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return invitations;
            }

            var jsonDocument = JsonDocument.Parse(responseBody);
            var root = jsonDocument.RootElement;

            if (!root.TryGetProperty("documents", out var documents))
            {
                return invitations;
            }

            foreach (var doc in documents.EnumerateArray())
            {
                if (!doc.TryGetProperty("fields", out var fields))
                    continue;

                var toUserId = GetStringValue(fields, "toUserId");
                var status = GetStringValue(fields, "status");

                if (toUserId == userId && status == "pending")
                {
                    var invitation = new CaregiverInvitation
                    {
                        Id = GetDocumentId(doc),
                        FromUserId = GetStringValue(fields, "fromUserId"),
                        ToUserId = toUserId,
                        ToUserEmail = GetStringValue(fields, "toUserEmail"),
                        FromUserName = GetStringValue(fields, "fromUserName"),
                        Status = status,
                        CreatedAt = GetTimestampValue(fields, "createdAt"),
                        RespondedAt = GetTimestampValueNullable(fields, "respondedAt")
                    };
                    invitations.Add(invitation);
                }
            }

            return invitations;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"B³¹d pobierania zaproszeñ: {ex.Message}");
            return invitations;
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<List<CaregiverInvitation>> GetSentPendingInvitationsAsync(string userId, string idToken)
    {
        var invitations = new List<CaregiverInvitation>();

        try
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(idToken))
            {
                return invitations;
            }

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/caregiver_invitations?key={FirebaseConfig.WebApiKey}";

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);

            var response = await _httpClient.GetAsync(url);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return invitations;
            }

            var jsonDocument = JsonDocument.Parse(responseBody);
            var root = jsonDocument.RootElement;

            if (!root.TryGetProperty("documents", out var documents))
            {
                return invitations;
            }

            foreach (var doc in documents.EnumerateArray())
            {
                if (!doc.TryGetProperty("fields", out var fields))
                    continue;

                var fromUserId = GetStringValue(fields, "fromUserId");
                var status = GetStringValue(fields, "status");

                if (fromUserId == userId && status == "pending")
                {
                    var invitation = new CaregiverInvitation
                    {
                        Id = GetDocumentId(doc),
                        FromUserId = fromUserId,
                        ToUserId = GetStringValue(fields, "toUserId"),
                        ToUserEmail = GetStringValue(fields, "toUserEmail"),
                        FromUserName = GetStringValue(fields, "fromUserName"),
                        Status = status,
                        CreatedAt = GetTimestampValue(fields, "createdAt"),
                        RespondedAt = GetTimestampValueNullable(fields, "respondedAt")
                    };
                    invitations.Add(invitation);
                }
            }

            return invitations;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"B³¹d pobierania wys³anych zaproszeñ: {ex.Message}");
            return invitations;
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<List<CaregiverInvitation>> GetSentRejectedInvitationsAsync(string userId, string idToken)
    {
        var invitations = new List<CaregiverInvitation>();

        try
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(idToken))
            {
                return invitations;
            }

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/caregiver_invitations?key={FirebaseConfig.WebApiKey}";

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);

            var response = await _httpClient.GetAsync(url);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return invitations;
            }

            var jsonDocument = JsonDocument.Parse(responseBody);
            var root = jsonDocument.RootElement;

            if (!root.TryGetProperty("documents", out var documents))
            {
                return invitations;
            }

            foreach (var doc in documents.EnumerateArray())
            {
                if (!doc.TryGetProperty("fields", out var fields))
                    continue;

                var fromUserId = GetStringValue(fields, "fromUserId");
                var status = GetStringValue(fields, "status");

                if (fromUserId == userId && status == "rejected")
                {
                    var invitation = new CaregiverInvitation
                    {
                        Id = GetDocumentId(doc),
                        FromUserId = fromUserId,
                        ToUserId = GetStringValue(fields, "toUserId"),
                        ToUserEmail = GetStringValue(fields, "toUserEmail"),
                        FromUserName = GetStringValue(fields, "fromUserName"),
                        Status = status,
                        CreatedAt = GetTimestampValue(fields, "createdAt"),
                        RespondedAt = GetTimestampValueNullable(fields, "respondedAt")
                    };
                    invitations.Add(invitation);
                }
            }

            return invitations;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"B³¹d pobierania odrzuconych zaproszeñ: {ex.Message}");
            return invitations;
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<List<CaregiverInvitation>> GetAllCaregiverInvitationsAsync(string userId, string idToken)
    {
        var invitations = new List<CaregiverInvitation>();

        try
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(idToken))
            {
                return invitations;
            }

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/caregiver_invitations?key={FirebaseConfig.WebApiKey}";

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);

            var response = await _httpClient.GetAsync(url);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return invitations;
            }

            var jsonDocument = JsonDocument.Parse(responseBody);
            var root = jsonDocument.RootElement;

            if (!root.TryGetProperty("documents", out var documents))
            {
                return invitations;
            }

            foreach (var doc in documents.EnumerateArray())
            {
                if (!doc.TryGetProperty("fields", out var fields))
                    continue;

                var fromUserId = GetStringValue(fields, "fromUserId");

                // Get all invitations from this user (sent or received)
                if (fromUserId == userId)
                {
                    var invitation = new CaregiverInvitation
                    {
                        Id = GetDocumentId(doc),
                        FromUserId = fromUserId,
                        ToUserId = GetStringValue(fields, "toUserId"),
                        ToUserEmail = GetStringValue(fields, "toUserEmail"),
                        FromUserName = GetStringValue(fields, "fromUserName"),
                        Status = GetStringValue(fields, "status"),
                        CreatedAt = GetTimestampValue(fields, "createdAt"),
                        RespondedAt = GetTimestampValueNullable(fields, "respondedAt")
                    };
                    invitations.Add(invitation);
                }
            }

            return invitations;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"B³¹d pobierania wszystkich zaproszeñ: {ex.Message}");
            return invitations;
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<bool> DeleteCaregiverInvitationAsync(string invitationId, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(invitationId) || string.IsNullOrWhiteSpace(idToken))
            {
                return false;
            }

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/caregiver_invitations/{invitationId}?key={FirebaseConfig.WebApiKey}";

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);

            var response = await _httpClient.DeleteAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"B³¹d usuwania zaproszenia. Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Response: {responseBody}");
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"B³¹d usuwania zaproszenia: {ex.Message}");
            return false;
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<bool> AcceptCaregiverInvitationAsync(string userId, string invitationId, string caregiverId, string idToken)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[AcceptInvitation] START - userId: {userId}, caregiverId: {caregiverId}");

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(caregiverId) || string.IsNullOrWhiteSpace(idToken))
            {
                return false;
            }

            var caretakerId = caregiverId;  
            var actualCaregiverId = userId; 

            // Step 1: Update ONLY the current user's (caregiver's) profile with caretaker info
            System.Diagnostics.Debug.WriteLine($"[AcceptInvitation] Updating current user's caretakersID");
            var caregiverProfile = await GetUserProfileAsync(actualCaregiverId, idToken);
            
            if (caregiverProfile == null)
            {
                caregiverProfile = new UserProfile 
                { 
                    CaretakersID = new List<string> { caretakerId },
                    CreatedAt = DateTime.UtcNow
                };
            }
            else if (!caregiverProfile.CaretakersID.Contains(caretakerId))
            {
                caregiverProfile.CaretakersID.Add(caretakerId);
            }

            var caregiverUpdateSuccess = await SaveUserProfileAsync(actualCaregiverId, caregiverProfile, idToken);
            System.Diagnostics.Debug.WriteLine($"[AcceptInvitation] Caregiver profile update: {caregiverUpdateSuccess}");

            if (!caregiverUpdateSuccess)
            {
                return false;
            }

            // Step 2: Update invitation status to accepted
            System.Diagnostics.Debug.WriteLine($"[AcceptInvitation] Attempting to update invitation status");
            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/caregiver_invitations/{invitationId}?key={FirebaseConfig.WebApiKey}";

            var invitation = new CaregiverInvitation
            {
                Id = invitationId,
                FromUserId = caretakerId,
                ToUserId = actualCaregiverId,
                Status = "accepted",
                RespondedAt = DateTime.UtcNow
            };

            var payloadJson = BuildInvitationPayload(invitation);
            var content = new StringContent(payloadJson, System.Text.Encoding.UTF8, "application/json");

            // SET AUTHORIZATION HEADER BEFORE THE REQUEST
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);
            var response = await _httpClient.PatchAsync(url, content);
            _httpClient.DefaultRequestHeaders.Authorization = null;
            
            System.Diagnostics.Debug.WriteLine($"[AcceptInvitation] Invitation update: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[AcceptInvitation] Warning - could not update invitation: {responseBody}");
            }

            // Step 3: Update caretaker's caregiversID using targeted update (respects Security Rules)
            System.Diagnostics.Debug.WriteLine($"[AcceptInvitation] Attempting to update caretaker's caregiversID");
            
            var caretakerProfile = await GetUserProfileAsync(caretakerId, idToken);
            if (caretakerProfile == null)
            {
                caretakerProfile = new UserProfile
                {
                    CaregiversID = new List<string> { actualCaregiverId },
                    CreatedAt = DateTime.UtcNow
                };
            }
            else if (!caretakerProfile.CaregiversID.Contains(actualCaregiverId))
            {
                caretakerProfile.CaregiversID.Add(actualCaregiverId);
            }

            // Use targeted update with updateMask to only update caregiversID field
            var caretakerUpdateUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/users/{caretakerId}?key={FirebaseConfig.WebApiKey}&updateMask.fieldPaths=caregiversID";
            
            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("fields");
                writer.WriteStartObject();
                
                writer.WritePropertyName("caregiversID");
                writer.WriteStartObject();
                writer.WritePropertyName("arrayValue");
                writer.WriteStartObject();
                
                if (caretakerProfile.CaregiversID.Count > 0)
                {
                    writer.WritePropertyName("values");
                    writer.WriteStartArray();
                    foreach (var id in caretakerProfile.CaregiversID)
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
                var caretakerPayload = System.Text.Encoding.UTF8.GetString(stream.ToArray());
                
                System.Diagnostics.Debug.WriteLine($"[AcceptInvitation] Caretaker update payload: {caretakerPayload}");
                
                var caretakerContent = new StringContent(caretakerPayload, System.Text.Encoding.UTF8, "application/json");
                
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);
                var caretakerResponse = await _httpClient.PatchAsync(caretakerUpdateUrl, caretakerContent);
                _httpClient.DefaultRequestHeaders.Authorization = null;
                
                System.Diagnostics.Debug.WriteLine($"[AcceptInvitation] Caretaker update response: {caretakerResponse.StatusCode}");
                
                if (!caretakerResponse.IsSuccessStatusCode)
                {
                    var caretakerResponseBody = await caretakerResponse.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[AcceptInvitation] Caretaker update error: {caretakerResponseBody}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"[AcceptInvitation] SUCCESS - Relationship established");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AcceptInvitation] Exception: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> UpdateProfileFieldAsync(string userId, string fieldName, List<string> values, string idToken)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateProfileFieldAsync] START - userId: {userId}, fieldName: {fieldName}, values count: {values.Count}");

            // First, try to ensure the document exists by creating a minimal profile if needed
            System.Diagnostics.Debug.WriteLine($"[UpdateProfileFieldAsync] Ensuring document exists");
            await EnsureProfileExistsAsync(userId, idToken);

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/users/{userId}?key={FirebaseConfig.WebApiKey}&updateMask.fieldPaths={fieldName}";

            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("fields");
                writer.WriteStartObject();

                writer.WritePropertyName(fieldName);
                writer.WriteStartObject();
                writer.WritePropertyName("arrayValue");
                writer.WriteStartObject();

                if (values.Count > 0)
                {
                    writer.WritePropertyName("values");
                    writer.WriteStartArray();
                    foreach (var value in values)
                    {
                        writer.WriteStartObject();
                        writer.WriteString("stringValue", value);
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
                writer.WriteEndObject();

                writer.WriteEndObject();
                writer.WriteEndObject();

                writer.Flush();
                var payloadJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());

                System.Diagnostics.Debug.WriteLine($"[UpdateProfileFieldAsync] Sending update: {payloadJson}");

                var content = new StringContent(payloadJson, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PatchAsync(url, content);

                System.Diagnostics.Debug.WriteLine($"[UpdateProfileFieldAsync] Response status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[UpdateProfileFieldAsync] Error: {responseBody}");
                }

                return response.IsSuccessStatusCode;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateProfileFieldAsync] Exception: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[UpdateProfileFieldAsync] StackTrace: {ex.StackTrace}");
            return false;
        }
    }

    private async Task EnsureProfileExistsAsync(string userId, string idToken)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[EnsureProfileExistsAsync] Checking if profile exists for userId: {userId}");

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/users/{userId}?key={FirebaseConfig.WebApiKey}";

            var getResponse = await _httpClient.GetAsync(url);

            if (getResponse.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"[EnsureProfileExistsAsync] Profile already exists");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[EnsureProfileExistsAsync] Profile doesn't exist, creating minimal profile");

            // Create minimal profile
            var minimalProfile = new UserProfile
            {
                Email = string.Empty,
                FirstName = string.Empty,
                LastName = string.Empty,
                Sex = string.Empty,
                PhoneNumber = string.Empty,
                Age = 0,
                IsCaregiver = false,
                CreatedAt = DateTime.UtcNow,
                CaretakersID = new List<string>(),
                CaregiversID = new List<string>()
            };

            var payloadJson = BuildProfilePayload(minimalProfile);
            var content = new StringContent(payloadJson, System.Text.Encoding.UTF8, "application/json");

            var createResponse = await _httpClient.PatchAsync(url, content);
            System.Diagnostics.Debug.WriteLine($"[EnsureProfileExistsAsync] Profile creation response: {createResponse.StatusCode}");

            if (!createResponse.IsSuccessStatusCode)
            {
                var responseBody = await createResponse.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[EnsureProfileExistsAsync] Error creating profile: {responseBody}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[EnsureProfileExistsAsync] Exception: {ex.Message}");
        }
    }

    public async Task<bool> RejectCaregiverInvitationAsync(string userId, string invitationId, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(invitationId) || string.IsNullOrWhiteSpace(idToken))
            {
                return false;
            }

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/caregiver_invitations/{invitationId}?key={FirebaseConfig.WebApiKey}";

            var invitation = new CaregiverInvitation
            {
                Id = invitationId,
                Status = "rejected",
                RespondedAt = DateTime.UtcNow
            };

            var payloadJson = BuildInvitationPayload(invitation);
            var content = new StringContent(payloadJson, System.Text.Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);

            var response = await _httpClient.PatchAsync(url, content);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"B³¹d odrzucania zaproszenia: {ex.Message}");
            return false;
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<bool> RemoveCaregiverAsync(string userId, string caregiverId, string idToken)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[RemoveCaregiver] START - userId: {userId}, caregiverId: {caregiverId}");

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(caregiverId))
            {
                return false;
            }

            // 1. Pobierz profil u¿ytkownika
            var userProfile = await GetUserProfileAsync(userId, idToken);
            if (userProfile == null)
                return false;

            // 2. Usuñ caregiverId z caregiversID
            userProfile.CaregiversID.Remove(caregiverId);

            // 3. Pobierz profil opiekuna
            var caregiverProfile = await GetUserProfileAsync(caregiverId, idToken);
            if (caregiverProfile == null)
                return false;

            // 4. Usuñ userId z caretakersID
            caregiverProfile.CaretakersID.Remove(userId);

            // 5. Aktualizuj profil u¿ytkownika (zwyk³a aktualizacja)
            System.Diagnostics.Debug.WriteLine($"[RemoveCaregiver] Updating user profile");
            var userSaved = await SaveUserProfileAsync(userId, userProfile, idToken);
            if (!userSaved)
            {
                System.Diagnostics.Debug.WriteLine($"[RemoveCaregiver] Failed to update user profile");
                return false;
            }

            // 6. Aktualizuj profil caregiver'a u¿ywaj¹c targeted update (tylko caretakersID)
            System.Diagnostics.Debug.WriteLine($"[RemoveCaregiver] Updating caregiver profile with targeted update");
            var caregiverUpdateUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/users/{caregiverId}?key={FirebaseConfig.WebApiKey}&updateMask.fieldPaths=caretakersID";
            
            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("fields");
                writer.WriteStartObject();
                
                writer.WritePropertyName("caretakersID");
                writer.WriteStartObject();
                writer.WritePropertyName("arrayValue");
                writer.WriteStartObject();
                
                if (caregiverProfile.CaretakersID.Count > 0)
                {
                    writer.WritePropertyName("values");
                    writer.WriteStartArray();
                    foreach (var id in caregiverProfile.CaretakersID)
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
                var caregiverPayload = System.Text.Encoding.UTF8.GetString(stream.ToArray());
                
                System.Diagnostics.Debug.WriteLine($"[RemoveCaregiver] Caregiver update payload: {caregiverPayload}");
                
                var caregiverContent = new StringContent(caregiverPayload, System.Text.Encoding.UTF8, "application/json");
                
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);
                var caregiverResponse = await _httpClient.PatchAsync(caregiverUpdateUrl, caregiverContent);
                _httpClient.DefaultRequestHeaders.Authorization = null;
                
                System.Diagnostics.Debug.WriteLine($"[RemoveCaregiver] Caregiver update response: {caregiverResponse.StatusCode}");
                
                if (!caregiverResponse.IsSuccessStatusCode)
                {
                    var caregiverResponseBody = await caregiverResponse.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[RemoveCaregiver] Caregiver update error: {caregiverResponseBody}");
                    return false;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[RemoveCaregiver] SUCCESS - Relationship removed");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RemoveCaregiver] Exception: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    public async Task<List<CaregiverInfo>> GetCaregiversAsync(string userId, string idToken)
    {
        var caregivers = new List<CaregiverInfo>();

        try
        {
            System.Diagnostics.Debug.WriteLine($"[GetCaregiversAsync] START - userId: {userId}");

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(idToken))
            {
                System.Diagnostics.Debug.WriteLine($"[GetCaregiversAsync] Missing userId or idToken");
                return caregivers;
            }

            // 1. Pobierz profil u¿ytkownika aby uzyskaæ listê caregiverIDs
            System.Diagnostics.Debug.WriteLine($"[GetCaregiversAsync] Fetching user profile");
            var userProfile = await GetUserProfileAsync(userId, idToken);
            
            if (userProfile == null)
            {
                System.Diagnostics.Debug.WriteLine($"[GetCaregiversAsync] User profile is null");
                return caregivers;
            }

            System.Diagnostics.Debug.WriteLine($"[GetCaregiversAsync] User has {userProfile.CaregiversID.Count} caregivers");

            if (userProfile.CaregiversID.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[GetCaregiversAsync] No caregivers found");
                return caregivers;
            }

            // 2. Dla ka¿dego caregiverId pobierz jego profil
            foreach (var caregiverId in userProfile.CaregiversID)
            {
                System.Diagnostics.Debug.WriteLine($"[GetCaregiversAsync] Fetching caregiver profile for {caregiverId}");
                var profile = await GetUserProfileAsync(caregiverId, idToken);
                if (profile != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[GetCaregiversAsync] Adding caregiver: {profile.FirstName} {profile.LastName}");
                    caregivers.Add(new CaregiverInfo
                    {
                        UserId = caregiverId,
                        Email = profile.Email,
                        FirstName = profile.FirstName,
                        LastName = profile.LastName,
                        Status = "accepted",
                        AddedAt = profile.CreatedAt
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[GetCaregiversAsync] Could not fetch caregiver profile for {caregiverId}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"[GetCaregiversAsync] SUCCESS - returning {caregivers.Count} caregivers");
            return caregivers;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GetCaregiversAsync] Exception: {ex.Message}\n{ex.StackTrace}");
            return caregivers;
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<bool> SaveUserPublicProfileAsync(string userId, UserPublicProfile profile, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(idToken))
            {
                return false;
            }

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/user_public_profiles/{userId}?key={FirebaseConfig.WebApiKey}";

            var payloadJson = BuildPublicProfilePayload(profile);

            var content = new StringContent(
                payloadJson,
                System.Text.Encoding.UTF8,
                "application/json"
            );

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);

            var response = await _httpClient.PatchAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"B³¹d zapisywania publicznego profilu. Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Response: {responseBody}");
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"B³¹d zapisywania publicznego profilu: {ex.Message}");
            return false;
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<(UserPublicProfile? profile, string? userId)> GetUserPublicProfileByEmailAsync(string email, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(idToken))
            {
                System.Diagnostics.Debug.WriteLine($"GetUserPublicProfileByEmailAsync: email lub idToken s¹ puste");
                return (null, null);
            }

            System.Diagnostics.Debug.WriteLine($"GetUserPublicProfileByEmailAsync: Szukam u¿ytkownika z emailem: {email}");

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/user_public_profiles?key={FirebaseConfig.WebApiKey}";

            System.Diagnostics.Debug.WriteLine($"GetUserPublicProfileByEmailAsync: URL: {url}");

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);

            var response = await _httpClient.GetAsync(url);
            var responseBody = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"GetUserPublicProfileByEmailAsync: Response Status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"GetUserPublicProfileByEmailAsync: ¯¹danie siê nie powiod³o. Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"GetUserPublicProfileByEmailAsync: Response Body: {responseBody}");
                return (null, null);
            }

            var jsonDocument = JsonDocument.Parse(responseBody);
            var root = jsonDocument.RootElement;

            if (!root.TryGetProperty("documents", out var documents))
            {
                System.Diagnostics.Debug.WriteLine($"GetUserPublicProfileByEmailAsync: Brak 'documents' w odpowiedzi");
                return (null, null);
            }

            var docsCount = 0;
            try { docsCount = documents.GetArrayLength(); } catch { }
            System.Diagnostics.Debug.WriteLine($"GetUserPublicProfileByEmailAsync: Liczba dokumentów: {docsCount}");

            foreach (var doc in documents.EnumerateArray())
            {
                if (!doc.TryGetProperty("fields", out var fields))
                {
                    continue;
                }

                var docEmail = GetStringValue(fields, "email");

                if (!string.IsNullOrEmpty(docEmail))
                {
                    System.Diagnostics.Debug.WriteLine($"GetUserPublicProfileByEmailAsync: Sprawdzam email: {docEmail} vs {email}");
                }

                if (docEmail.Equals(email, StringComparison.OrdinalIgnoreCase))
                {
                    var userId = GetDocumentId(doc);
                    System.Diagnostics.Debug.WriteLine($"GetUserPublicProfileByEmailAsync: Znaleziono u¿ytkownika! ID: {userId}, Email: {docEmail}");

                    var profile = new UserPublicProfile
                    {
                        UserId = userId,
                        Email = docEmail,
                        FirstName = GetStringValue(fields, "firstName"),
                        LastName = GetStringValue(fields, "lastName"),
                        CreatedAt = GetTimestampValue(fields, "createdAt")
                    };
                    return (profile, userId);
                }
            }

            System.Diagnostics.Debug.WriteLine($"GetUserPublicProfileByEmailAsync: Nie znaleziono u¿ytkownika z emailem: {email}");
            return (null, null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetUserPublicProfileByEmailAsync - B³¹d: {ex.Message}\n{ex.StackTrace}");
            return (null, null);
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

    private static DateTime? GetTimestampValueNullable(JsonElement fields, string key)
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

    private static string GetDocumentId(JsonElement document)
    {
        if (document.TryGetProperty("name", out var nameElement))
        {
            var name = nameElement.GetString() ?? string.Empty;
            var parts = name.Split('/');
            return parts.Length > 0 ? parts[^1] : string.Empty;
        }
        return string.Empty;
    }

    private static string BuildInvitationPayload(CaregiverInvitation invitation)
    {
        using (var stream = new System.IO.MemoryStream())
        using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("fields");
            writer.WriteStartObject();

            if (!string.IsNullOrEmpty(invitation.Id))
            {
                writer.WritePropertyName("id");
                writer.WriteStartObject();
                writer.WriteString("stringValue", invitation.Id);
                writer.WriteEndObject();
            }

            if (!string.IsNullOrEmpty(invitation.FromUserId))
            {
                writer.WritePropertyName("fromUserId");
                writer.WriteStartObject();
                writer.WriteString("stringValue", invitation.FromUserId);
                writer.WriteEndObject();
            }

            if (!string.IsNullOrEmpty(invitation.ToUserId))
            {
                writer.WritePropertyName("toUserId");
                writer.WriteStartObject();
                writer.WriteString("stringValue", invitation.ToUserId);
                writer.WriteEndObject();
            }

            if (!string.IsNullOrEmpty(invitation.ToUserEmail))
            {
                writer.WritePropertyName("toUserEmail");
                writer.WriteStartObject();
                writer.WriteString("stringValue", invitation.ToUserEmail);
                writer.WriteEndObject();
            }

            if (!string.IsNullOrEmpty(invitation.FromUserName))
            {
                writer.WritePropertyName("fromUserName");
                writer.WriteStartObject();
                writer.WriteString("stringValue", invitation.FromUserName);
                writer.WriteEndObject();
            }

            writer.WritePropertyName("status");
            writer.WriteStartObject();
            writer.WriteString("stringValue", invitation.Status);
            writer.WriteEndObject();

            writer.WritePropertyName("createdAt");
            writer.WriteStartObject();
            writer.WriteString("timestampValue", invitation.CreatedAt.ToString("o"));
            writer.WriteEndObject();

            if (invitation.RespondedAt.HasValue)
            {
                writer.WritePropertyName("respondedAt");
                writer.WriteStartObject();
                writer.WriteString("timestampValue", invitation.RespondedAt.Value.ToString("o"));
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
            writer.WriteEndObject();

            writer.Flush();
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    private static string BuildPublicProfilePayload(UserPublicProfile profile)
    {
        using (var stream = new System.IO.MemoryStream())
        using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("fields");
            writer.WriteStartObject();

            // userId
            writer.WritePropertyName("userId");
            writer.WriteStartObject();
            writer.WriteString("stringValue", profile.UserId);
            writer.WriteEndObject();

            // email
            writer.WritePropertyName("email");
            writer.WriteStartObject();
            writer.WriteString("stringValue", profile.Email);
            writer.WriteEndObject();

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

            // createdAt
            writer.WritePropertyName("createdAt");
            writer.WriteStartObject();
            writer.WriteString("timestampValue", profile.CreatedAt.ToString("o"));
            writer.WriteEndObject();

            writer.WriteEndObject();
            writer.WriteEndObject();

            writer.Flush();
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
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
