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
    Task<List<CaregiverInvitation>> GetReceivedRejectedInvitationsAsync(string userId, string idToken);
    Task<List<CaregiverInvitation>> GetAllCaregiverInvitationsAsync(string userId, string idToken);
    Task<List<CaregiverInvitation>> GetAllReceivedInvitationsAsync(string userId, string idToken);
    Task<bool> AcceptCaregiverInvitationAsync(string userId, string invitationId, string caregiverId, string idToken);
    Task<bool> RejectCaregiverInvitationAsync(string userId, string invitationId, string idToken);
    Task<bool> DeleteCaregiverInvitationAsync(string invitationId, string idToken);
    
    // Caregiver and Caretaker Relationship Operations
    Task<bool> RemoveCaregiverAsync(string userId, string caregiverId, string idToken);
    Task<bool> RemoveCaretakerAsync(string userId, string caretakerId, string idToken);
    Task<List<CaregiverInfo>> GetCaregiversAsync(string userId, string idToken);
    Task<List<CaregiverInfo>> GetCaretakersAsync(string userId, string idToken);

    // Question Management Operations
    Task<bool> SaveQuestionToCaretakerAsync(string caretakerId, Question question, string idToken);
    Task<bool> UpdateQuestionToCaretakerAsync(string caretakerId, Question question, string idToken);
    Task<bool> DeleteQuestionFromCaretakerAsync(string caretakerId, string questionId, string idToken);

    // CareTaker Questions Operations
    Task<bool> CreateCareTakerQuestionsAsync(string caretakerId, List<Question> questions, string idToken);
    Task<CareTakerQuestions?> GetCareTakerQuestionsAsync(string caretakerId, string idToken);
    Task<bool> UpdateCareTakerQuestionsAsync(string caretakerId, List<Question> questions, string idToken);

    // Answer Operations
    Task<bool> SaveAnswerAsync(string caretakerId, QuestionAnswer answer, string idToken);
    Task<List<QuestionAnswer>> GetCaretakerAnswersAsync(string caretakerId, string idToken);

    // Test Session Operations
    Task<bool> SaveTestSessionAsync(string caretakerId, TestSession session, string idToken);
    Task<TestSession?> GetLatestTestSessionAsync(string caretakerId, string idToken);
    Task<List<TestSession>> GetTestSessionsAsync(string caretakerId, string idToken);
}
