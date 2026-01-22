using System.Text.Json;
using I_AM.Models;
using I_AM.Services.Helpers;
using I_AM.Services.Interfaces;

namespace I_AM.Services;

/// <summary>
/// Refactored FirestoreService with improved maintainability and reduced duplication
/// </summary>
public class FirestoreService : IFirestoreService
{
    private readonly HttpClient _httpClient;
    private readonly string _projectId = FirebaseConfig.ProjectId;

    // Firestore Collection Names
    private const string COLLECTION_USERS = "users";
    private const string COLLECTION_PUBLIC_PROFILES = "user_public_profiles";
    private const string COLLECTION_INVITATIONS = "caregiver_invitations";
    private const string COLLECTION_EMAIL_MAPPING = "email_to_user_id";

    // Invitation Status Constants
    private const string STATUS_PENDING = "pending";
    private const string STATUS_ACCEPTED = "accepted";
    private const string STATUS_REJECTED = "rejected";

    // Firestore Field Names
    private const string FIELD_USER_ID = "userId";
    private const string FIELD_FROM_USER_ID = "fromUserId";
    private const string FIELD_TO_USER_ID = "toUserId";
    private const string FIELD_EMAIL = "email";
    private const string FIELD_STATUS = "status";
    private const string FIELD_CARETAKERS_ID = "caretakersID";
    private const string FIELD_CAREGIVERS_ID = "caregiversID";

    public FirestoreService()
    {
        _httpClient = new HttpClient();
    }

    #region User Profile Operations

    public async Task<bool> SaveUserProfileAsync(string userId, UserProfile profile, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(idToken))
            {
                return false;
            }

            profile.CreatedAt = DateTime.UtcNow;
            var url = BuildFirestoreUrl(COLLECTION_USERS, userId);
            var payloadJson = FirestorePayloadBuilder.BuildProfilePayload(profile);

            return await SendPatchRequestAsync(url, payloadJson, idToken, "profile");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving profile: {ex.Message}\n{ex.StackTrace}");
            return false;
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

            var url = BuildFirestoreUrl(COLLECTION_USERS, userId);
            var jsonDocument = await FetchFirestoreDocumentAsync(url, idToken);
            
            if (jsonDocument == null || !jsonDocument.RootElement.TryGetProperty("fields", out var fields))
            {
                return null;
            }

            return MapToUserProfile(fields);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching profile: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteUserProfileAsync(string userId, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(idToken))
            {
                return false;
            }

            var url = BuildFirestoreUrl(COLLECTION_USERS, userId);
            return await SendDeleteRequestAsync(url, idToken, "profile");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting profile: {ex.Message}\n{ex.StackTrace}");
            return false;
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

            var url = BuildFirestoreUrl(COLLECTION_USERS);
            var jsonDocument = await FetchFirestoreDocumentAsync(url, idToken);
            
            if (jsonDocument == null || !jsonDocument.RootElement.TryGetProperty("documents", out var documents))
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

                var docEmail = FirestoreValueExtractor.GetStringValue(fields, FIELD_EMAIL);
                
                if (!string.IsNullOrEmpty(docEmail))
                {
                    System.Diagnostics.Debug.WriteLine($"GetUserProfileByEmailAsync: Sprawdzam email: {docEmail} vs {email}");
                }

                if (docEmail.Equals(email, StringComparison.OrdinalIgnoreCase))
                {
                    var userId = FirestoreValueExtractor.GetDocumentId(doc);
                    System.Diagnostics.Debug.WriteLine($"GetUserProfileByEmailAsync: Znaleziono u¿ytkownika! ID: {userId}, Email: {docEmail}");

                    var profile = MapToUserProfile(fields);
                    profile.Email = docEmail;
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
    }

    #endregion

    #region Public Profile Operations

    public async Task<bool> SaveUserPublicProfileAsync(string userId, UserPublicProfile profile, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(idToken))
            {
                return false;
            }

            var url = BuildFirestoreUrl(COLLECTION_PUBLIC_PROFILES, userId);
            var payloadJson = FirestorePayloadBuilder.BuildPublicProfilePayload(profile);

