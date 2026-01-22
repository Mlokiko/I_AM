using I_AM.Models;

namespace I_AM.Services.Interfaces;

/// <summary>
/// Service for Firestore database operations
/// </summary>
public interface IFirestoreService
{
    // User Profile Operations
    Task<bool> SaveUserProfileAsync(string userId, UserProfile profile, string idToken);
    Task<UserProfile?> GetUserProfileAsync(string userId, string idToken);
    Task<bool> DeleteUserProfileAsync(string userId, string idToken);
    
    // Public Profile Operations
    Task<bool> SaveUserPublicProfileAsync(string userId, UserPublicProfile profile, string idToken);
    Task<(UserPublicProfile? profile, string? userId)> GetUserPublicProfileByEmailAsync(string email, string idToken);
    
    // Caregiver Invitation Operations
    Task<bool> SaveCaregiverInvitationAsync(string invitationId, CaregiverInvitation invitation, string idToken);
    Task<List<CaregiverInvitation>> GetPendingInvitationsAsync(string userId, string idToken);
    Task<List<CaregiverInvitation>> GetSentPendingInvitationsAsync(string userId, string idToken);
    Task<List<CaregiverInvitation>> GetSentRejectedInvitationsAsync(string userId, string idToken);
    Task<List<CaregiverInvitation>> GetAllCaregiverInvitationsAsync(string userId, string idToken);
    Task<bool> AcceptCaregiverInvitationAsync(string userId, string invitationId, string caregiverId, string idToken);
    Task<bool> RejectCaregiverInvitationAsync(string userId, string invitationId, string idToken);
    Task<bool> DeleteCaregiverInvitationAsync(string invitationId, string idToken);
    
    // Caregiver Relationship Operations
    Task<bool> RemoveCaregiverAsync(string userId, string caregiverId, string idToken);
    Task<List<CaregiverInfo>> GetCaregiversAsync(string userId, string idToken);


//DODANO KLAUDIA
    // Question Management Operations
    Task<bool> SaveQuestionAsync(string caretakerId, string caregiverId, Question question, string idToken);
    Task<Question?> GetQuestionAsync(string questionId, string idToken);
    Task<List<Question>> GetCaregiverQuestionsAsync(string caretakerId, string caregiverId, string idToken);
    Task<bool> UpdateQuestionAsync(string caretakerId, string caregiverId, Question question, string idToken);
    Task<bool> DeleteQuestionAsync(string questionId, string idToken);

    // Answer Operations
    Task<bool> SaveAnswerAsync(string caretakerId, string caregiverId, QuestionAnswer answer, string idToken);
    Task<List<QuestionAnswer>> GetCaretakerAnswersAsync(string caretakerId, string caregiverId, string idToken);

    // Test Session Operations
    Task<bool> SaveTestSessionAsync(string caretakerId, string caregiverId, TestSession session, string idToken);
    Task<TestSession?> GetLatestTestSessionAsync(string caretakerId, string caregiverId, string idToken);
    Task<List<TestSession>> GetTestSessionsAsync(string caretakerId, string caregiverId, string idToken);
}
