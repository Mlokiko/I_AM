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

            var profile = MapToUserProfile(fields);
            profile.Id = userId;  // Ustaw ID bezpoœrednio
            return profile;
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

    public async Task<List<CaregiverInvitation>> GetReceivedRejectedInvitationsAsync(string userId, string idToken)
    {
        return await GetInvitationsAsync(
            userId, 
            idToken, 
            (inv, uid) => inv.ToUserId == uid && inv.Status == STATUS_REJECTED);
    }

    public async Task<List<CaregiverInvitation>> GetAllCaregiverInvitationsAsync(string userId, string idToken)
    {
        return await GetInvitationsAsync(
            userId, 
            idToken, 
            (inv, uid) => inv.FromUserId == uid);
    }

    public async Task<List<CaregiverInvitation>> GetAllReceivedInvitationsAsync(string userId, string idToken)
    {
        return await GetInvitationsAsync(
            userId, 
            idToken, 
            (inv, uid) => inv.ToUserId == uid);
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
            var caretakerPayload = FirestorePayloadBuilder.BuildStringArrayPayload(FIELD_CAREGIVERS_ID, caretakerProfile.CaregiversID);
            
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
            var caregiverPayload = FirestorePayloadBuilder.BuildStringArrayPayload(FIELD_CARETAKERS_ID, caregiverProfile.CaretakersID);
            
            return await SendPatchRequestAsync(caregiverUrl, caregiverPayload, idToken, "caregiver profile");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error removing caregiver: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    public async Task<bool> RemoveCaretakerAsync(string userId, string caretakerId, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(caretakerId))
            {
                return false;
            }

            var userProfile = await GetUserProfileAsync(userId, idToken);
            if (userProfile == null)
                return false;

            userProfile.CaretakersID.Remove(caretakerId);

            var caretakerProfile = await GetUserProfileAsync(caretakerId, idToken);
            if (caretakerProfile == null)
                return false;

            caretakerProfile.CaregiversID.Remove(userId);

            // Update user profile (normal update)
            if (!await SaveUserProfileAsync(userId, userProfile, idToken))
            {
                return false;
            }

            // Update caretaker profile using targeted update
            var caretakerUrl = BuildFirestoreUrl(COLLECTION_USERS, caretakerId, $"updateMask.fieldPaths={FIELD_CAREGIVERS_ID}");
            var caretakerPayload = FirestorePayloadBuilder.BuildStringArrayPayload(FIELD_CAREGIVERS_ID, caretakerProfile.CaregiversID);
            
            return await SendPatchRequestAsync(caretakerUrl, caretakerPayload, idToken, "caretaker profile");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error removing caretaker: {ex.Message}\n{ex.StackTrace}");
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

    public async Task<List<CaregiverInfo>> GetCaretakersAsync(string userId, string idToken)
    {
        var caretakers = new List<CaregiverInfo>();

        try
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(idToken))
            {
                return caretakers;
            }

            var userProfile = await GetUserProfileAsync(userId, idToken);
            
            if (userProfile?.CaretakersID.Count == 0)
            {
                return caretakers;
            }

            foreach (var caretakerId in userProfile!.CaretakersID)
            {
                var profile = await GetUserProfileAsync(caretakerId, idToken);
                if (profile != null)
                {
                    caretakers.Add(new CaregiverInfo
                    {
                        UserId = caretakerId,
                        Email = profile.Email,
                        FirstName = profile.FirstName,
                        LastName = profile.LastName,
                        Status = STATUS_ACCEPTED,
                        AddedAt = profile.CreatedAt
                    });
                }
            }

            return caretakers;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching caretakers: {ex.Message}\n{ex.StackTrace}");
            return caretakers;
        }
    }

    #endregion

    #region Question Management Operations

    public async Task<bool> SaveQuestionAsync(string caretakerId, Question question, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(caretakerId) || string.IsNullOrWhiteSpace(idToken))
            {
                return false;
            }

            question.Id = string.IsNullOrEmpty(question.Id) ? Guid.NewGuid().ToString() : question.Id;
            question.CaretakerId = caretakerId;
            question.UpdatedAt = DateTime.UtcNow;

            System.Diagnostics.Debug.WriteLine(
                $"[SaveQuestionAsync] Saving question: ID={question.Id}, Text={question.Text}, CaretakerId={caretakerId}");

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

    public async Task<List<Question>> GetCaregiverQuestionsAsync(string caretakerId, string idToken)
    {
        var questions = new List<Question>();

        try
        {
            if (string.IsNullOrWhiteSpace(caretakerId) || string.IsNullOrWhiteSpace(idToken))
            {
                return questions;
            }

            var url = BuildFirestoreUrl("questions_of_caretakers", caretakerId);
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

                if (careTakerId == caretakerId)
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

    public async Task<bool> UpdateQuestionAsync(string caretakerId, Question question, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(question.Id) || string.IsNullOrWhiteSpace(idToken))
            {
                return false;
            }

            question.CaretakerId = caretakerId;
            question.UpdatedAt = DateTime.UtcNow;

            System.Diagnostics.Debug.WriteLine(
                $"[UpdateQuestionAsync] Updating question: ID={question.Id}, Text={question.Text}");

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

            System.Diagnostics.Debug.WriteLine(
                $"[DeleteQuestionAsync] Deleting question: ID={questionId}");

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

    #region CareTaker Questions Operations

    public async Task<bool> CreateCareTakerQuestionsAsync(string caretakerId, List<Question> questions, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(caretakerId) || string.IsNullOrWhiteSpace(idToken) || questions == null)
            {
                return false;
            }

            var careTakerQuestions = new CareTakerQuestions
            {
                CaretakerId = caretakerId,
                Questions = questions,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var url = BuildFirestoreUrl("questions_of_caretakers", caretakerId);
            var payloadJson = FirestorePayloadBuilder.BuildCareTakerQuestionsPayload(careTakerQuestions);

            return await SendPatchRequestAsync(url, payloadJson, idToken, "careTaker questions");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating careTaker questions: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    public async Task<CareTakerQuestions?> GetCareTakerQuestionsAsync(string caretakerId, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(caretakerId) || string.IsNullOrWhiteSpace(idToken))
            {
                return null;
            }

            var url = BuildFirestoreUrl("questions_of_caretakers", caretakerId);
            var jsonDocument = await FetchFirestoreDocumentAsync(url, idToken);

            if (jsonDocument == null || !jsonDocument.RootElement.TryGetProperty("fields", out var fields))
            {
                return null;
            }

            return MapToCareTakerQuestions(jsonDocument.RootElement, fields);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching careTaker questions: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateCareTakerQuestionsAsync(string caretakerId, List<Question> questions, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(caretakerId) || string.IsNullOrWhiteSpace(idToken) || questions == null)
            {
                return false;
            }

            var careTakerQuestions = new CareTakerQuestions
            {
                CaretakerId = caretakerId,
                Questions = questions,
                UpdatedAt = DateTime.UtcNow
            };

            var url = BuildFirestoreUrl("questions_of_caretakers", caretakerId);
            var payloadJson = FirestorePayloadBuilder.BuildCareTakerQuestionsPayload(careTakerQuestions);

            return await SendPatchRequestAsync(url, payloadJson, idToken, "careTaker questions");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating careTaker questions: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    #endregion

    #region Answer Operations

    public async Task<bool> SaveAnswerAsync(string caretakerId, QuestionAnswer answer, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(caretakerId) || string.IsNullOrWhiteSpace(idToken))
            {
                return false;
            }

            answer.Id = string.IsNullOrEmpty(answer.Id) ? Guid.NewGuid().ToString() : answer.Id;
            answer.CaretakerId = caretakerId;

            System.Diagnostics.Debug.WriteLine(
                $"[SaveAnswerAsync] Saving answer: ID={answer.Id}, QuestionId={answer.QuestionId}, CaretakerId={caretakerId}");

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

    public async Task<List<QuestionAnswer>> GetCaretakerAnswersAsync(string caretakerId, string idToken)
    {
        var answers = new List<QuestionAnswer>();

        try
        {
            if (string.IsNullOrWhiteSpace(caretakerId) || string.IsNullOrWhiteSpace(idToken))
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

                var answersCaretakerId = FirestoreValueExtractor.GetStringValue(fields, "caretakerId");

                if (answersCaretakerId == caretakerId)
                {
                    var answer = MapToQuestionAnswer(doc, fields);
                    answers.Add(answer);
                }
            }

            return answers;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching answers: {ex.Message}");
            return answers;
        }
    }

    #endregion

    #region Test Session Operations

    public async Task<bool> SaveTestSessionAsync(string caretakerId, TestSession session, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(caretakerId) || string.IsNullOrWhiteSpace(idToken))
            {
                return false;
            }

            session.Id = string.IsNullOrEmpty(session.Id) ? Guid.NewGuid().ToString() : session.Id;
            session.CaretakerId = caretakerId;

            System.Diagnostics.Debug.WriteLine(
                $"[SaveTestSessionAsync] Saving test session: ID={session.Id}, CaretakerId={caretakerId}");

            var url = BuildFirestoreUrl("testSessions", session.Id);
            var payloadJson = FirestorePayloadBuilder.BuildTestSessionPayload(session);

            return await SendPatchRequestAsync(url, payloadJson, idToken, "test session");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving test session: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    public async Task<TestSession?> GetLatestTestSessionAsync(string caretakerId, string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(caretakerId) || string.IsNullOrWhiteSpace(idToken))
            {
                return null;
            }

            var url = BuildFirestoreUrl("testSessions");
            var jsonDocument = await FetchFirestoreDocumentAsync(url, idToken);

            if (jsonDocument == null || !jsonDocument.RootElement.TryGetProperty("documents", out var documents))
            {
                return null;
            }

            TestSession? latestSession = null;

            foreach (var doc in documents.EnumerateArray())
            {
                if (!doc.TryGetProperty("fields", out var fields))
                    continue;

                var sessionCaretakerId = FirestoreValueExtractor.GetStringValue(fields, "caretakerId");

                if (sessionCaretakerId == caretakerId)
                {
                    var session = MapToTestSession(doc, fields);
                    if (latestSession == null || session.CompletedAt > latestSession.CompletedAt)
                    {
                        latestSession = session;
                    }
                }
            }

            return latestSession;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching latest test session: {ex.Message}");
            return null;
        }
    }

    public async Task<List<TestSession>> GetTestSessionsAsync(string caretakerId, string idToken)
    {
        var sessions = new List<TestSession>();

        try
        {
            if (string.IsNullOrWhiteSpace(caretakerId) || string.IsNullOrWhiteSpace(idToken))
            {
                return sessions;
            }

            var url = BuildFirestoreUrl("testSessions");
            var jsonDocument = await FetchFirestoreDocumentAsync(url, idToken);

            if (jsonDocument == null || !jsonDocument.RootElement.TryGetProperty("documents", out var documents))
            {
                return sessions;
            }

            foreach (var doc in documents.EnumerateArray())
            {
                if (!doc.TryGetProperty("fields", out var fields))
                    continue;

                var sessionCaretakerId = FirestoreValueExtractor.GetStringValue(fields, "caretakerId");

                if (sessionCaretakerId == caretakerId)
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

    /// <summary>
    /// Maps Firestore document and fields to a TestSession object
    /// </summary>
    private static TestSession MapToTestSession(JsonElement doc, JsonElement fields)
    {
        return new TestSession
        {
            Id = FirestoreValueExtractor.GetDocumentId(doc),
            CaretakerId = FirestoreValueExtractor.GetStringValue(fields, "caretakerId"),
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
            Id = FirestoreValueExtractor.GetDocumentId(fields),
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
    /// Maps Firestore document and fields to a Question object
    /// </summary>
    private static Question MapToQuestion(JsonElement doc, JsonElement fields)
    {
        return new Question
        {
            Id = FirestoreValueExtractor.GetDocumentId(doc),
            CaretakerId = FirestoreValueExtractor.GetStringValue(fields, "caretakerId"),
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

    /// <summary>
    /// Maps Firestore document and fields to a CareTakerQuestions object
    /// </summary>
    private static CareTakerQuestions MapToCareTakerQuestions(JsonElement doc, JsonElement fields)
    {
        try
        {
            return new CareTakerQuestions
            {
                CaretakerId = FirestoreValueExtractor.GetStringValue(fields, "caretakerId"),
                Questions = FirestoreValueExtractor.GetQuestionsList(fields, "questions") ?? new List<Question>(),
                CreatedAt = FirestoreValueExtractor.GetTimestampValue(fields, "createdAt"),
                UpdatedAt = FirestoreValueExtractor.GetTimestampValue(fields, "updatedAt")
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MapToCareTakerQuestions] Error mapping careTaker questions: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Maps Firestore document and fields to a QuestionAnswer object
    /// </summary>
    private static QuestionAnswer MapToQuestionAnswer(JsonElement doc, JsonElement fields)
    {
        return new QuestionAnswer
        {
            Id = FirestoreValueExtractor.GetDocumentId(doc),
            QuestionId = FirestoreValueExtractor.GetStringValue(fields, "questionId"),
            CaretakerId = FirestoreValueExtractor.GetStringValue(fields, "caretakerId"),
            SelectedOption = FirestoreValueExtractor.GetStringValue(fields, "selectedOption"),
            SelectedOptionPoints = FirestoreValueExtractor.GetDecimalValue(fields, "selectedOptionPoints"),
            OpenAnswer = FirestoreValueExtractor.GetStringValue(fields, "openAnswer"),
            AnsweredAt = FirestoreValueExtractor.GetTimestampValue(fields, "answeredAt")
        };
    }

    #endregion
}