            return await SendPatchRequestAsync(url, payloadJson, idToken, "public profile");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving public profile: {ex.Message}");
            return false;
        }
    }

    public async Task<(UserPublicProfile? profile, string? userId)> GetUserPublicProfileByEmailAsync(string email, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(idToken))
            {
                return (null, null);
            }

            var url = BuildFirestoreUrl(COLLECTION_PUBLIC_PROFILES);
            var jsonDocument = await FetchFirestoreDocumentAsync(url, idToken);
            
            if (jsonDocument == null || !jsonDocument.RootElement.TryGetProperty("documents", out var documents))
            {
                return (null, null);
            }

            foreach (var doc in documents.EnumerateArray())
            {
                if (!doc.TryGetProperty("fields", out var fields))
                    continue;

                var docEmail = FirestoreValueExtractor.GetStringValue(fields, FIELD_EMAIL);

                if (docEmail.Equals(email, StringComparison.OrdinalIgnoreCase))
                {
                    var userId = FirestoreValueExtractor.GetDocumentId(doc);
                    var profile = new UserPublicProfile
                    {
                        UserId = userId,
                        Email = docEmail,
                        FirstName = FirestoreValueExtractor.GetStringValue(fields, "firstName"),
                        LastName = FirestoreValueExtractor.GetStringValue(fields, "lastName"),
                        CreatedAt = FirestoreValueExtractor.GetTimestampValue(fields, "createdAt")
                    };
                    return (profile, userId);
                }
            }

            return (null, null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching public profile: {ex.Message}\n{ex.StackTrace}");
            return (null, null);
        }
    }

    #endregion

    #region Caregiver Invitation Operations

    public async Task<bool> SaveCaregiverInvitationAsync(string invitationId, CaregiverInvitation invitation, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(invitationId) || string.IsNullOrWhiteSpace(idToken))
            {
                return false;
            }

            var url = BuildFirestoreUrl(COLLECTION_INVITATIONS, invitationId);
            var payloadJson = FirestorePayloadBuilder.BuildInvitationPayload(invitation);

            return await SendPatchRequestAsync(url, payloadJson, idToken, "invitation");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving invitation: {ex.Message}");
            return false;
        }
    }

    public async Task<List<CaregiverInvitation>> GetPendingInvitationsAsync(string userId, string idToken)
    {
        return await GetInvitationsAsync(
            userId, 
            idToken, 
            (inv, uid) => inv.ToUserId == uid && inv.Status == STATUS_PENDING);
    }

    public async Task<List<CaregiverInvitation>> GetSentPendingInvitationsAsync(string userId, string idToken)
    {
        return await GetInvitationsAsync(
            userId, 
            idToken, 
            (inv, uid) => inv.FromUserId == uid && inv.Status == STATUS_PENDING);
    }

    public async Task<List<CaregiverInvitation>> GetSentRejectedInvitationsAsync(string userId, string idToken)
    {
        return await GetInvitationsAsync(
            userId, 
            idToken, 
            (inv, uid) => inv.FromUserId == uid && inv.Status == STATUS_REJECTED);
    }

    public async Task<List<CaregiverInvitation>> GetAllCaregiverInvitationsAsync(string userId, string idToken)
    {
        return await GetInvitationsAsync(
            userId, 
            idToken, 
            (inv, uid) => inv.FromUserId == uid);
    }

    /// <summary>
    /// Generic method to fetch invitations with custom filtering
    /// </summary>
    private async Task<List<CaregiverInvitation>> GetInvitationsAsync(
        string userId, 
        string idToken, 
        Func<CaregiverInvitation, string, bool> filterPredicate)
    {
        var invitations = new List<CaregiverInvitation>();

        try
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(idToken))
            {
                return invitations;
            }

            var url = BuildFirestoreUrl(COLLECTION_INVITATIONS);
            var jsonDocument = await FetchFirestoreDocumentAsync(url, idToken);
            
            if (jsonDocument == null || !jsonDocument.RootElement.TryGetProperty("documents", out var documents))
            {
                return invitations;
            }

            foreach (var doc in documents.EnumerateArray())
            {
                if (!doc.TryGetProperty("fields", out var fields))
                    continue;

                var invitation = MapToCaregiverInvitation(doc, fields);
                
                if (filterPredicate(invitation, userId))
                {
                    invitations.Add(invitation);
                }
            }

            return invitations;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching invitations: {ex.Message}");
            return invitations;
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

            var url = BuildFirestoreUrl(COLLECTION_INVITATIONS, invitationId);
            return await SendDeleteRequestAsync(url, idToken, "invitation");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting invitation: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> AcceptCaregiverInvitationAsync(string userId, string invitationId, string caregiverId, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(caregiverId) || string.IsNullOrWhiteSpace(idToken))
            {
                return false;
            }

            var caretakerId = caregiverId;
            var actualCaregiverId = userId;

            // Update caregiver's profile with caretaker
            var caregiverProfile = await GetUserProfileAsync(actualCaregiverId, idToken) 
                ?? new UserProfile { CaretakersID = new List<string>(), CreatedAt = DateTime.UtcNow };
            
            if (!caregiverProfile.CaretakersID.Contains(caretakerId))
            {
                caregiverProfile.CaretakersID.Add(caretakerId);
            }

            if (!await SaveUserProfileAsync(actualCaregiverId, caregiverProfile, idToken))
            {
                return false;
            }

            // Update invitation status
            var invitationUrl = BuildFirestoreUrl(COLLECTION_INVITATIONS, invitationId);
            var invitationDoc = await FetchFirestoreDocumentAsync(invitationUrl, idToken);
            
            if (invitationDoc == null || !invitationDoc.RootElement.TryGetProperty("fields", out var fields))
            {
                return false;
            }

            var invitation = MapToCaregiverInvitation(invitationDoc.RootElement, fields);
            invitation.Status = STATUS_ACCEPTED;
            invitation.RespondedAt = DateTime.UtcNow;

            var payloadJson = FirestorePayloadBuilder.BuildInvitationPayload(invitation);
            await SendPatchRequestAsync(invitationUrl, payloadJson, idToken, "invitation");

            // Update caretaker's profile with caregiver
            var caretakerProfile = await GetUserProfileAsync(caretakerId, idToken)
                ?? new UserProfile { CaregiversID = new List<string>(), CreatedAt = DateTime.UtcNow };
            
            if (!caretakerProfile.CaregiversID.Contains(actualCaregiverId))
            {
                caretakerProfile.CaregiversID.Add(actualCaregiverId);
            }

            var caretakerUrl = BuildFirestoreUrl(COLLECTION_USERS, caretakerId, $"updateMask.fieldPaths={FIELD_CAREGIVERS_ID}");
            var caretakerPayload = BuildArrayFieldPayload(FIELD_CAREGIVERS_ID, caretakerProfile.CaregiversID);
            
            return await SendPatchRequestAsync(caretakerUrl, caretakerPayload, idToken, "caretaker profile");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error accepting invitation: {ex.Message}");
            return false;
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

            var url = BuildFirestoreUrl(COLLECTION_INVITATIONS, invitationId);
            var invitationDoc = await FetchFirestoreDocumentAsync(url, idToken);
            
            if (invitationDoc == null || !invitationDoc.RootElement.TryGetProperty("fields", out var fields))
            {
                return false;
            }

            var invitation = MapToCaregiverInvitation(invitationDoc.RootElement, fields);
            invitation.Status = STATUS_REJECTED;
            invitation.RespondedAt = DateTime.UtcNow;

            var payloadJson = FirestorePayloadBuilder.BuildInvitationPayload(invitation);
            return await SendPatchRequestAsync(url, payloadJson, idToken, "invitation");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error rejecting invitation: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Caregiver Relationship Operations

    public async Task<bool> RemoveCaregiverAsync(string userId, string caregiverId, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(caregiverId))
            {
                return false;
            }

            var userProfile = await GetUserProfileAsync(userId, idToken);
            if (userProfile == null)
                return false;

            userProfile.CaregiversID.Remove(caregiverId);

            var caregiverProfile = await GetUserProfileAsync(caregiverId, idToken);
            if (caregiverProfile == null)
                return false;

            caregiverProfile.CaretakersID.Remove(userId);

            // Update user profile (normal update)
            if (!await SaveUserProfileAsync(userId, userProfile, idToken))
            {
                return false;
            }

            // Update caregiver profile using targeted update
            var caregiverUrl = BuildFirestoreUrl(COLLECTION_USERS, caregiverId, $"updateMask.fieldPaths={FIELD_CARETAKERS_ID}");
            var caregiverPayload = BuildArrayFieldPayload(FIELD_CARETAKERS_ID, caregiverProfile.CaretakersID);
            
            return await SendPatchRequestAsync(caregiverUrl, caregiverPayload, idToken, "caregiver profile");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error removing caregiver: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    public async Task<List<CaregiverInfo>> GetCaregiversAsync(string userId, string idToken)
    {
        var caregivers = new List<CaregiverInfo>();

        try
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(idToken))
            {
                return caregivers;
            }

            var userProfile = await GetUserProfileAsync(userId, idToken);
            
            if (userProfile?.CaregiversID.Count == 0)
            {
                return caregivers;
            }

            foreach (var caregiverId in userProfile!.CaregiversID)
            {
                var profile = await GetUserProfileAsync(caregiverId, idToken);
                if (profile != null)
                {
                    caregivers.Add(new CaregiverInfo
                    {
                        UserId = caregiverId,
                        Email = profile.Email,
                        FirstName = profile.FirstName,
                        LastName = profile.LastName,
                        Status = STATUS_ACCEPTED,
                        AddedAt = profile.CreatedAt
                    });
                }
            }

            return caregivers;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching caregivers: {ex.Message}\n{ex.StackTrace}");
            return caregivers;
        }
    }

    #endregion

    #region Question Management Operations

    public async Task<bool> SaveQuestionAsync(string caretakerId, string caregiverId, Question question, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(caretakerId) || string.IsNullOrWhiteSpace(caregiverId) || string.IsNullOrWhiteSpace(idToken))
            {
                return false;
            }

            question.Id = string.IsNullOrEmpty(question.Id) ? Guid.NewGuid().ToString() : question.Id;
            question.CaretakerId = caretakerId;
            question.CaregiverId = caregiverId;
            question.UpdatedAt = DateTime.UtcNow;

            var url = BuildFirestoreUrl("questions", question.Id);
            var payloadJson = FirestorePayloadBuilder.BuildQuestionPayload(question);

            return await SendPatchRequestAsync(url, payloadJson, idToken, "question");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving question: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    public async Task<Question?> GetQuestionAsync(string questionId, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(questionId) || string.IsNullOrWhiteSpace(idToken))
            {
                return null;
            }

            var url = BuildFirestoreUrl("questions", questionId);
            var jsonDocument = await FetchFirestoreDocumentAsync(url, idToken);

            if (jsonDocument == null || !jsonDocument.RootElement.TryGetProperty("fields", out var fields))
            {
                return null;
            }

            return MapToQuestion(jsonDocument.RootElement, fields);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching question: {ex.Message}");
            return null;
        }
    }

    public async Task<List<Question>> GetCaregiverQuestionsAsync(string caretakerId, string caregiverId, string idToken)
    {
        var questions = new List<Question>();

        try
        {
            if (string.IsNullOrWhiteSpace(caretakerId) || string.IsNullOrWhiteSpace(caregiverId) || string.IsNullOrWhiteSpace(idToken))
            {
                return questions;
            }

            var url = BuildFirestoreUrl("questions");
            var jsonDocument = await FetchFirestoreDocumentAsync(url, idToken);

            if (jsonDocument == null || !jsonDocument.RootElement.TryGetProperty("documents", out var documents))
            {
                return questions;
            }

            foreach (var doc in documents.EnumerateArray())
            {
                if (!doc.TryGetProperty("fields", out var fields))
                    continue;

                var careTakerId = FirestoreValueExtractor.GetStringValue(fields, "caretakerId");
                var careGiverId = FirestoreValueExtractor.GetStringValue(fields, "caregiverId");

                if (careTakerId == caretakerId && careGiverId == caregiverId)
                {
                    var question = MapToQuestion(doc, fields);
                    questions.Add(question);
                }
            }

            return questions.OrderBy(q => q.Order).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching questions: {ex.Message}");
            return questions;
        }
    }

    public async Task<bool> UpdateQuestionAsync(string caretakerId, string caregiverId, Question question, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(question.Id) || string.IsNullOrWhiteSpace(idToken))
            {
                return false;
            }

            question.UpdatedAt = DateTime.UtcNow;
            var url = BuildFirestoreUrl("questions", question.Id);
            var payloadJson = FirestorePayloadBuilder.BuildQuestionPayload(question);

            return await SendPatchRequestAsync(url, payloadJson, idToken, "question");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating question: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    public async Task<bool> DeleteQuestionAsync(string questionId, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(questionId) || string.IsNullOrWhiteSpace(idToken))
            {
                return false;
            }

            var url = BuildFirestoreUrl("questions", questionId);
            return await SendDeleteRequestAsync(url, idToken, "question");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting question: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Answer Operations

    public async Task<bool> SaveAnswerAsync(string caretakerId, string caregiverId, QuestionAnswer answer, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(caretakerId) || string.IsNullOrWhiteSpace(caregiverId) || string.IsNullOrWhiteSpace(idToken))
            {
                return false;
            }

            answer.Id = string.IsNullOrEmpty(answer.Id) ? Guid.NewGuid().ToString() : answer.Id;
            answer.CaretakerId = caretakerId;
            answer.CaregiverId = caregiverId;

            var url = BuildFirestoreUrl("answers", answer.Id);
            var payloadJson = FirestorePayloadBuilder.BuildAnswerPayload(answer);

            return await SendPatchRequestAsync(url, payloadJson, idToken, "answer");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving answer: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    public async Task<List<QuestionAnswer>> GetCaretakerAnswersAsync(string caretakerId, string caregiverId, string idToken)
    {
        var answers = new List<QuestionAnswer>();

        try
        {
            if (string.IsNullOrWhiteSpace(caretakerId) || string.IsNullOrWhiteSpace(caregiverId) || string.IsNullOrWhiteSpace(idToken))
            {
                return answers;
            }

            var url = BuildFirestoreUrl("answers");
            var jsonDocument = await FetchFirestoreDocumentAsync(url, idToken);

            if (jsonDocument == null || !jsonDocument.RootElement.TryGetProperty("documents", out var documents))
            {
                return answers;
            }

            foreach (var doc in documents.EnumerateArray())
            {
                if (!doc.TryGetProperty("fields", out var fields))
                    continue;

                var careTakerId = FirestoreValueExtractor.GetStringValue(fields, "caretakerId");
                var careGiverId = FirestoreValueExtractor.GetStringValue(fields, "caregiverId");

                if (careTakerId == caretakerId && careGiverId == caregiverId)
                {
                    var answer = MapToQuestionAnswer(doc, fields);
                    answers.Add(answer);
                }
            }

            return answers.OrderByDescending(a => a.AnsweredAt).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching answers: {ex.Message}");
            return answers;
        }
    }

    #endregion

    #region Test Session Operations

    public async Task<bool> SaveTestSessionAsync(string caretakerId, string caregiverId, TestSession session, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(caretakerId) || string.IsNullOrWhiteSpace(caregiverId) || string.IsNullOrWhiteSpace(idToken))
            {
                return false;
            }

            session.Id = string.IsNullOrEmpty(session.Id) ? Guid.NewGuid().ToString() : session.Id;
            session.CaretakerId = caretakerId;
            session.CaregiverId = caregiverId;

            var url = BuildFirestoreUrl("test_sessions", session.Id);
            var payloadJson = FirestorePayloadBuilder.BuildTestSessionPayload(session);

            return await SendPatchRequestAsync(url, payloadJson, idToken, "test session");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving test session: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    public async Task<TestSession?> GetLatestTestSessionAsync(string caretakerId, string caregiverId, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(caretakerId) || string.IsNullOrWhiteSpace(caregiverId) || string.IsNullOrWhiteSpace(idToken))
            {
                return null;
            }

            var sessions = await GetTestSessionsAsync(caretakerId, caregiverId, idToken);
            return sessions.OrderByDescending(s => s.CompletedAt).FirstOrDefault();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching latest test session: {ex.Message}");
            return null;
        }
    }

    public async Task<List<TestSession>> GetTestSessionsAsync(string caretakerId, string caregiverId, string idToken)
    {
        var sessions = new List<TestSession>();

        try
        {
            if (string.IsNullOrWhiteSpace(caretakerId) || string.IsNullOrWhiteSpace(caregiverId) || string.IsNullOrWhiteSpace(idToken))
            {
                return sessions;
            }

            var url = BuildFirestoreUrl("test_sessions");
            var jsonDocument = await FetchFirestoreDocumentAsync(url, idToken);

            if (jsonDocument == null || !jsonDocument.RootElement.TryGetProperty("documents", out var documents))
            {
                return sessions;
            }

            foreach (var doc in documents.EnumerateArray())
            {
                if (!doc.TryGetProperty("fields", out var fields))
                    continue;

                var careTakerId = FirestoreValueExtractor.GetStringValue(fields, "caretakerId");
                var careGiverId = FirestoreValueExtractor.GetStringValue(fields, "caregiverId");

                if (careTakerId == caretakerId && careGiverId == caregiverId)
                {
                    var session = MapToTestSession(doc, fields);
                    sessions.Add(session);
                }
            }

            return sessions.OrderByDescending(s => s.CompletedAt).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching test sessions: {ex.Message}");
            return sessions;
        }
    }

    #endregion

    #region Mapping Methods

    private static Question MapToQuestion(JsonElement doc, JsonElement fields)
    {
        return new Question
        {
            Id = FirestoreValueExtractor.GetDocumentId(doc),
            CaretakerId = FirestoreValueExtractor.GetStringValue(fields, "caretakerId"),
            CaregiverId = FirestoreValueExtractor.GetStringValue(fields, "caregiverId"),
            Text = FirestoreValueExtractor.GetStringValue(fields, "text"),
            Description = FirestoreValueExtractor.GetStringValue(fields, "description"),
            Type = FirestoreValueExtractor.GetStringValue(fields, "type"),
            Options = FirestoreValueExtractor.GetQuestionOptions(fields, "options"),
            IsActive = FirestoreValueExtractor.GetBoolValue(fields, "isActive"),
            CreatedAt = FirestoreValueExtractor.GetTimestampValue(fields, "createdAt"),
            UpdatedAt = FirestoreValueExtractor.GetTimestampValue(fields, "updatedAt"),
            Order = FirestoreValueExtractor.GetIntValue(fields, "order")
        };
    }

    private static QuestionAnswer MapToQuestionAnswer(JsonElement doc, JsonElement fields)
    {
        return new QuestionAnswer
        {
            Id = FirestoreValueExtractor.GetDocumentId(doc),
            QuestionId = FirestoreValueExtractor.GetStringValue(fields, "questionId"),
            CaretakerId = FirestoreValueExtractor.GetStringValue(fields, "caretakerId"),
            CaregiverId = FirestoreValueExtractor.GetStringValue(fields, "caregiverId"),
            SelectedOption = FirestoreValueExtractor.GetStringValue(fields, "selectedOption"),
            SelectedOptionPoints = FirestoreValueExtractor.GetDecimalValue(fields, "selectedOptionPoints"),
            OpenAnswer = FirestoreValueExtractor.GetStringValue(fields, "openAnswer"),
            AnsweredAt = FirestoreValueExtractor.GetTimestampValue(fields, "answeredAt")
        };
    }

    private static TestSession MapToTestSession(JsonElement doc, JsonElement fields)
    {
        return new TestSession
        {
            Id = FirestoreValueExtractor.GetDocumentId(doc),
            CaretakerId = FirestoreValueExtractor.GetStringValue(fields, "caretakerId"),
            CaregiverId = FirestoreValueExtractor.GetStringValue(fields, "caregiverId"),
            TotalPoints = FirestoreValueExtractor.GetDecimalValue(fields, "totalPoints"),
            MaxPoints = FirestoreValueExtractor.GetDecimalValue(fields, "maxPoints"),
            PercentageScore = FirestoreValueExtractor.GetDecimalValue(fields, "percentageScore"),
            CompletedAt = FirestoreValueExtractor.GetTimestampValue(fields, "completedAt")
        };
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Builds a Firestore REST API URL
    /// </summary>
    private string BuildFirestoreUrl(string collection, string? documentId = null, string? queryParams = null)
    {
        var path = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/{collection}";
        
        if (!string.IsNullOrEmpty(documentId))
        {
            path += $"/{documentId}";
        }

        path += $"?key={FirebaseConfig.WebApiKey}";

        if (!string.IsNullOrEmpty(queryParams))
        {
            path += $"&{queryParams}";
        }

        return path;
    }

    /// <summary>
    /// Sends a PATCH request to Firestore (thread-safe with request-scoped auth)
    /// </summary>
    private async Task<bool> SendPatchRequestAsync(string url, string payloadJson, string idToken, string operationName)
    {
        try
        {
            var content = new StringContent(payloadJson, System.Text.Encoding.UTF8, "application/json");
            
            var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = content };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Error {operationName}. Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Response: {responseBody}");
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception during {operationName}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sends a DELETE request to Firestore (thread-safe with request-scoped auth)
    /// </summary>
    private async Task<bool> SendDeleteRequestAsync(string url, string idToken, string operationName)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Error {operationName}. Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Response: {responseBody}");
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception during {operationName}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Fetches a Firestore document and returns the parsed JSON (thread-safe)
    /// </summary>
    private async Task<JsonDocument?> FetchFirestoreDocumentAsync(string url, string idToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return JsonDocument.Parse(responseBody);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception fetching document: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Maps Firestore fields to a UserProfile object
    /// </summary>
    private static UserProfile MapToUserProfile(JsonElement fields)
    {
        return new UserProfile
        {
            FirstName = FirestoreValueExtractor.GetStringValue(fields, "firstName"),
            LastName = FirestoreValueExtractor.GetStringValue(fields, "lastName"),
            Age = FirestoreValueExtractor.GetIntValue(fields, "age"),
            Sex = FirestoreValueExtractor.GetStringValue(fields, "sex"),
            PhoneNumber = FirestoreValueExtractor.GetStringValue(fields, "phoneNumber"),
            Email = FirestoreValueExtractor.GetStringValue(fields, FIELD_EMAIL),
            CreatedAt = FirestoreValueExtractor.GetTimestampValue(fields, "createdAt"),
            IsCaregiver = FirestoreValueExtractor.GetBoolValue(fields, "isCaregiver"),
            CaretakersID = FirestoreValueExtractor.GetStringArray(fields, FIELD_CARETAKERS_ID),
            CaregiversID = FirestoreValueExtractor.GetStringArray(fields, FIELD_CAREGIVERS_ID)
        };
    }

    /// <summary>
    /// Maps Firestore document and fields to a CaregiverInvitation object
    /// </summary>
    private static CaregiverInvitation MapToCaregiverInvitation(JsonElement doc, JsonElement fields)
    {
        return new CaregiverInvitation
        {
            Id = FirestoreValueExtractor.GetDocumentId(doc),
            FromUserId = FirestoreValueExtractor.GetStringValue(fields, FIELD_FROM_USER_ID),
            ToUserId = FirestoreValueExtractor.GetStringValue(fields, FIELD_TO_USER_ID),
            ToUserEmail = FirestoreValueExtractor.GetStringValue(fields, "toUserEmail"),
            FromUserName = FirestoreValueExtractor.GetStringValue(fields, "fromUserName"),
            Status = FirestoreValueExtractor.GetStringValue(fields, FIELD_STATUS),
            CreatedAt = FirestoreValueExtractor.GetTimestampValue(fields, "createdAt"),
            RespondedAt = FirestoreValueExtractor.GetTimestampValueNullable(fields, "respondedAt")
        };
    }

    /// <summary>
    /// Builds a JSON payload for updating a string array field
    /// </summary>
    private static string BuildArrayFieldPayload(string fieldName, List<string> values)
    {
        using (var stream = new MemoryStream())
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
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    #endregion
}
